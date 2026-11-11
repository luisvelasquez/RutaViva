# Decisiones de arquitectura

## Referencias adaptadas

De `C:\repo\businessname-down-payment` (solo lectura):

| Archivo de referencia | Qué se adaptó |
| --- | --- |
| `...Manager.Api/Controllers/DownPaymentController.cs` | Controladores delgados + `IMediator` |
| `...Application/DependencyContainer.cs` | MediatR registrado en Application |
| `...Manager.Api/Extensions/SwaggerExtension.cs` + `WebApplicationExtension.cs` | OpenAPI + Swagger UI en `/swagger` |
| `...Manager.Api/Properties/launchSettings.json` | Perfil local con `launchUrl: swagger` |
| `...Infrastructure/Repositories/Base/SqlServer/SqlServerBase.cs` | Idea de base SQL compartida → `SqlRepositoryBase` (sin Polly/credenciales de empresa) |

`vehicle-management` no estaba disponible en este entorno.

## Capas

- **Api**: Controllers, Contracts HTTP, Configuration, DI, worker.
- **Application**: Commands/Queries por funcionalidad, MediatR, contratos de persistencia/idempotencia.
- **Domain**: Aggregates + ValueObjects + Events.
- **Infrastructure**: Connections, Transactions, Repositories, Queries `.sql`, Idempotency, Concurrency.

## CQRS visible

- Escrituras: `IIdempotentCommand<T>` + handler + `IdempotentCommandBehavior`.
- Lecturas: `IQuery<T>` sin idempotencia.
- Un solo mediador: **MediatR** (alineado al proyecto de referencia).

## Persistencia

- Escrituras: `SqlUnitOfWork` comparte conexión/transacción entre repos del mismo scope.
- Lecturas: `MaintenanceDashboardReadStore` abre y libera su propia conexión.
- La API no inicializa esquema ni seed; scripts en `backend/database/00_*.sql` … `02_*.sql`.
- Pruebas de integración preparan la base con `TestDatabase` + `01_schema.sql`.

## Idempotencia vs concurrencia

- Idempotencia: recibo + huella dentro de la transacción (`IIdempotentExecutor`).
- Concurrencia: `IApplicationLock` (sp_getapplock), `Version`, índices únicos filtrados.
