# RutaViva

Sistema de mantenimiento preventivo de flotas. API REST ASP.NET Core (.NET 10), Angular 20 y SQL Server 2022.

Arquitectura limpia + CQRS (MediatR) + DDD: **Controller → Request → Command → MediatR → Handler → Aggregate Root → Repository → DTO**.

## Arranque local (orden recomendado)

### 1. SQL Server (Docker)

```powershell
Copy-Item .env.example .env
# Edita SQL_PASSWORD en .env
docker compose up -d sqlserver
```

En SSMS / Azure Data Studio / sqlcmd, contra `localhost,1433` (sa + password de `.env`):

1. `backend/database/00_create_database.sql` (conectado a `master`)
2. `backend/database/01_schema.sql` (conectado a `RutaViva`)
3. Opcional: `backend/database/02_seed_demo.sql`

La API **no** crea la base, no ejecuta esquema ni carga demo al arrancar.

### 2. API (puerto 5000)

```powershell
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;Database=RutaViva;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True" --project backend/src/RutaViva.Api
dotnet run --project backend/src/RutaViva.Api --launch-profile RutaViva.Api
```

| Recurso | URL |
| --- | --- |
| API | http://localhost:5000 |
| Salud | http://localhost:5000/health |
| Swagger UI | http://localhost:5000/swagger |
| OpenAPI | http://localhost:5000/openapi/v1.json |

### 3. Angular

```powershell
cd frontend
npm ci
npm start
```

http://localhost:4200 — proxy `/api` → `http://localhost:5000` (`frontend/proxy.conf.json`).

## Recorridos de sustentación (archivos reales)

### Escribir: completar una orden

1. `Api/Controllers/ServiceOrdersController.cs` → `CompleteServiceOrder`
2. `Api/Requests/ServiceOrders/CompleteServiceOrderRequest.cs`
3. `Application/ServiceOrders/Commands/CompleteServiceOrderCommand.cs` + `IdempotencyContext`
4. `IMediator.Send` → `Messaging/Behaviors/IdempotentCommandBehavior.cs`
5. `Application/ServiceOrders/Handlers/CompleteServiceOrderCommandHandler.cs` (adquiere `vehicle:{id}` y `order:{id}` antes de recargar)
6. `Domain/Aggregates/ServiceOrders/ServiceOrder.CompleteService` + `MaintenanceRoutine.ResetMaintenanceCycle` + `Vehicle.RegisterOdometerReading`
7. `Infrastructure/.../ServiceOrderRepository` / rutinas / vehículos + Outbox (misma UoW)
8. `Application/ServiceOrders/Dtos/ServiceOrderDto.cs`

### Leer: consultar mantenimiento

1. `Api/Controllers/MaintenanceOverviewController.cs` → `GetMaintenanceDashboard` (`GET /api/dashboard`)
2. `Application/MaintenanceOverview/Queries/GetMaintenanceOverviewQuery.cs`
3. `IMediator.Send`
4. `Application/MaintenanceOverview/Handlers/GetMaintenanceOverviewQueryHandler.cs`
5. `IMaintenanceOverviewReader` → `Infrastructure/.../MaintenanceOverviewReader.cs` (proyecta filas SQL → DTO)
6. `MaintenanceOverviewDto` (Vehicles, Routines, Orders, Events, Today)

### Capas de modelo (mismo concepto, tres tipos)

| Tipo | Capa | Rol |
| --- | --- | --- |
| `ServiceOrderRow` | Infrastructure | Resultado Dapper / columnas SQL |
| `ServiceOrder` | Domain | Raíz de agregado; se reconstruye en el repositorio |
| `ServiceOrderDto` | Application | Salida de comandos/consultas hacia la API |

La consulta del dashboard proyecta filas → DTO **sin** pasar por el agregado. Las escrituras reconstruyen el agregado en el repositorio y mapean a DTO al final del handler.

## Registro de dependencias

`Program.cs` compone un solo contenedor:

- `AddApi()` — controladores, OpenAPI/Swagger UI, CORS, `DailyMaintenanceWorker`, `OutboxPublisherWorker`
- `AddApplication()` — MediatR, behaviors, `DueMaintenanceCoordinator`
- `AddInfrastructure()` — SQL, repos, UoW, idempotencia, locks, Outbox writer/publisher, reloj

Domain no registra DI.

## Capas DDD (resumen)

