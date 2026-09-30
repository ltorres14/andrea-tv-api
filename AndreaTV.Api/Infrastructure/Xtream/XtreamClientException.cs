using System.Net;

namespace AndreaTV.Api.Infrastructure.Xtream;

public enum XtreamClientError
{
    Configuration,
    Unavailable,
    Timeout,
    HttpError,
    InvalidResponse
}

public sealed class XtreamClientException : Exception
{
    public XtreamClientException(
        XtreamClientError error,
        string message,
        HttpStatusCode? providerStatusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
        ProviderStatusCode = providerStatusCode;
    }

    public XtreamClientError Error { get; }

    public HttpStatusCode? ProviderStatusCode { get; }
}
