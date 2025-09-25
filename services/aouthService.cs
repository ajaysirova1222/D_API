//Handlers / CustomJwtAuthenticationHandler.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

public class SourceJwtAuthenticationSchemeOptions:AuthenticationSchemeOptions
{
    public string SecretKey { get; set; }
    public string ValidIssuer { get; set; }
    public string ValidAudience { get; set; }
    public string Subject { get; set; }

}

public class SourceJwtAuthenticationHandler : AuthenticationHandler<SourceJwtAuthenticationSchemeOptions>
{

    public SourceJwtAuthenticationHandler(
        IOptionsMonitor<SourceJwtAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {

    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        //encryption.CalculateSHA256Hash(req.addInfo["guid"].ToString())
        // check if headers have both bearer and guid
        if (!Request.Headers.ContainsKey("Authorization") || !Request.Headers.ContainsKey("guid"))
        {
            return AuthenticateResult.Fail("Authorization header not found.");
        }


        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid Authorization header.");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        //var payload = token.Split('.')[0];
        var hashGUID = cf.CalculateSHA256Hash(Request.Headers["guid"].ToString());
        string jwtPayload = Encoding.UTF8.GetString(Convert.FromBase64String(cf.Base64UrlDecode(token.Split('.')[1])));
        var jwt = JsonSerializer.Deserialize<Dictionary<string, object>>(jwtPayload);

        if (jwt["guid"].ToString() != hashGUID)
        {
            return AuthenticateResult.Fail("Invalid GUID when Authenticating.");
        }

        try
        {
            var principal = ValidateJWT(token);
            if (principal == null)
            {
                return AuthenticateResult.Fail("Invalid token.");
            }

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
        }
    }

    public ClaimsPrincipal ValidateJWT(string token)
    {
        //Options.SecretKey
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(Options.SecretKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Options.ValidIssuer,
            ValidAudience = Options.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}