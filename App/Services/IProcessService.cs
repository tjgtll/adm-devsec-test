using App.Models;

namespace App.Services;

public interface IProcessService
{
    Task<ProcessResponse> ProcessAsync(ProcessRequest request, CancellationToken ct);
}
