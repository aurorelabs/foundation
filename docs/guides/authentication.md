# Authentication and authorization

Callers authenticate with a **JWT Bearer token** from one identity provider, and with an **API key** when clients are
configured. With both on, every `RequireAuthorization()` accepts either kind of caller.

```csharp
services.AddAuroreAuthConfiguration(appSettings.IdentityProvider, appSettings.ApiKeys);
```

## Identity provider

```json
"IdentityProvider": {
  "AuthorityUrl": "http://auth.localhost:9999/realms/aurorelabs",
  "Audience": "api",
  "Provider": "Keycloak",
  "ClockSkewSeconds": 30,
  "RequireHttpsMetadata": false,
  "Scopes": { "api.read": "Read access to the API" }
}
```

- One provider per service. `Provider` is `Keycloak` or `Generic`; it decides where the roles come from (below).
- `ClockSkewSeconds` is how much clock difference between the provider and the service is tolerated (default 30, valid 0 to
  300). The framework default is 5 minutes, which lets expired tokens through for too long.
- Production refuses a local or plain-http authority and `RequireHttpsMetadata: false` at startup.

## Roles and scopes

Whatever the provider writes, the application sees **standard role claims** and a `scope` claim, so endpoints never depend on
the provider.

- **Keycloak:** realm roles (`realm_access.roles`) and the roles of the client named by `Audience` (`resource_access.<audience>.roles`).
  Roles of other clients are not granted.
- **Generic:** the `role` and `roles` claims. Keycloak's JSON claims are ignored, so a look-alike claim grants nothing.
- Scopes are the space-separated `scope` claim (or `scp`).

The helpers, on any endpoint or group:

```csharp
.RequireRoles("admin")                // every role listed
.RequireAnyRole("admin", "support")   // at least one
.RequireScopes("api.read", "api.write")
.RequireAnyScope("api.read", "api.write")
```

In code: `user.HasRole("admin")`, `user.HasScope("api.read")`, `user.GetRoles()`, `user.GetScopes()` (compared exactly, case matters).

## API keys

```json
"ApiKeys": {
  "HeaderName": "X-Api-Key",
  "Clients": { "billing-service": { "Key": "<secret>", "Roles": ["reader"], "Scopes": ["api.read"] } }
}
```

The client name becomes the caller's identity. Keys are compared in constant time. Production requires keys of at least 32
characters and unique per client. Supply them through the environment (`ApiKeys__Clients__billing-service__Key`).

## Pinning a scheme

Without pinning an endpoint accepts both. To accept only one:

```csharp
.RequireAuthorization(policy => policy.AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme).RequireAuthenticatedUser())
```

The OpenAPI document follows: an endpoint pinned to the API key lists only the API key, one pinned to bearer lists the token
schemes, and one that pins nothing lists all of them.

## Tested against Keycloak

The application that uses the package checks it against a real Keycloak (a realm with a reader and an admin user, started with
docker compose in its own tests), so the roles, scopes and subject of real tokens are what the package maps; see [testing](../instructions/testing.md).
