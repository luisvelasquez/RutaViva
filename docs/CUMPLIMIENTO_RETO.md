# Cumplimiento del reto RutaViva

Fuente de requisitos: PDF `DEV I_Reto.pdf` (3 páginas) + instrucciones técnicas de entrega del revisor.

Fecha de este informe: 10 de noviembre de 2026.

## Diferencias PDF vs instrucciones técnicas

| Tema | PDF | Instrucciones técnicas | Decisión |
| --- | --- | --- | --- |
| Pantallas | «Fuera de alcance: … pantallas» | UI Angular obligatoria; la sección de entrega del PDF también pide interfaz | Se implementa Angular |
| Persistencia | Permite memoria | SQL Server + Dapper + `.sql` | SQL Server |
| Backend | Cualquier componente | ASP.NET Core API REST | ASP.NET Core |
| Auth / pagos / agenda taller | Fuera de alcance | Fuera de alcance | No implementados |

## Catálogo de requisitos

### Funcionales (RF)

| ID | Requisito | Ref. PDF |
| --- | --- | --- |
| RF-01 | Registrar/consultar vehículos con taller asignado | p.1 RN1; entrega UI+API |
| RF-02 | Registrar/consultar rutinas por vehículo (km, tiempo o ambos) | p.1 RN1 |
| RF-03 | Recibir lecturas de odómetro | p.1 actores; p.2 RN6 |
| RF-04 | Evaluar vencimientos por km (lectura) y por tiempo (revisión diaria) | p.1 RN2 |
| RF-05 | Aviso «por vencer» una sola vez por ciclo al entrar en el último 10 % | p.1 RN2 |
| RF-06 | Crear orden al vencer, una abierta por vehículo+rutina | p.1–2 RN3 |
| RF-07 | Flujo orden: creada→cotizada→aprobada→en ejecución→completada | p.2 RN4 |
| RF-08 | Autoaprobación ≤ 1.500.000 COP; mayor requiere cliente | p.2 RN4 |
| RF-09 | Rechazo cancela; cliente cancela antes de ejecución | p.2 RN4 |
| RF-10 | Completar con km/fecha reales y reiniciar rutina | p.2 RN5 |
| RF-11 | Consultar eventos publicados | p.2 fuera de alcance avisos reales; basta evento |
| RF-12 | Interfaz de usuario + backend | p.3 alcance |

### Reglas de negocio (RN)

| ID | Regla | Ref. PDF |
| --- | --- | --- |
| RN-01 | Intervalos km/tiempo/ambos; vence el primero | p.1 |
| RN-02 | Estados al día / por vencer / vencida | p.1 |
| RN-03 | Umbral 10 % y aviso único por ciclo | p.1 |
| RN-04 | Una orden abierta ≠ completada/cancelada | p.2 |
| RN-05 | Techo autoaprobación 1.500.000 COP inclusive | p.2 |
| RN-06 | Odómetro nunca disminuye; ≤ se ignora sin error | p.2 |
| RN-07 | Tras cancelar no se modela recuperación del ciclo | p.2 fuera de alcance |

### Técnicos de las instrucciones (RT)

| ID | Requisito |
| --- | --- |
| RT-01 | Clean Architecture Domain/Application/Infrastructure/Api + tests |
| RT-02 | DDD con agregados Vehicle, MaintenanceRoutine, ServiceOrder + VOs |
| RT-03 | SQL en archivos `.sql`, Dapper parametrizado, sin EF |
| RT-04 | Repos por agregado + UoW/transacción compartida |
| RT-05 | Idempotencia Idempotency-Key + Outbox transaccional |
| RT-06 | Concurrencia con SQL (índices, versión, locks) |
| RT-07 | Docker Compose + Dockerfiles + `.env.example` |
| RT-08 | Pruebas dominio + integración SQL + flujo Angular |
| RT-09 | README + informe de cumplimiento |

## Matriz de cumplimiento

