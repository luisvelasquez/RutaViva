# Decisiones de arquitectura

## Organización y dependencias

Frontend Angular y API ASP.NET Core son aplicaciones independientes. El backend tiene un único despliegue y SQL Server. Capas:

- **Domain**: agregados, VOs, invariantes, eventos.
- **Application**: casos de uso por funcionalidad (`Vehicles`, `Routines`, `Maintenance`, `Orders`) y contratos.
- **Infrastructure**: Dapper, consultas `.sql`, repositorios por agregado, UoW, idempotencia, outbox.
- **Api**: HTTP, Problem Details, worker diario/publicador, composición DI.

Domain no depende de Dapper, SQL ni HTTP. No hay microservicios: completar una orden, reiniciar la rutina, outbox y recibo deben ser atómicos.

## Agregados y glosario

| Negocio | Código | Comportamiento |
| --- | --- | --- |
| Vehículo | `Vehicle` | `RegisterReading` |
| Rutina | `MaintenanceRoutine` | `EvaluateDue`, `ResetCycle`, `SuppressCancelledCycle` |
| Orden | `ServiceOrder` | `Quote`, `Approve`, `RejectQuote`, `Cancel`, `StartService`, `CompleteService` |
| Kilometraje | `Kilometrage` | VO usado como `Odometer` / bases de ciclo |
| Dinero COP | `MoneyCop` | VO + techo de autoaprobación |
| Intervalo | `MaintenanceInterval` | km, días o ambos |

Reconstitución: `Reconstitute(...)` sin eventos de creación ni setters públicos de negocio. Persistencia usa filas (`VehicleRow`, …) y mapeo explícito.

## Persistencia (sin mini-ORM)

Se eliminó el antiguo `SqlSession` con diccionarios de agregados y guardado automático.

- `SqlUnitOfWork` administra conexión/transacción/commit/rollback.
- Cada repositorio ejecuta SQL explícito leído por `SqlQueryCatalog` desde archivos embebidos.
- Optimistic concurrency: `UPDATE … WHERE Id=@Id AND Version=@ExpectedVersion`.
- Unicidad de orden abierta: índice filtrado `UX_Orders_OpenRoutine`.
- Un aviso por ciclo: índice filtrado `UX_Outbox_NoticeCycle`.

## Idempotencia vs concurrencia

**Idempotencia HTTP:** header `Idempotency-Key`, ámbito = ruta, huella SHA-256 del body. Recibo en la misma transacción. Éxitos únicamente; fallos no se marcan como exitosos.

**Concurrencia:** distinta de idempotencia. Locks de aplicación:

1. `idem:{hash(scope|key)}`
2. Recursos de negocio ordenados (`vehicle:{guid}`, `order:{guid}`)

La revisión diaria pagina vehículos (50) y adquiere lock por vehículo dentro de la transacción del comando diario. No se usa un lock global de toda la flota ni `SemaphoreSlim` como garantía.

## Eventos

Los agregados acumulan `DomainEvent` y los casos de uso los vuelcan al Outbox. El worker publica al log y marca `PublishedAt`. Entrega **al menos una vez**; la generación única del aviso es una garantía distinta (dominio + índice).

## Tiempo

`IClock` con `Today` en zona Bogotá (`America/Bogota` / `SA Pacific Standard Time` en Windows). Intervalos de tiempo en **días** (6 meses ≈ 180).

## Frontend

Organizado por features (`fleet`, `orders`, `events`), modelos y servicio HTTP separados, formularios reactivos, locale `es-CO`, conservación de clave de idempotencia ante resultado incierto.

## Versiones

| Componente | Versión |
| --- | --- |
| .NET / ASP.NET Core | 10 |
| Dapper | 2.1.66 |
| Microsoft.Data.SqlClient | 6.1.3 |
| Angular | 20.3.x |
| Node (Docker build) | 24 |
| SQL Server | 2022 (imagen oficial) |
| nginx | 1.28-alpine |

## Fuentes

- https://github.com/DapperLib/Dapper
- https://learn.microsoft.com/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql
- https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker
- https://angular.dev/reference/versions
