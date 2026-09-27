using App.Models;
using App.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("api")]
public class ProcessController : ControllerBase
{
    private readonly IProcessService _processService;
    private readonly IValidator<ProcessRequest> _validator;

    public ProcessController(
        IProcessService processService,
        IValidator<ProcessRequest> validator)
    {
        _processService = processService;
        _validator = validator;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Process(
        [FromBody] ProcessRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return Ok(new ProcessResponse
            {
                IsError = 1,
                ErrorCode = "MISSING_BODY",
                ErrorMessage = "Request body is required"
            });
        }

        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var first = validationResult.Errors.First();
            return Ok(new ProcessResponse
            {
                IsError = 1,
                ErrorCode = first.ErrorCode,
                ErrorMessage = first.ErrorMessage
            });
        }

        var result = await _processService.ProcessAsync(request, ct);
        return Ok(result);
    }
}
