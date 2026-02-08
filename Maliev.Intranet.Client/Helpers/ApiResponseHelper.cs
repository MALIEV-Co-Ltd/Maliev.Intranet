using System.Net.Http.Json;
using System.Text.Json;

namespace Maliev.Intranet.Client.Helpers;

/// <summary>
/// Helper for parsing API responses and extracting user-friendly error messages.
/// </summary>
public static class ApiResponseHelper
{
    /// <summary>
    /// Attempts to parse the response body as ValidationProblemDetails or a generic error
    /// and returns a human-readable error message.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>A user-friendly error string.</returns>
    public static async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return string.Empty;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Error: {response.StatusCode} ({response.ReasonPhrase})";
        }

        try
        {
            // Try to parse as standard ASP.NET Core ValidationProblemDetails
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var problem = JsonSerializer.Deserialize<ValidationProblemDetailsDto>(content, options);

            if (problem?.Errors != null && problem.Errors.Count != 0)
            {
                // Join all error messages from the dictionary
                return string.Join(" ", problem.Errors.SelectMany(x => x.Value));
            }

            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }
        }
        catch
        {
            // Not a JSON ProblemDetails or parsing failed, fallback to raw content or status code
        }

        // If it looks like JSON but not our expected format, don't show the raw JSON unless it's short
        if (content.TrimStart().StartsWith('{') && content.Length > 200)
        {
            return $"An unexpected error occurred ({response.StatusCode}).";
        }

        return content;
    }

    private sealed class ValidationProblemDetailsDto
    {
        public string? Title { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
