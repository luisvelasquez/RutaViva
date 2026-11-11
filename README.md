# RutaViva

Sistema de mantenimiento preventivo de flotas. API REST ASP.NET Core (.NET 10), Angular 20 y SQL Server 2022.

Arquitectura limpia + CQRS (MediatR) + DDD, pensada para explicar en entrevista: **Controller → Command → Mediator → Handler → Aggregate Root → Repository**.

## Arranque local (orden recomendado)

### 1. SQL Server

```powershell
Copy-Item .env.example .env
# Edita SQL_PASSWORD en .env
docker compose up -d sqlserver
```

En SSMS / Azure Data Studio / sqlcmd, contra `localhost,1433` (sa + password de `.env`):

1. `backend/database/00_create_database.sql` (conectado a `master`)
2. `backend/database/01_schema.sql` (conectado a `RutaViva`)
3. Opcional: `backend/database/02_seed_demo.sql`

La API **ya no crea ni siembra** la base al arrancar.

### 2. API

Configura la cadena (User Secrets o variable de entorno). No dejes la contraseña en código.

```powershell
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;Database=RutaViva;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True" --project backend/src/RutaViva.Api
dotnet run --project backend/src/RutaViva.Api
```

- API: http://localhost:5000  
- Swagger UI: http://localhost:5000/swagger  
- OpenAPI: http://localhost:5000/openapi/v1.json  
- Salud: http://localhost:5000/health  

### 3. Angular

```powershell
cd frontend
npm ci
npm start
```

Abre http://localhost:4200 (proxy `/api` → `http://localhost:5000`).

### Docker Compose completo

Tras crear esquema/seed sobre el contenedor SQL:

```powershell
docker compose up --build -d
```

- Web: http://localhost:8080  
- API: http://localhost:5000  

## Ruta de sustentación (escritura)

Ejemplo: cotizar una orden.

1. `ServiceOrdersController.QuoteServiceOrder`
2. `QuoteServiceOrderCommand` (+ `IdempotencyContext`)
3. `IMediator.Send` → pipeline `IdempotentCommandBehavior`
4. `QuoteServiceOrderCommandHandler`
5. `ServiceOrder.Quote` (raíz de agregado)
6. `IServiceOrderRepository.SaveServiceOrderAsync` + Outbox + recibo (misma transacción)

## Ruta de sustentación (lectura)

1. `MaintenanceDashboardController.GetMaintenanceDashboard`
2. `GetMaintenanceDashboardQuery`
3. `IMediator.Send`
4. `GetMaintenanceDashboardQueryHandler`
5. `IMaintenanceDashboardReadStore` → `MaintenanceDashboardDto`

## Mapa de raíces y objetos de valor

| Concepto | Ubicación | Protege |
| --- | --- | --- |
| `AggregateRoot` | `Domain/Aggregates` | Id, Version, eventos |
| `Vehicle` | `Aggregates/Vehicles` | Placa, taller, odómetro monótono (`RegisterOdometerReading`) |
| `MaintenanceRoutine` | `Aggregates/MaintenanceRoutines` | Intervalos, estado, aviso único, reinicio |
| `ServiceOrder` | `Aggregates/ServiceOrders` | Cotización y transiciones del flujo |
| `Kilometrage` / `MoneyCop` / `MaintenanceInterval` | `Domain/ValueObjects` | Reglas de valor usadas en el modelo |

Varias raíces se coordinan en Application (p. ej. `CompleteServiceOrderCommandHandler` y `DueMaintenanceCoordinator`) relacionándolas por Id.

## Registro de dependencias

`Program.cs` compone un solo contenedor:

- `AddApi()` — controladores, OpenAPI/Swagger UI, CORS, worker
- `AddApplication()` — MediatR, handlers, coordinador
- `AddInfrastructure()` — SQL, repos, UoW, idempotencia, locks, reloj

Domain no registra DI.

## Reglas del PDF → código → prueba

| Regla del PDF | Clase / método | Prueba | Resultado | Limitación |
| --- | --- | --- | --- | --- |
| Intervalos km/tiempo/ambos; vence el primero | `MaintenanceInterval`, `EvaluateDueStatus` | `DomainTests` | Ejecutada OK | — |
| Aviso último 10 % una vez por ciclo | `MaintenanceRoutine.EvaluateDueStatus` | `Warning_only_once...` | Ejecutada OK | Concurrencia SQL: implementar y ejecutar con `RUTAVIVA_TEST_SQL` |
| Una orden abierta por vehículo+rutina | índice + `HasOpenOrderForRoutineAsync` | `SqlIntegrationTests` | Implementada | Sin ejecutar si no hay SQL |
| Autoaprobación ≤ 1.500.000 COP | `MoneyCop` + `ServiceOrder.Quote` | `Approval_threshold` | Ejecutada OK | — |
| Completar reinicia ciclo | `CompleteService` + `ResetMaintenanceCycle` | dominio + SQL | Dominio OK | SQL opcional |
| Lectura ≤ se ignora | `RegisterOdometerReading` | `Equal_or_old_readings_ignored` | Ejecutada OK | — |

## Pruebas

```powershell
dotnet test backend/RutaViva.sln -c Release
```

Integración SQL:

```powershell
$env:RUTAVIVA_TEST_SQL = 'Server=localhost;Database=master;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet test backend/RutaViva.sln -c Release
```

## Versiones

| Componente | Versión |
| --- | --- |
| .NET | 10 |
| MediatR | 12.5 |
| Dapper | 2.1.66 |
| Microsoft.Data.SqlClient | 6.1.3 |
| Angular | 20.3.x |
| SQL Server | 2022 |

## Documentación adicional

- `docs/ARCHITECTURE.md` — decisiones técnicas
- `docs/CUMPLIMIENTO_RETO.md` — matriz del reto
- `docs/API.md` — contratos HTTP
