using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using CustomerSecurity = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.ProductComparison.Tests.StorefrontE2E.Infrastructure;

/// <summary>
/// Mints the bearer token the storefront keeps in localStorage.auth, for a buyer of the harness's
/// <see cref="TestUsers"/>. The claims are the ones the platform's /connect/token puts in a customer's token and the
/// compare list reads: the account id (<c>sub</c>), its name and email, and the organization the buyer signed in
/// with, when any. Only the signature is test-owned (a symmetric key the host validates).
/// </summary>
internal sealed class TestTokenService(TestUsers users)
{
    public const string Issuer = "http://product-comparison-storefront-e2e/";
    public const string Audience = "resource_server";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public static readonly SymmetricSecurityKey SigningKey =
        new(SHA256.HashData(Encoding.UTF8.GetBytes("VirtoCommerce.ProductComparison storefront E2E signing key")));

    private readonly ConcurrentDictionary<string, (string UserId, string OrganizationId)> _refreshTokens = new();

    /// <summary>A token for an existing account, as a test signs a browser in without the sign-in page.</summary>
    public TokenResponse Issue(string userId, string organizationId = null)
    {
        var user = users.Find(userId) ?? throw new InvalidOperationException($"No account with id '{userId}'.");

        var claims = new List<Claim>
        {
            new(OpenIddictConstants.Claims.Subject, user.Id),
            new(OpenIddictConstants.Claims.Name, user.UserName),
            new(OpenIddictConstants.Claims.Email, user.Email),
        };

        if (!string.IsNullOrEmpty(organizationId))
        {
            claims.Add(new Claim(CustomerSecurity.Claims.OrganizationId, organizationId));
        }

        var now = DateTime.UtcNow;
        var accessToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(Lifetime),
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });

        var refreshToken = Guid.NewGuid().ToString("N");
        _refreshTokens[refreshToken] = (user.Id, organizationId);

        return new TokenResponse(accessToken, refreshToken, (int)Lifetime.TotalSeconds);
    }

    /// <summary>The refresh grant: a new token for the account the refresh token was issued to, once.</summary>
    public TokenResponse Refresh(string refreshToken)
        => _refreshTokens.TryRemove(refreshToken ?? string.Empty, out var owner) ? Issue(owner.UserId, owner.OrganizationId) : null;
}
