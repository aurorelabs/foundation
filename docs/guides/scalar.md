# API documentation (Scalar)

Interactive docs on top of ASP.NET Core OpenAPI. Whether they are available is up to the application: the blueprint does not register them in Production.

Code: `src/AspNetCore/OpenApi/Extensions/ScalarExtensions.cs`. The application wires it in its own `ScalarConfiguration` (`ConfigureAuroreScalarDocuments` once, then `AddOpenApi` with the `AddAurore...` transformers per version, and `MapScalarApiReference` with `WithAuroreAuthentication`).

- **One document per version** in the catalog (`v1`, `v2`), named with the same format as API versioning, so documents
  cannot drift from the mapped versions. Each has title, description, maintainer contact, and a notice when deprecated.
- **Security schemes**: JWT bearer, API key, OpenID Connect, OAuth2. The lock icon is decided per operation from endpoint
  metadata: authorized endpoints are locked, `AllowAnonymous` ones are not.
- **Login from the UI**: OAuth2 authorization code + PKCE (SHA-256), client id from `ApiDocs:OAuthClientId`, scopes from
  `IdentityProvider:Scopes`. In the identity provider, create that client as public with PKCE and add the docs page URL as a
  redirect URI.

Describe an endpoint with `Summary`, `Description` and `.ProducesProblem(...)`/`.Produces<T>()`. The theme and the title are set by the application when it maps the Scalar page.
