# RutaViva

Preventive fleet maintenance. ASP.NET Core (.NET 10) API, Angular 20, SQL Server 2022.

Clean Architecture + CQRS (MediatR) + DDD: **Controller → Request → Command/Query → MediatR → Handler → Aggregate → Repository/ReadStore → DTO**.

## Local startup

### 1. SQL Server

```powershell
Copy-Item .env.example .env
docker compose up -d sqlserver
```

Against `127.0.0.1,1433` (sa + `.env` password):

| Scenario | Scripts |
| --- | --- |
| **New database** | `00_create_database.sql` → `01_schema.sql` → optional `02_seed_demo.sql` |
| **Existing English-short schema** (`Routines`/`Orders`/`Receipts`/`Outbox`) | stop API → `03_migrate_existing_to_english.sql` → restart API |

The API does **not** bootstrap schema or seed data.

### 2. API (port 5000)

```powershell
dotnet user-secrets set "ConnectionStrings:Database" "Server=127.0.0.1;Database=RutaViva;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True" --project backend/src/RutaViva.Api
dotnet run --project backend/src/RutaViva.Api --launch-profile RutaViva.Api
```

- Health: http://localhost:5000/health  
- Swagger: http://localhost:5000/swagger  
- OpenAPI: http://localhost:5000/openapi/v1.json  

### 3. Angular

```powershell
cd frontend
npm ci
npm start
```

Proxy: `/api` → `http://localhost:5000`.

## Final HTTP contract

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/dashboard` | Maintenance summary |
| GET | `/api/vehicles` | List vehicles |
| GET | `/api/vehicles/{vehicleId}` | Read vehicle |
| POST | `/api/vehicles` | Register vehicle (`201` + `Location`) |
| GET | `/api/vehicles/{vehicleId}/routines` | List routines |
| POST | `/api/vehicles/{vehicleId}/routines` | Register routine (`201`) |
| POST | `/api/vehicles/{vehicleId}/readings` | Odometer reading (`accepted` true/false) |
| GET | `/api/orders?vehicleId=&status=` | List orders |
| GET | `/api/orders/{orderId}` | Read order |
| PATCH | `/api/orders/{orderId}` | Lifecycle update (`operation`) |
| POST | `/api/maintenance/evaluate` | Manual evaluation (demo/ops) |
| GET | `/health` | Readiness |

PATCH body uses `operation`: `quote` | `approve` | `reject` | `cancel` | `start` | `complete`. Quote requires `amount`. Complete requires `kilometres` + `date`. All writes need `Idempotency-Key`.

Old `POST /api/orders/{id}/quote|approve|...` routes were removed after updating the Angular client.

## Table / column rename map

| Previous | Current |
| --- | --- |
| `dbo.Vehicles.Id/Plate/Workshop/Odometer` | `VehicleId` / `LicensePlate` / `AssignedWorkshop` / `OdometerKilometers` |
| `dbo.Routines` | `dbo.MaintenanceRoutines` (+ `MaintenanceRoutineId`, `IntervalKilometers`, `LastServiceOdometerKilometers`, `MaintenanceCycleId`, `DueSoonNoticeRaised`, `IsCycleCancelled`, …) |
| `dbo.Orders` | `dbo.ServiceOrders` (`ServiceOrderId`, `MaintenanceRoutineId`, `Status`, `QuoteAmountCop`, `ActualServiceOdometerKilometers`, `ActualServiceDate`, …) |
| `dbo.Receipts` | `dbo.IdempotencyRecords` (`IdempotencyKey`, `RequestFingerprint`, `StoredResponse`) |
| `dbo.Outbox` | `dbo.OutboxMessages` (`OutboxMessageId`, `EventType`, `Payload`, `MaintenanceCycleId`) |

**No business columns removed.** `Version`, cycle ids, due-soon / cancel flags, workshop on the order, fingerprints, stored responses and `PublishedAt` were kept. `IntervalDays` remains calendar days (not months). Outbox `EventType` values (`MaintenanceDueSoon`, …) unchanged for pending messages and UI.

## Dapper models

Folder `Infrastructure/Persistence/Models` (was `Rows`):

| Model | Role |
| --- | --- |
| `VehiclePersistenceModel` | SQL → repository reconstitutes `Vehicle` |
| `MaintenanceRoutinePersistenceModel` | SQL → `MaintenanceRoutine` |
| `ServiceOrderPersistenceModel` | SQL → `ServiceOrder` |
| `OutboxMessagePersistenceModel` | Outbox read/publish |
| `IdempotencyRecordPersistenceModel` | Idempotency receipt |

API JSON DTOs keep English camelCase names consumed by Angular (`plate`, `workshop`, `odometer`, `state`, …).

## Tests

```powershell
dotnet test backend/RutaViva.sln -c Release
$env:RUTAVIVA_TEST_SQL = 'Server=127.0.0.1,1433;Database=master;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=True'
dotnet test backend/RutaViva.sln -c Release
```

Fresh test databases apply `01_schema.sql` only (final names).

## Sustentation walkthrough

**Write (complete order):** `ServiceOrdersController.ApplyServiceOrderUpdate` → `ApplyServiceOrderUpdateRequest` → `CompleteServiceOrderCommand` → MediatR → handler → `ServiceOrder.CompleteServiceOrder` + `MaintenanceRoutine.ResetMaintenanceCycle` → repositories → `ServiceOrderDto`.

**Read (dashboard):** `MaintenanceOverviewController` → `GetMaintenanceOverviewQuery` → reader → DTOs (no aggregates).

**Layers:** `Vehicle` (domain rules) ≠ `VehiclePersistenceModel` (Dapper) ≠ `VehicleRepository` (rebuilds aggregate) ≠ `VehicleDto` (API).