| Tipo concreto | Categoría | Por qué | Regla / hecho | Dónde se usa |
| --- | --- | --- | --- | --- |
| `AggregateRoot` | Clase base abstracta | Id, Version, eventos | Concurrencia optimista + Outbox | Todas las raíces |
| `Vehicle` | Raíz | Identidad, taller, odómetro monótono | Lectura ≤ se ignora | Lecturas / completar |
| `MaintenanceRoutine` | Raíz | Intervalo, vencimiento, aviso único, reinicio | 10 % / ciclo / reset | Evaluación / completar |
| `ServiceOrder` | Raíz | Cotización y transiciones | Autoaprobación ≤ 1.500.000 COP | Handlers de orden |
| `Kilometres` | VO | Odómetro o distancia; ≥0, 2 decimales | Comparación por valor | Vehicle, Routine, Order |
| `MaintenanceInterval` | VO | km, días o ambos; vence el primero | Umbral 10 % | Routine |
| `MoneyCop` | VO clase | Importe COP válido (no `default`) | Autoaprobación vive en la orden | `ServiceOrder.Quote` |
| `DomainEvent` + factory | Hecho | Type/Message persistibles | DueSoon / Created / Completed / StateChanged | Outbox |
| `DueMaintenanceCoordinator` | Application | Coordina repos + raíces + Outbox | Crear orden al vencer | Lectura / evaluación |
| `ServiceOrderDto` / `MaintenanceRoutineDto` | DTO | Transporte Application→API | Sin reglas | Controllers / overview |
| `ServiceOrderRow` etc. | Modelo SQL | Materialización Dapper | Columnas/alias | Repos / queries |

No hay servicio de dominio: las reglas viven en raíces/VO; la coordinación multi-raíz está en Application.

## Interpretaciones del PDF

| Tema | Decisión |
| --- | --- |
| Meses vs días | El modelo usa **días** (`IntervalDays`). Un “mes” del enunciado se interpreta como días calendario configurados en la rutina. |
| Salto directo a vencida | No se emite aviso `MaintenanceDueSoon` tardío; solo orden al vencer. |
| Fuera de alcance | Auth, pagos, agenda de taller, notificaciones reales (basta Outbox/evento). |
| `vehicle-management` | No disponible en esta máquina; se adaptó solo `businessname-down-payment` (controllers, OpenAPI/Swagger, DI por capa). Credenciales/negocio de empresa no se copiaron. |

## Cumplimiento del reto (matriz)

| Regla del PDF | Cumple | Archivo / método | Prueba | Resultado / pendiente |
| --- | --- | --- | --- | --- |
| RN intervalos km/tiempo/ambos | Cumple | `MaintenanceInterval`, `EvaluateDueStatus` | `DomainTests` | Dominio OK |
| Aviso último 10 % una vez/ciclo | Cumple | `MaintenanceRoutine.EvaluateDueStatus`, `UX_Outbox_NoticeCycle` | dominio + SQL concurrente | Dominio OK; SQL con `RUTAVIVA_TEST_SQL` |
| Una orden abierta vehículo+rutina | Cumple | `UX_Orders_OpenRoutine`, `InsertServiceOrderAsync` | SQL unicidad | Índice + Conflict esperado |
| Autoaprobación ≤ 1.500.000 COP | Cumple | `ServiceOrder.AutomaticApprovalCeilingCop` | `DomainTests` | Dominio OK |
| Completar reinicia ciclo | Cumple | `CompleteService` + `ResetMaintenanceCycle` | SQL replay | Conserva CycleId en replay |
| Lectura ≤ se ignora | Cumple | `Vehicle.RegisterOdometerReading` | dominio | OK |
| Interfaz Angular + API | Cumple | `frontend/`, `RutaViva.Api` | build / proxy | Proxy a :5000 |
| Idempotency-Key obligatorio | Cumple | `IdempotencyHttp`, OpenAPI Required | `HttpIdempotencyTests` | 422 sin cabecera |
| Concurrencia lecturas + daily | Cumple | locks en handlers MediatR | SQL concurrente | Lock `vehicle:{id}` en handler |

## Pruebas

```powershell
dotnet test backend/RutaViva.sln -c Release
```

Integración SQL:

```powershell
$env:RUTAVIVA_TEST_SQL = 'Server=localhost;Database=master;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet test backend/RutaViva.sln -c Release
```

## Convenciones adaptadas de la referencia

Desde `C:\repo\businessname-down-payment` (solo lectura): OpenAPI + Swagger UI en `/swagger`, DI por proyecto (`AddApi`/`AddApplication`/`AddInfrastructure`), conexión SQL vía factory/configuración. No se importaron secretos ni código de negocio Satrack.

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

- `docs/ARCHITECTURE.md`
- `docs/CUMPLIMIENTO_RETO.md`
- `docs/API.md`
