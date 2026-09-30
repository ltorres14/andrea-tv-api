using AndreaTV.Api.Modules.Devices.Models;
using AndreaTV.Api.Modules.Devices.Services;
using AndreaTV.Api.Infrastructure.Xtream;
using Microsoft.AspNetCore.Mvc;

namespace AndreaTV.Api.Modules.Devices.Controllers;

[ApiController]
[Route("api/devices")]
public sealed class DevicesController(
    IDeviceService deviceService,
    IXtreamConnectionService xtreamConnectionService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<DeviceStatus>(StatusCodes.Status200OK)]
    public ActionResult<DeviceStatus> GetStatus() => Ok(deviceService.GetStatus());

    [HttpGet("xtream/status")]
    [ProducesResponseType<XtreamConnectionStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType<XtreamConnectionStatus>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<XtreamConnectionStatus>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<XtreamConnectionStatus>> GetXtreamStatus(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await xtreamConnectionService.GetStatusAsync(cancellationToken);

            if (!result.Authenticated)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, result);
            }

            if (!result.IsUsable)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }

            return Ok(result);
        }
        catch (XtreamClientException exception)
        {
            return CreateXtreamProblem(exception.Error);
        }
    }

    private ObjectResult CreateXtreamProblem(XtreamClientError error)
    {
        var (statusCode, title, errorCode) = error switch
        {
            XtreamClientError.Configuration => (
                StatusCodes.Status503ServiceUnavailable,
                "Xtream configuration is incomplete.",
                "xtream_configuration_missing"),
            XtreamClientError.Timeout => (
                StatusCodes.Status504GatewayTimeout,
                "The Xtream provider timed out.",
                "xtream_timeout"),
            XtreamClientError.Unavailable => (
                StatusCodes.Status503ServiceUnavailable,
                "The Xtream provider is unavailable.",
                "xtream_unavailable"),
            XtreamClientError.HttpError => (
                StatusCodes.Status502BadGateway,
                "The Xtream provider returned an HTTP error.",
                "xtream_http_error"),
            _ => (
                StatusCodes.Status502BadGateway,
                "The Xtream provider returned an invalid response.",
                "xtream_invalid_response")
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };
        problem.Extensions["code"] = errorCode;

        return StatusCode(statusCode, problem);
    }
}
