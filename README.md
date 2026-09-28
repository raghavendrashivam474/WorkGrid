# WorkGrid

**WorkGrid** is a hybrid, local-first employee and asset management platform designed for mobile-first operational environments. It combines instant offline capabilities and on-device storage with a stateless, secure, and authenticated backend services pipeline.

---

## 📌 Current Status

* **Phase:** 4 — Hybrid Backend & Remote Data
* **Sprint:** S4.1 → S4.4 (Post-Completion)
* **Current Release:** `v4.0` (System Baseline) / `vS4.4` (Technical Build Tag)
* **Status:** Completed — Authenticated Remote Data Pipeline Proven & Integrated (Local-First Offline Core fully preserved).

---

## 🎯 Vision & Approach

WorkGrid is designed for environments where network connectivity is intermittent, expensive, or completely unavailable. 

* **Domain-Driven Design (DDD):** Clean Domain model with zero external technical dependencies.
* **Local-First Architecture:** The mobile client treats local SQLite persistence as its primary, instant, and authoritative source of truth.
* **Hybrid Connectivity:** Rather than direct client-database coupling, remote communication crosses an explicit, secure HTTP boundary using stateless token authentication and a mapped DTO pipeline.

```text
                               WorkGrid
                                  │
                 ┌────────────────┴────────────────┐
                 │                                 │
           Mobile Client                      Backend API
           (WorkGrid.App)                    (WorkGrid.Api)
                 │                                 │
         ┌───────┴───────┐                         │
         │               │                         │
      SQLite       IRemoteClient                   │
   workgrid.db3    (Bearer JWT)                    │
  (Authoritative)        │                         ▼
                         │ HTTP Get       [Authorize] Middleware
                         │ /api/employees          │
                         ▼                         ▼
                    WorkGrid.Api              SQLite DB
                 (Stateless Host)        workgrid_server.db
```

---

## 🌐 Hybrid Topology & Local-First Coexistence

In WorkGrid, local persistence and remote queries operate in parallel without state leakage or blocking operations:

* **Local Authority:** ViewModels load directly from `IEmployeeRepository` backed by on-device SQLite storage (`workgrid.db3`). Local CRUD operations work instantly, even in airplane mode.
* **Remote Query Channel:** High-level abstractions like `IEmployeeRemoteService` allow the client to safely fetch, compare, or stream remote data without bypassing local application rules.
* **Network Fault Isolation:** Network timeouts, domain unreachable states, or server crashes are caught by the `IRemoteClient` transport layer, wrapped in a type-safe `RemoteResult<T>` envelope, and reported to the application without crashing the mobile app or degrading the offline user experience.

### 🔒 Stateless Security Boundary
Authentication between the Client and the API is stateless, secure, and separated from mobile UI sessions:
* **Stateless Tokens:** The server-side API bypasses in-memory session singletons. Valid credentials sent to `POST /api/auth/login` verify against database password hashes (using PBKDF2 with 100,000 iterations) and yield an HMAC-SHA256 signed JSON Web Token (JWT).
* **Claims Propagation:** The token embeds immutable cryptographic claims defining user identity and role-based clearance (`Viewer`, `Manager`, `Admin`).
* **Transport Automation:** On the client side, `IRemoteClient` handles token state management. Calling `SetAuthToken(token)` automatically injects `Authorization: Bearer <token>` headers into every outbound HTTP request. Calling `ClearAuthToken()` safely strips them on logout.

---

## 📁 Repository Structure

```text
WorkGrid/
│
├── src/
│   ├── WorkGrid.Domain/          # Core Domain Entities, Invariants, and ITokenService Contracts
│   ├── WorkGrid.Infrastructure/  # Local SQLite DB (EF Core), JwtTokenService, and Remote Boundary (IRemoteClient)
│   ├── WorkGrid.Api/             # ASP.NET Core Web Host, Controllers, and JWT Middleware
│   └── WorkGrid.App/             # .NET MAUI Mobile Client (Views, Shell, and ViewModels)
│
├── tests/
│   ├── WorkGrid.Domain.Tests/          # Pure Domain entity and validation unit tests
│   ├── WorkGrid.Infrastructure.Tests/  # SQLite repository, hashing, and mock-based RemoteClient tests
│   └── WorkGrid.Api.Tests/             # Controller unit and WebApplicationFactory integration tests
│
├── docs/
│   ├── architecture/             # Architecture overviews (API layer, Remote auth, Vertical slice)
│   ├── decisions/                # Architectural Decision Records (ADRs 0001 - 0009)
│   └── phases/                   # Detailed closeout and milestone post-completion reports
│
├── Directory.Build.props
├── global.json
├── README.md
└── WorkGrid.sln
```

---

## 🗺️ Development Roadmap

| Phase | Milestone | Focus | Status |
| --- | --- | --- | --- |
| **Phase 0** | `v0.0` | Environment setup, DDD patterns, and base solution architecture | **Completed** |
| **Phase 1** | `v1.0` | Local Mobile Core (Local SQLite DB, Employee CRUD, basic ViewModels) | **Completed** |
| **Phase 2** | `v2.0` | Core Business Workflows (Assets, Assignments, Status management) | **Completed** |
| **Phase 3** | `v3.0` | Identity, Security & Hashing (PBKDF2 local validation, Role authorizations) | **Completed** |
| **Phase 4** | `v4.0` / `vS4.4` | Hybrid Backend (ASP.NET Core REST API, JWT stateless auth, Vertical remote slice) | **Completed** |
| **Phase 5** | `v5.0` | Bidirectional Synchronization (Sync protocols, Dirty tracking, Conflict resolution) | *Planned* |
| **Phase 6** | `v6.0` | Deployment & Production Hardening (Cloud setup, Telemetry, UI Polishing) | *Planned* |

---

## 🛠️ Getting Started

### Prerequisites

- .NET 8 SDK (8.0.400 or higher)
- MAUI workload (`dotnet workload install maui-android`)
- Android SDK & Emulators (configured via `ANDROID_HOME` or your IDE)

### 1. Build the Solution

Verify compilation across all layers of the platform:
```bash
dotnet build WorkGrid.sln --nologo
```

### 2. Run the Multi-Tier Test Suite

Run the full, unified test suite containing 120 validated tests spanning the domain, local storage, security hashing, network simulation, and full API integration:
```bash
dotnet test WorkGrid.sln --nologo -v minimum
```

### 3. Run the Backend API Locally

To start the server-side API (which auto-initializes the schema inside `workgrid_server.db` on launch):
```bash
dotnet run --project src/WorkGrid.Api/WorkGrid.Api.csproj
```

Once running, navigate to the local Swagger UI to interact with endpoints and test schema properties:
```text
https://localhost:7061/swagger
```

To call the secure endpoints via terminal, retrieve a bearer token by submitting credentials to the authentication controller:
```bash
# Get Bearer Token via curl
curl -k -X POST "https://localhost:7061/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username": "your_user", "password": "your_password"}'
```