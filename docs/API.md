# Contrato HTTP

Todas las operaciones POST requieren `Content-Type: application/json` e `Idempotency-Key: <uuid>`. Todos los éxitos de comando devuelven 200 con JSON; los reintentos reproducen ese JSON. Campos camelCase, fechas ISO `yyyy-MM-dd`, importes en COP.

| Método y ruta | Uso |
| --- | --- |
| GET /health | Disponibilidad y conexión a SQL |
| GET /openapi/v1.json | Contrato OpenAPI generado |
| GET /api/dashboard | Vehículos, rutinas, órdenes y 100 eventos recientes |
| POST /api/vehicles | Registrar vehículo con taller |
| POST /api/vehicles/{id}/routines | Configurar rutina y evaluarla |
| POST /api/vehicles/{id}/readings | Procesar lectura y evaluar rutinas |
| POST /api/maintenance/evaluate | Revisión por tiempo manual; body `{}` |
| POST /api/orders/{id}/quote | Cotizar |
| POST /api/orders/{id}/approve | Aprobar cotización alta |
| POST /api/orders/{id}/reject | Rechazar y cancelar |
| POST /api/orders/{id}/cancel | Cancelar antes de ejecución |
| POST /api/orders/{id}/start | Iniciar orden aprobada |
| POST /api/orders/{id}/complete | Completar y reiniciar rutina |

## Payloads

Crear vehículo:

```json
{"plate":"XYZ987","workshop":"Taller Norte","odometer":20000}
```

Crear rutina:

```json
{"name":"Aceite","intervalKm":10000,"intervalDays":180,"lastServiceKm":20000,"lastServiceDate":"2026-11-10"}
```

Configura `intervalKm` o `intervalDays` como null si no aplica; no pueden ser ambos null. El último servicio no puede superar odómetro/fecha actual.

Lectura:

```json
{"kilometres":29000}
```

Respuesta incluye `accepted` y el vehículo. Menor o igual devuelve accepted=false, sin error.

Cotizar (usa la versión consultada):

```json
{"expectedVersion":1,"amount":1500000}
```

Aprobar/rechazar/cancelar/iniciar:

```json
{"expectedVersion":2}
```

Completar:

```json
{"expectedVersion":3,"kilometres":30000,"date":"2026-11-10"}
```

## Errores

Problem Details con `title`, `status` y `traceId`:

- 400: JSON/tipos inválidos.
- 404: agregado inexistente.
- 409: versión desactualizada, clave reutilizada con otros datos o conflicto de unicidad.
- 422: regla de negocio o clave ausente/inválida.
- 503: timeout/bloqueo transitorio; reintentar con la misma clave y contenido.
- 500: fallo no esperado; detalles solamente en log.

La versión devuelta es la confirmada por el comando. Una reproducción idempotente devuelve la respuesta histórica, aunque el recurso haya cambiado después. Para el estado actual consulta el dashboard.
