# Developer Guide — Running and Testing `WorkGrid.Api`

## 1. Overview

`WorkGrid.Api` provides the HTTP/REST backend service for WorkGrid. It uses ASP.NET Core 8.0 with SQLite server persistence.

## 2. Running Locally

### Via CLI:
```bash
dotnet run --project src/WorkGrid.Api/WorkGrid.Api.csproj
```

By default, the server runs on:

- HTTPS: `https://localhost:7061` (or configured port in launchSettings.json)
- HTTP: `http://localhost:5164`

### Swagger UI

In development mode, open a browser and navigate to:

```text
https://localhost:7061/swagger
```

## 3. Testing Endpoints

### 1. Get All Employees

```PowerShell
# PowerShell
Invoke-RestMethod -Uri "https://localhost:7061/api/employees" -Method Get
-SkipCertificateCheck
```

```Bash
# curl
curl -k -X GET "https://localhost:7061/api/employees" -H "Accept: application/json"
```

### Expected response (200 OK):

```JSON
[
  {
    "id": "e6a2b8e3-...",
    "employeeCode": "EMP-001",
    "name": "Jane Doe",
    "email": "jane.doe@workgrid.com",
    "department": "Engineering"
  }
]
```

## 4. Running Automated API Tests

All unit and web integration tests are located in `tests/WorkGrid.Api.Tests`.

#### To run only API tests:

```Bash
dotnet test tests/WorkGrid.Api.Tests/WorkGrid.Api.Tests.csproj --nologo -v normal
```

#### To run all solution tests:

```Bash
dotnet test WorkGrid.sln --nologo
```

## 5. Persistence Behavior in Development

- The API uses `workgrid_server.db` created in the executable base directory.
- `EnsureCreated()` initializes the SQLite database schema automatically on startup 
  if it does not already exist.
- Integration tests use isolated, transient SQLite database files 
  (`integration_test_*.db`)    that are cleaned up during fixture disposal.
