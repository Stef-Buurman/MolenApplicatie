using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MolenApplicatie.Server.Models;

namespace MolenApplicatie.Server.Filters
{
    public class FileUploadFilter : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
        {
            var context = actionContext.HttpContext;
            var fileUploadOptions = context.RequestServices.GetRequiredService<IOptions<FileUploadOptions>>().Value;

            if (string.IsNullOrWhiteSpace(fileUploadOptions.Authorization))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("File upload authorization has not been configured.");
                return;
            }

            var authorizationHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                context.Response.Headers.WWWAuthenticate = "Bearer";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            const string bearerPrefix = "Bearer ";
            var providedAuthorization =
                authorizationHeader.StartsWith(bearerPrefix,
                    StringComparison.OrdinalIgnoreCase)
                    ? authorizationHeader[bearerPrefix.Length..].Trim()
                    : authorizationHeader.Trim();

            if (!SecureEquals(providedAuthorization, fileUploadOptions.Authorization.Trim()))
            {
                context.Response.Headers.WWWAuthenticate = "Bearer";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next();
        }

        private static bool SecureEquals(string providedValue, string configuredValue)
        {
            var providedBytes = Encoding.UTF8.GetBytes(providedValue);
            var configuredBytes = Encoding.UTF8.GetBytes(configuredValue);
            if (providedBytes.Length != configuredBytes.Length) return false;

            return CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
        }
    }
}