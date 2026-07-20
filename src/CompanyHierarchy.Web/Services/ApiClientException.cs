using System.Net;

namespace CompanyHierarchy.Web.Services;

public sealed class ApiClientException : Exception
{
    public ApiClientException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
