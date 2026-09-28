# Architecture — Remote Authentication (`WorkGrid.Api` & `IRemoteClient`)

## 1. Overview

In Phase 4, WorkGrid adopts a dual authentication model:
1. **Local Single-User Sessions (`WorkGrid.App`):** Managed in-memory by `ISessionService` and local SQLite persistence for instant offline-first operations.
2. **Stateless Remote Authentication (`WorkGrid.Api`):** Managed via standard JSON Web Tokens (JWT) over HTTP Bearer authentication for server-side API access.

```text
┌─────────────────────────────────────────────────────────────────┐
│                          WorkGrid Client                        │
│                                                                 │
│  ┌─────────────────────────┐       ┌─────────────────────────┐  │
│  │     ISessionService     │       │      IRemoteClient      │  │
│  │ (Local Mobile Session)  │       │   (AuthToken Lifecycle) │  │
│  └─────────────────────────┘       └────────────┬────────────┘  │
└─────────────────────────────────────────────────┼───────────────┘
                                                  │
                                                  │ HTTP Bearer Token
                                                  ▼
                                     ┌─────────────────────────┐
                                     │      WorkGrid.Api       │
                                     │  (JWT Authentication)   │
                                     └────────────┬────────────┘
                                                  │
                                                  ▼
                                     ┌─────────────────────────┐
                                     │     IUserRepository     │
                                     │     + PasswordHasher    │
                                     └─────────────────────────┘
```

## 2. Authentication Flow

### A. Login (POST /api/auth/login)

   1. Client sends LoginRequest (Username, Password).
   2. API validates credentials directly via IUserRepository and IPasswordHasher (PBKDF2 with 100,000 iterations).
   3. Inactive accounts are rejected immediately (401 Unauthorized).
   4. If valid, ITokenService.GenerateToken(user) mints a signed HMAC-SHA256 JWT containing claims:
       * ClaimTypes.NameIdentifier (User ID Guid)
       * ClaimTypes.Name (Username)
       * ClaimTypes.Role (User Role string: Admin, Manager, Viewer)

   5. API returns LoginResponse (Token, Username, Role).

### B. Attaching Credentials to Outbound Requests

The client stores the token in `IRemoteClient`:

```csharp
remoteClient.SetAuthToken(loginResponse.Token);
```

`RemoteClient` automatically injects the `Authorization: Bearer <token>` header on all subsequent HTTP calls through its configured `HttpClient`.

### C. Logout & Token Invalidation

```csharp
remoteClient.ClearAuthToken();
```

Clearing the token strips the `Authorization` header from the client pipeline. Future requests against protected endpoints will receive `401 Unauthorized` without crashing the application.

## 3. Error Handling and Status Codes

| Server Outcome | HTTP Status | RemoteResult.ErrorKind | Application Impact |
| --- | --- | --- | --- |
| **Valid Token** | 200 OK | None (Success) | Data retrieved successfully |
| **Missing / Invalid Token** | 401 Unauthorized | `RemoteErrorKind.Unauthorized` | User prompted to log in to remote server |
| **Inactive Account** | 401 Unauthorized | `RemoteErrorKind.Unauthorized` | Remote access denied |
| **Insufficient Role Permission** | 403 Forbidden | `RemoteErrorKind.Unauthorized` | Action forbidden for user's role |
| **Server Connection Down** | N/A (Timeout / Err) | `RemoteErrorKind.Unreachable` | Local-first functionality unaffected |

## 4. Security Principles

1. **Password Hashes Never Leave the Server/Database**: Clients only submit plain text passwords over 
   TLS/HTTPS during login. Database PBKDF2 hashes are never transmitted.
2. **Stateless Scalability**: The server maintains zero session state in RAM for API consumers, 
   avoiding multi-tenant session collisions.
3. **Clean Boundaries**: Domain entities and contracts have no references to System.IdentityModel.Tokens.Jwt 
   or ASP.NET Core libraries.
