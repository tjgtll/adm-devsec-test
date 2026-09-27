using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using App.Models;
using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace App.Services;

public class ProcessService : IProcessService
{
    private readonly NpgsqlDataSource _dataSource;
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ProcessService(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ProcessResponse> ProcessAsync(ProcessRequest request, CancellationToken ct)
    {
        try
        {
            var url = string.Empty;
            try
            {
                url = Encoding.UTF8.GetString(Convert.FromBase64String(request.Url_b64!));
            }
            catch (FormatException ex)
            {
                return Error("URL_BASE64_DECODE_ERROR", ex.Message);
            }

            var page = string.Empty;
            try
            {
                page = Encoding.UTF8.GetString(Convert.FromBase64String(request.Page_b64!));
            }
            catch (FormatException ex)
            {
                return Error("PAGE_BASE64_DECODE_ERROR", ex.Message);
            }

            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(page, ct);

            var elements = new List<IElement>();
            try
            {
                elements = document.QuerySelectorAll(request.Selector!) 
                                   .ToList();
            }
            catch (DomException ex)
            {
                return Error("INVALID_SELECTOR", ex.Message);
            }

            var attrList = elements.Select(e => e.GetAttribute(request.Attribute!) 
                                                ?? string.Empty)
                                   .ToList();

            var emails = await Task.Run(() => EmailRegex.Matches(page)
                                                        .Select(m => m.Value)
                                                        .ToList(), ct);

            var encryptedBytes = null as byte[];
            try
            {
                encryptedBytes = Convert.FromBase64String(request.Encrypted_text_bytes_b64!);
            }
            catch (FormatException ex)
            {
                return Error("ENCRYPTED_TEXT_BASE64_DECODE_ERROR", ex.Message);
            }

            var keyBytes = null as byte[];
            try
            {
                keyBytes = Convert.FromBase64String(request.Key_bytes_b64!);
            }
            catch (FormatException ex)
            {
                return Error("KEY_BASE64_DECODE_ERROR", ex.Message);
            }

            var decryptedText = string.Empty;
            try
            {
                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                decryptedText = Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                return Error("DECRYPTION_ERROR", ex.Message);
            }

            await SaveElementsAsync(elements, request.Attribute!, ct);

            return new ProcessResponse
            {
                IsError = 0,
                ErrorCode = string.Empty,
                ErrorMessage = string.Empty,
                ElementsCount = elements.Count,
                EmailsCount = emails.Count,
                Url = url,
                DecryptedPlainText = decryptedText,
                ElementsAttrList = attrList,
                EmailsList = emails
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Error("INTERNAL_ERROR", ex.Message);
        }
    }


    private async Task SaveElementsAsync(IReadOnlyCollection<IElement> elements,
                                         string attribute,
                                         CancellationToken ct)
    {
        if (elements.Count == 0) return;

        var rows = elements.Select(e => new
                                        {
                                            AttrValue = e.GetAttribute(attribute) 
                                                        ?? string.Empty,
                                            Html = e.OuterHtml
                                        }).ToArray();

        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        const string insertSql = """
            INSERT INTO elements (attr_value, html)
            VALUES (@AttrValue, @Html);
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(insertSql, rows, cancellationToken: ct));
    }

    private static ProcessResponse Error(string code, string message) => new()
    {
        IsError = 1,
        ErrorCode = code,
        ErrorMessage = message,
        ElementsCount = 0,
        EmailsCount = 0,
        Url = string.Empty,
        DecryptedPlainText = string.Empty,
        ElementsAttrList = new List<string>(),
        EmailsList = new List<string>()
    };
}
