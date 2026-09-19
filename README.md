# WorkGrid

**WorkGrid** is a hybrid, local-first employee and asset management platform designed for mobile-first operational environments.

---

## 📌 Current Status

  * **Phase:** 0 — Foundation
  * **Sprint:** S0.2 & S0.3 — Architecture & Domain Foundation (Complete)
  * **Target Release:** v0.0
  * **Status:** Completed — Stable Foundation Established

---

## 🎯 Vision & Approach

* **Domain:** Employee & Asset Management
* **Primary Platform:** Mobile (.NET MAUI, Android initial target)
* **Design Philosophy:** Local-first (offline capable with SQLite local persistence)
* **Future Architecture:** Hybrid synchronization with an ASP.NET Core backend

```text
                    WorkGrid
                       │
              ┌────────┴────────┐
              │                 │
        Mobile Client       Backend API
        .NET MAUI           ASP.NET Core
              │                 │
           SQLite          Server Database
              │                 │
              └────── Sync ─────┘
```

## 📁 Repository Structure

```text
WorkGrid/
│
├── src/
│   ├── WorkGrid.App/             # .NET MAUI mobile application
│   ├── WorkGrid.Domain/          # Core business entities and contracts
│   ├── WorkGrid.Infrastructure/  # Future persistence, SQLite & technical implementation
│   └── WorkGrid.Api/             # Future ASP.NET Core backend API
│
├── tests/
│   ├── WorkGrid.Domain.Tests/          # Domain layer unit tests
│   ├── WorkGrid.Infrastructure.Tests/  # Infrastructure layer unit tests
│   └── WorkGrid.Api.Tests/             # API layer unit tests
│
├── docs/
│   ├── architecture/             # Architecture overview and design
│   ├── decisions/                # Architectural Decision Records (ADRs)
│   └── development/              # Conventions, setup, and workflows
│
├── .gitignore
├── Directory.Build.props
├── global.json
├── README.md
└── WorkGrid.sln
```

## 🗺️ Development Roadmap

| Phase | Milestone | Focus | Status |
| --- | --- | --- | --- |
| **Phase 0** | v0.0 | Foundation (Repository, Architecture & Domain scaffolding) | Active |
| **Phase 1** | v1.0 | Local Mobile Core (SQLite, Local CRUD, Base UI) | Planned |
| **Phase 2** | v2.0 | Business Workflows (Assignments, Asset Tracking) | Planned |
| **Phase 3** | v3.0 | Identity & Quality (Validation, Security, Hardening) | Planned |
| **Phase 4** | v4.0 | Hybrid Backend (ASP.NET Core REST API, Server DB) | Planned |
| **Phase 5** | v5.0 | Synchronization (Bidirectional sync, Conflict resolution) | Planned |
| **Phase 6** | v6.0 | Production & Portfolio (Polishing, Deployment, Telemetry) | Planned |

## 🛠️ Getting Started

### Prerequisites

- .NET 8 SDK (8.0.425 or compatible)
- MAUI workload (dotnet workload install maui-android)
- Android SDK (configured via ANDROID_HOME)

### Build & Run Tests

```Bash
# Build the entire solution
dotnet build

# Run all test projects
dotnet test
```
