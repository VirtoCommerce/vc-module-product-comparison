using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// The two auth endpoints a signed-in storefront calls on its own: the refresh grant of /connect/token
/// (form-encoded, as useAuth.ts posts it) and /revoke/token. Not an OAuth server: tests sign buyers in with a token
/// they mint (<see cref="TestTokenService"/>), not through the sign-in page.
/// </summary>
internal static class TestTokenEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/connect/token", async (HttpRequest request, TestTokenService tokens) =>
        {
            var form = await request.ReadFormAsync();

            var token = form["grant_type"].ToString() == "refresh_token"
                ? await tokens.RefreshAsync(form["refresh_token"].ToString())
                : null;

            return token == null
                ? Results.BadRequest(new { error = "invalid_grant" })
                : Results.Json(new { access_token = token.AccessToken, refresh_token = token.RefreshToken, token_type = "Bearer", expires_in = token.ExpiresIn });
        });

        app.MapPost("/revoke/token", () => Results.Ok());
    }
}
