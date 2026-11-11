# RutaViva

Sistema de mantenimiento preventivo de flotas. API REST ASP.NET Core (.NET 10), Angular 20 y SQL Server 2022, con aplicaciones separadas y arquitectura limpia.

## Ejecutar todo con Docker

Requiere Docker Engine / Docker Desktop con Compose v2, contenedores Linux, CPU x86-64 y al menos 4 GB asignados a Docker. SQL Server usa la imagen oficial Developer (solo desarrollo). En ARM se necesita un entorno x86-64 compatible; no se garantiza emulación.

1. Copia `.env.example` a `.env` y cambia `SQL_PASSWORD` por una contraseña fuerte (sin punto y coma, para este ejemplo de cadena de conexión).
2. Desde esta carpeta ejecuta:

```bash
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

PowerShell también admite estos comandos. Para copiar: `Copy-Item .env.example .env`; en bash: `cp .env.example .env`.

- Interfaz: http://localhost:8080
- API: http://localhost:5000/api/dashboard
- Salud: http://localhost:5000/health
- Contrato OpenAPI: http://localhost:5000/openapi/v1.json
- SQL Server: `localhost,1433`, usuario `sa`, contraseña de `.env`, base `RutaViva`. En SSMS habilita confiar en certificado para esta instalación local.

La API ejecuta el script inicial idempotente y carga datos de ejemplo una sola vez. No utiliza Entity Framework ni migraciones. El arranque espera a que SQL Server esté listo, y la web espera a la API. Los datos persisten en el volumen `sql-data`.

Para detener: `docker compose down`. Para **borrar definitivamente los datos de desarrollo** y empezar de cero: `docker compose down -v`.

## Qué puedes demostrar

La flota inicial tiene `ABC123`, con aceite por vencer, frenos vencidos y llantas al día. Ya existe una orden para frenos.

1. En Vehículos y rutinas, envía 26.000 km: aceite vence y se crea su orden.
2. Repite la lectura o envía una menor: se ignora sin error.
3. En Órdenes, cotiza frenos por 1.500.000 COP: aprobación automática.
4. Cotiza aceite por 1.500.001 COP: requiere aprobación o rechazo.
5. Aprueba, inicia y completa el servicio, indicando sus valores reales.
6. Consulta el nuevo ciclo en la rutina y los eventos publicados.
7. En una orden creada/cotizada/aprobada, prueba cancelar; en ejecución ya no se permite.
8. Ejecuta Evaluar mantenimiento para activar el mismo caso de uso de la revisión diaria.

El frontend muestra todas las acciones de cliente y taller para demostrar el reto. No hay autenticación, tal como está fuera de alcance; las reglas siempre se verifican en el backend.

## Ejecutar en desarrollo

Instala .NET SDK 10, Node 24 y Docker. Primero levanta solo SQL Server:

```bash
docker compose up -d sqlserver
```

Configura `ConnectionStrings__Database` en tu terminal. **No se incluye una contraseña real en appsettings.**

Bash:

```bash
export ConnectionStrings__Database='Server=localhost;Database=RutaViva;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet run --project backend/src/RutaViva.Api --urls http://localhost:5000
```

PowerShell:

```powershell
$env:ConnectionStrings__Database = 'Server=localhost;Database=RutaViva;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet run --project backend/src/RutaViva.Api --urls http://localhost:5000
```

En otra terminal:

```bash
cd frontend
npm ci
npm start
```

Abre http://localhost:4200. El proxy de desarrollo envía `/api` a la API local.

## Pruebas

```bash
dotnet test backend/RutaViva.sln -c Release
```

Las pruebas de dominio no necesitan infraestructura. Las de integración se omiten explícitamente si no está configurado `RUTAVIVA_TEST_SQL`.

Para probar SQL Server real, con el servicio SQL iniciado:

```bash
export RUTAVIVA_TEST_SQL='Server=localhost;Database=master;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True'
dotnet test backend/RutaViva.sln -c Release
```

En PowerShell usa `$env:RUTAVIVA_TEST_SQL = '...'`. Se crean bases `RutaVivaTest_<guid>` aisladas, que se eliminan al terminar; usa un servidor de desarrollo con permiso para crear/eliminar esas bases.

Prueba HTTP de extremo a extremo, con la API iniciada y Python 3 instalado:

```bash
python scripts/smoke.py http://localhost:5000
```

Este script agrega un vehículo de prueba y recorre idempotencia, lecturas concurrentes, aviso único, cotización, conflictos de versión, finalización y nuevo ciclo. Puedes repetirlo: cada ejecución usa una placa diferente.

Compilar frontend: `cd frontend && npm ci && npm run build`.

Prueba de navegador opcional, con toda la solución iniciada:

```bash
python -m pip install playwright
python -m playwright install chromium
python scripts/ui_smoke.py http://localhost:8080
```

Crea datos propios y verifica el flujo desde Angular, incluyendo la vista móvil. En `docs/VALIDATION.md` se detalla lo verificado en esta entrega.

## Estructura

| Ruta | Responsabilidad |
| --- | --- |
| `backend/src/RutaViva.Domain` | Agregados, objetos de valor, invariantes y eventos |
| `backend/src/RutaViva.Application` | Casos de uso por funcionalidad, contratos y DTO |
| `backend/src/RutaViva.Infrastructure` | Dapper, consultas `.sql`, repos por agregado, UoW, Outbox |
| `backend/src/RutaViva.Api` | HTTP, errores, composición y worker diario/outbox |
| `backend/tests/RutaViva.Tests` | Pruebas de dominio y de SQL Server real |
| `frontend` | Angular por features (`fleet`/`orders`/`events`) |
| `backend/database` | Script SQL inicial, sin migraciones |
| `docs/ARCHITECTURE.md` | Decisiones, concurrencia, DDD y límites |
| `docs/CUMPLIMIENTO_RETO.md` | Matriz de cumplimiento del reto |
| `docs/API.md` | Endpoints, payloads y respuestas |

## Alcance y decisiones

Se implementa interfaz porque la sección final de entrega la pide, aunque otra parte del enunciado excluye pantallas. No se implementan pagos, autenticación ni selección/agenda del taller. La orden usa el taller asignado al vehículo.

Los intervalos por tiempo se configuran en **días**: el ejemplo de seis meses se representa como 180 días; aviso en el día 162. Esta convención está explícita porque el enunciado no define meses calendario. Los intervalos de días no enteros no son aceptados.

Tras cancelar o rechazar, el ciclo queda suprimido y no vuelve a crear órdenes. Lo que sucede después de cancelar está fuera de alcance. Un salto de «al día» a «vencida» crea directamente la orden, sin enviar un aviso tardío de «por vencer».

El dominio usa nombres técnicos en inglés y la interfaz presenta los conceptos en español; el glosario está en la documentación de arquitectura. La matriz de cumplimiento está en `docs/CUMPLIMIENTO_RETO.md`.

## Repositorio

Incluye `.gitignore` y una configuración de CI. Crea tu repositorio público y sube esta carpeta, sin `.env`, `node_modules`, `bin` ni `obj`. Lee `docs/ARCHITECTURE.md` antes de presentarlo: explica los compromisos que debes poder defender.

## Cambios de estructura SQL

El script `backend/database/001_initial.sql` crea las tablas que falten. Se incorpora como recurso al compilar para que también funcione dentro de Docker. No modifica tablas existentes ni ejecuta migraciones.

Para un cambio posterior, crea un script SQL explícito, por ejemplo `002_agregar_columna.sql`, revísalo y ejecútalo en SSMS/Azure Data Studio/sqlcmd sobre la base correcta. Mantén los scripts en Git y actualiza las consultas Dapper cuando corresponda. Los scripts posteriores **no se aplican automáticamente**. En una instalación nueva, actualiza también el esquema inicial para reflejar la estructura completa.

No necesitas herramientas de EF, snapshots ni comandos `dotnet ef`.