| ID | Requisito y referencia al PDF | Estado | Evidencia en código | Prueba y resultado | Pendiente o limitación |
| --- | --- | --- | --- | --- | --- |
| RF-01 | Vehículos con taller | Cumple | `Vehicle`, `CreateVehicleHandler`, `POST /api/vehicles`, fleet panel | Dominio OK (ejecutado). Integración SQL: implementada, **no ejecutada** en esta sesión | Docker Desktop apagado |
| RF-02 | Rutinas propias | Cumple | `MaintenanceRoutine`, `CreateMaintenanceRoutineHandler`, UI rutinas | Dominio umbrales OK | — |
| RF-03 | Lecturas | Cumple | `Vehicle.RegisterReading`, `RegisterOdometerReadingHandler` | Dominio lecturas OK | — |
| RF-04 | Evaluación km/tiempo | Cumple | `EvaluateDue`, `EvaluateDueMaintenanceHandler`, `MaintenanceWorker` | Dominio fronteras km/días OK | Revisión diaria real no ejercida en runtime |
| RF-05 | Aviso 10 % una vez | Cumple | `MaintenanceRoutine.EvaluateDue`, índice `UX_Outbox_NoticeCycle` | Dominio OK; SQL concurrente implementada **sin ejecutar** | — |
| RF-06 | Orden al vencer / unicidad | Cumple | `DueMaintenanceCoordinator`, `UX_Orders_OpenRoutine` | Prueba SQL de índice implementada **sin ejecutar** | — |
| RF-07 | Flujo de estados | Cumple | `ServiceOrder` métodos de transición | Dominio flujo OK | — |
| RF-08 | Autoaprobación | Cumple | `MoneyCop.AutoApprovalCeiling`, `ServiceOrder.Quote` | Dominio umbral OK | — |
| RF-09 | Rechazo/cancelación | Cumple | `RejectQuote`, `Cancel`, handlers | Dominio OK | — |
| RF-10 | Completar + reinicio | Cumple | `CompleteService`, `ResetCycle`, `CompleteServiceOrderHandler` | Dominio OK; SQL rollback/replay **sin ejecutar** | — |
| RF-11 | Eventos | Cumple | Outbox + worker log + events panel | Compilación OK | Publicación runtime no verificada aquí |
| RF-12 | UI + API | Cumple | `frontend/`, `RutaViva.Api` | Angular build OK (ejecutado) | UI smoke no ejecutado |
| RN-01 | Primer intervalo | Cumple | `MaintenanceInterval.IsOverdue` | Dominio OK | — |
| RN-02 | Tres estados | Cumple | `MaintenanceStatus` | Dominio OK | — |
| RN-03 | 10 % + único | Cumple | `IsDueSoon`, `NoticeRaised` | Dominio OK | — |
| RN-04 | Orden abierta | Cumple | `IsOpen`, índice filtrado | Implementado | SQL no ejecutada |
| RN-05 | 1.500.000 inclusive | Cumple | `QualifiesForAutomaticApproval` | Dominio `1500000 → Approved` | — |
| RN-06 | Odómetro monótono | Cumple | `RegisterReading` | Dominio OK | — |
| RN-07 | Post-cancelación | Cumple (alcance) | `SuppressCancelledCycle` | Dominio OK | Comportamiento posterior no modelado a propósito |
| RT-01 | Clean Architecture | Cumple | Proyectos y `Composition` | Build Release OK | — |
| RT-02 | DDD + VOs usados | Cumple | `Kilometrage`, `MoneyCop`, `MaintenanceInterval` en agregados | Dominio comparación por valor OK | — |
| RT-03 | SQL archivos + Dapper | Cumple | `Persistence/Queries/**/*.sql`, `SqlQueryCatalog` | `SqlQueryCatalogTests` 7 OK | — |
| RT-04 | Repos + UoW | Cumple | `SqlUnitOfWork`, repos por agregado | Build OK | Sin SqlSession/ORM |
| RT-05 | Idempotencia + Outbox | Cumple | `IdempotentExecutor`, `Receipts`, `Outbox` | Implementado; SQL concurrente **sin ejecutar** | — |
| RT-06 | Concurrencia SQL | Cumple parcialmente | Locks por `idem:`/`vehicle:`/`order:`, Version, índices | Código listo | **No verificado** contra SQL Server real en esta sesión |
| RT-07 | Docker | Cumple parcialmente | `compose.yaml`, Dockerfiles, healthchecks, `.env.example` | Archivos presentes | Docker Engine no conectó (`dockerDesktopLinuxEngine`) |
| RT-08 | Pruebas | Cumple parcialmente | Domain + SqlIntegration + ui_smoke/smoke scripts | Dominio 30 + catalog 6 = 36 OK | Integración SQL y UI smoke **sin ejecutar** |
| RT-09 | Docs | Cumple | README, ARCHITECTURE, API, VALIDATION, este archivo | Entregado | — |
| Auth/pagos/agenda | Fuera de alcance | Fuera de alcance | — | — | Conforme PDF |
| Avisos reales (email) | Fuera de alcance | Evento basta | Outbox→log | — | — |

