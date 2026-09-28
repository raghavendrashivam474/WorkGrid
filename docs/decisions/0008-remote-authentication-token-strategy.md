# ADR-0008: Stateless JWT Bearer Authentication for Remote API

## Status
Accepted (S4.3)

## Context

Phase 3 established a local authentication and authorization system using:
- `IUserRepository` & `IPasswordHasher` (PBKDF2)
- `ISessionService` (an in-memory singleton storing `CurrentUser`)
- `IAuthorizationService` (evaluating permissions against `ISessionService.CurrentUser`)

In `WorkGrid.Api`, handling concurrent remote HTTP requests requires **stateless authentication**. Using the singleton `ISessionService` on the server would cause session bleed/race conditions across multiple API callers.

## Decision

1. **Domain Abstraction:** Created `ITokenService` in `WorkGrid.Domain.Contracts` with `string GenerateToken(User user)`. The Domain remains decoupled from JWT or token formatting specifics.
2. **Stateless Infrastructure Implementation:** Created `JwtTokenService` in `WorkGrid.Infrastructure.Services` using `System.IdentityModel.Tokens.Jwt` with HMAC-SHA256 signing, claims (`NameIdentifier`, `Name`, `Role`), and configurable expiration.
3. **Stateless Server Auth Flow:** `AuthController.Login` validates credentials directly via `IUserRepository` and `IPasswordHasher`, bypassing `ISessionService`, and returns a JWT Bearer token in `LoginResponse`.
4. **Endpoint Security:** Server endpoints (such as `GET /api/employees`) enforce authentication using standard ASP.NET Core `[Authorize]` attributes and JWT Bearer validation middleware.
5. **Remote Client Token Lifecycle:** Extended `IRemoteClient` and `RemoteClient` with `SetAuthToken(string?)`, `ClearAuthToken()`, and `AuthToken` property, automatically attaching `Authorization: Bearer <token>` to outbound HTTP requests and translating `401/403` to `RemoteErrorKind.Unauthorized`.

```text
Client Login Request ──► POST /api/auth/login
                                │
                        Verify Password (PBKDF2)
                                │
                        Generate JWT (JwtTokenService)
                                │
Client receives Token ◄── Return LoginResponse(Token, Role)
         │
         ▼
IRemoteClient.SetAuthToken(token)
         │
         ▼
Outbound Request ──────► GET /api/employees
(Auth: Bearer <token>)           │
                         [Authorize] Middleware
                                 │
                         200 OK with Data
```

## Rationale

1. **Server Statelessness**: Every HTTP request carries its own authentication context in the Bearer token, 
   enabling horizontal scaling without server-side session stores.
2. **Local-First Preservation**: The mobile app's local authentication flow 
   (which uses ISessionService and local SQLite) remains intact and unaffected.
3. **Domain Isolation**: No ASP.NET Core or JWT package references leak into WorkGrid.Domain.
4. **Transport Encapsulation**: The App layer interacts with IRemoteClient.SetAuthToken() and does not 
   manage raw HTTP Authorization headers directly.

## Consequences

- Tokens are signed with a symmetric secret configured via `appsettings.json` (`Jwt:Secret`).
- Token expiration requires client re-authentication. Refresh token machinery is intentionally 
  omitted until concrete requirements dictate it.
- Role-based authorization claims are embedded in the JWT and validated by ASP.NET Core policies.

## Related

- **ADR-0006**: Remote Boundary Foundation (S4.1)
- **ADR-0007**: Server-Side Persistence Strategy (S4.2)
S4.3 Closeout Report
