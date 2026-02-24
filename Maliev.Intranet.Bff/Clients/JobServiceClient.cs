using Maliev.Intranet.Shared;
using Maliev.Intranet.Shared.Dtos;
using Maliev.Intranet.Shared.Enums;

namespace Maliev.Intranet.Bff.Clients;

/// <summary>
/// Client for interacting with the Job microservice.
/// </summary>
public interface IJobServiceClient
{
    /// <summary>Retrieves a specific job by ID.</summary>
    Task<JobDto?> GetJobByIdAsync(Guid id, CancellationToken ct = default);
    
    /// <summary>Retrieves a paged list of jobs.</summary>
    Task<PagedResponse<JobDto>?> GetJobsAsync(JobStatus? status = null, string? technology = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    
    /// <summary>Retrieves the Kanban board view of jobs.</summary>
    Task<KanbanResponse?> GetKanbanAsync(CancellationToken ct = default);
    
    /// <summary>Queues a job on a specific machine.</summary>
    Task<JobDto?> QueueJobAsync(Guid id, QueueJobRequest request, CancellationToken ct = default);
    
    /// <summary>Starts a job.</summary>
    Task<JobDto?> StartJobAsync(Guid id, CancellationToken ct = default);
    
    /// <summary>Moves a job to finishing stage.</summary>
    Task<JobDto?> FinishJobAsync(Guid id, CancellationToken ct = default);
    
    /// <summary>Completes a job.</summary>
    Task<JobDto?> CompleteJobAsync(Guid id, CancellationToken ct = default);
    
    /// <summary>Cancels a job.</summary>
    Task<JobDto?> CancelJobAsync(Guid id, CancelJobRequest request, CancellationToken ct = default);
}

/// <summary>
/// Implementation of the job service client.
/// </summary>
public class JobServiceClient(HttpClient httpClient) : IJobServiceClient
{
    /// <inheritdoc />
    public async Task<JobDto?> GetJobByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<JobDto>($"/job/v1/jobs/{id}", ct);
    }

    /// <inheritdoc />
    public async Task<PagedResponse<JobDto>?> GetJobsAsync(JobStatus? status = null, string? technology = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = $"/job/v1/jobs?page={page}&pageSize={pageSize}";
        if (status.HasValue) url += $"&status={status}";
        if (!string.IsNullOrEmpty(technology)) url += $"&technology={technology}";
        
        return await httpClient.GetFromJsonAsync<PagedResponse<JobDto>>(url, ct);
    }

    /// <inheritdoc />
    public async Task<KanbanResponse?> GetKanbanAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("/job/v1/jobs/kanban", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<KanbanResponse>(cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<JobDto?> QueueJobAsync(Guid id, QueueJobRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/queue", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken: ct) : null;
    }

    /// <inheritdoc />
    public async Task<JobDto?> StartJobAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/job/v1/jobs/{id}/start", null, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken: ct) : null;
    }

    /// <inheritdoc />
    public async Task<JobDto?> FinishJobAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/job/v1/jobs/{id}/finish", null, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken: ct) : null;
    }

    /// <inheritdoc />
    public async Task<JobDto?> CompleteJobAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync($"/job/v1/jobs/{id}/complete", null, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken: ct) : null;
    }

    /// <inheritdoc />
    public async Task<JobDto?> CancelJobAsync(Guid id, CancelJobRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/job/v1/jobs/{id}/cancel", request, ct);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JobDto>(cancellationToken: ct) : null;
    }
}
