using Microsoft.AspNetCore.Mvc;

namespace AndreaTV.Api.Infrastructure.Xtream;

public static class XtreamProblemDetails
{
    public static ObjectResult From(XtreamClientException exception)
    {
        var (status, title, detail) = exception.Error switch
        {
            XtreamClientError.InvalidRequest => (400, "Invalid playback request", exception.Message),
            XtreamClientError.NotFound => (404, "Movie not found", "The requested movie was not found."),
            XtreamClientError.Configuration => (503, "Provider configuration unavailable", "The media provider is not configured."),
            XtreamClientError.CredentialsRejected => (502, "Provider authentication failed", "The media provider rejected the configured credentials."),
            XtreamClientError.Timeout => (504, "Provider timeout", "The media provider did not respond in time."),
            XtreamClientError.Unavailable => (503, "Provider unavailable", "The media provider is currently unavailable."),
            XtreamClientError.MissingLocation => (502, "Invalid playback response", "The provider did not return a playback location."),
            XtreamClientError.InvalidLocation => (502, "Invalid playback response", "The provider returned an invalid playback location."),
            XtreamClientError.UnexpectedResponse => (502, "Unexpected playback response", "The provider did not return the expected playback redirect."),
            _ => (502, "Invalid provider response", "The media provider returned an invalid response.")
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        }) { StatusCode = status };
    }
}
