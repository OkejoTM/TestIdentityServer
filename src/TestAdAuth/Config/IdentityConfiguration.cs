using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace TestAdAuth.Config;

public static class IdentityConfiguration
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("checkMate", "Check Mate Api", new List<string> { "role" })
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Machine to machine client
        new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "checkMate" }
        },
        // Interactive client using resource owner password
        new Client
        {
            ClientId = "mobile",
            ClientSecrets = { new Secret("mobile-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "checkMate"
            },
            AlwaysIncludeUserClaimsInIdToken = true,
            AllowOfflineAccess = true,
            RefreshTokenUsage = TokenUsage.ReUse
        }
    ];

    public static IEnumerable<ApiResource> ApiResources =>
        new[]
        {
            new ApiResource("checkMate", "Check Mate Api")
            {
                Scopes = { "checkMate" },
                UserClaims = { "role" }
            }
        };
}