using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace CareAI.API.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("/error")]
        public IActionResult HandleError()
        {
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            _logger.LogError(exception, "An unhandled exception occurred");

            var problemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred!",
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception?.Message,
                Instance = HttpContext.Request.Path
            };

            if (exception != null)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                problemDetails.Extensions["source"] = exception.Source;
            }

            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }

        [Route("/error/{statusCode}")]
        public IActionResult HandleErrorCode(int statusCode)
        {
            var statusCodeData = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            switch (statusCode)
            {
                case 404:
                    return NotFound(new ProblemDetails
                    {
                        Title = "Resource not found",
                        Status = StatusCodes.Status404NotFound,
                        Detail = $"The requested resource {statusCodeData?.OriginalPath} was not found",
                        Instance = HttpContext.Request.Path
                    });

                case 401:
                    return Unauthorized(new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = "You are not authorized to access this resource",
                        Instance = HttpContext.Request.Path
                    });

                case 403:
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "Forbidden",
                        Status = StatusCodes.Status403Forbidden,
                        Detail = "You don't have permission to access this resource",
                        Instance = HttpContext.Request.Path
                    });

                default:
                    return StatusCode(statusCode, new ProblemDetails
                    {
                        Title = "An error occurred",
                        Status = statusCode,
                        Detail = $"An error occurred with status code: {statusCode}",
                        Instance = HttpContext.Request.Path
                    });
            }
        }
    }
}