## Supuestos documentados

1. **Meses calendario:** el ejemplo «6 meses» se modela como **180 días**. Aviso temporal al día 162. No se usan meses civiles.
2. **Salto a vencida:** no se emite aviso tardío de «por vencer».
3. **Tras cancelar/rechazar:** el ciclo queda `CycleCancelled` y no genera nuevas órdenes hasta un reinicio por servicio completado (que no aplica si la orden se canceló).
4. **Reporte tardío al completar:** se acepta km del servicio ≤ odómetro actual (telemetría posterior). Si el km del servicio es mayor, avanza el odómetro y reevalúa otras rutinas.
5. **Idioma del dominio:** nombres técnicos en inglés; UI en español. Mapeo RegistrarLectura→`RegisterReading`, EvaluarVencimiento→`EvaluateDue`, Cotizar→`Quote`, etc.

## Revisión técnica

### Arquitectura limpia y dependencias
Domain no referencia Dapper/SQL/HTTP. Application define contratos. Infrastructure implementa. Api compone DI y HTTP. Verificado por referencias de proyectos y build.

### DDD y agregados
Tres raíces: `Vehicle`, `MaintenanceRoutine`, `ServiceOrder`. Relacionados por Ids. Sin setters públicos de negocio; reconstitución vía `Reconstitute`.

### Objetos de valor
`Kilometrage`, `MoneyCop` y `MaintenanceInterval` viven en propiedades/comportamiento del modelo (`Odometer`, `QuotedAmount`, `Interval`), no solo en validación desechable.

### Separación DTO
HTTP/commands (`CreateVehicleCommand`, …), DTOs de lectura (`VehicleDto`, …), filas SQL (`VehicleRow`, …) y dominio están separados. La API no expone entidades.

### SOLID y SLAP
Casos de uso por funcionalidad (ya no un `MaintenanceService` monolítico). Handlers: cargar → comportar → persistir → outbox. SQL/parametría en repos. Idempotencia en `IdempotentExecutor`.

### SQL separado y parametrizado
Consultas en `Persistence/Queries/**/*.sql`, nombres cerrados en `SqlQueryCatalog`. Sin `SELECT *` en esas consultas. Parametrización Dapper.

### Idempotencia vs concurrencia
- **Idempotencia:** recibo `(Scope, Key)` + huella SHA-256 del body; misma clave/contenido reproduce; distinta huella → 409; recibo dentro de la transacción.
- **Concurrencia:** `sp_getapplock` por recurso (`idem:…`, `vehicle:…`, `order:…`) con orden estable; `Version` optimista; índices únicos filtrados. No depende de `SemaphoreSlim`.

### Transacciones y Outbox
`IdempotentExecutor` abre UoW, ejecuta caso de uso, inserta recibo y confirma. Eventos se escriben en Outbox en la misma transacción. Publicación local al log: **al menos una vez**.

### Frontend
Features `fleet` / `orders` / `events`, modelos y `ApiService` separados, formularios reactivos, estados de carga/error/vacío, locale `es-CO`, clave de idempotencia en `sessionStorage`, deshabilitación mientras `busy`.

### Docker y configuración
Compose con volumen, healthchecks y depends_on. Secretos por `.env` (ejemplo sin quemar en código). Angular nginx proxy a API.

## Verificación ejecutada vs no ejecutada

| Verificación | Estado en esta sesión |
| --- | --- |
| `dotnet build -c Release` | Ejecutada — OK |
| Pruebas dominio + catálogo SQL embebido | Ejecutada — 36 OK |
| Pruebas integración SQL Server | **No ejecutada** (sin `RUTAVIVA_TEST_SQL` / Docker Desktop no disponible) |
| `npm run build` Angular | Ejecutada — OK |
| `docker compose up` | **No ejecutada** |
| `scripts/smoke.py` / `ui_smoke.py` | **No ejecutada** |

## Cómo completar la verificación pendiente

```powershell
Copy-Item .env.example .env
# Iniciar Docker Desktop, luego:
docker compose up --build -d
$env:RUTAVIVA_TEST_SQL = 'Server=localhost;Database=master;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet test backend/RutaViva.sln -c Release
python scripts/smoke.py http://localhost:5000
```
