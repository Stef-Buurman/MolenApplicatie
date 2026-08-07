using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace MolenApplicatie.Server.Filters;

public sealed class HangfireDashboardBasicAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireDashboardBasicAuthFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!TryGetCredentials(httpContext, out var username, out var password))
        {
            SetAuthenticationChallenge(httpContext);
            return false;
        }

        var isAuthorized = FixedTimeEquals(username, _username) && FixedTimeEquals(password, _password);
        if (!isAuthorized) SetAuthenticationChallenge(httpContext);
        return isAuthorized;
    }

    private static bool TryGetCredentials(HttpContext httpContext, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        var authorizationHeader =
            httpContext.Request.Headers.Authorization.ToString();

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var authenticationHeader)
            || !string.Equals(authenticationHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authenticationHeader.Parameter))
        {
            return false;
        }

        string decodedCredentials;

        try
        {
            var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter);
            decodedCredentials = Encoding.UTF8.GetString(credentialBytes);
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex <= 0) return false;

        username = decodedCredentials[..separatorIndex];
        password = decodedCredentials[(separatorIndex + 1)..];

        return true;
    }

    private static bool FixedTimeEquals(string suppliedValue, string configuredValue)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedValue);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredValue);
        return suppliedBytes.Length == configuredBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
    }

    private static void SetAuthenticationChallenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\", charset=\"UTF-8\"";
    }
}