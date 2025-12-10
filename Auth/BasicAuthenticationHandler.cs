using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web; // მხოლოდ ერთხელ
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AssetManagementApi.Auth;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out StringValues authHeaderValues))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

        var authHeader = AuthenticationHeaderValue.Parse(authHeaderValues.FirstOrDefault() ?? string.Empty);
        if (authHeader.Parameter is null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

        try
        {
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];

            // შეცვალე ეს ლოგიკა AppUsers-თან კავშირისთვის
            if (username == "admin" && password == "12345") // დროებითი
            {
                var claims = new[] {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }
    }
}