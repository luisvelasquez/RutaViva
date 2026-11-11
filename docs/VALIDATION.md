# Verificación de la entrega

Fecha: 10 de noviembre de 2026 (sesión de refactor).

| Comprobación | Resultado |
| --- | --- |
| Compilación .NET Release | Correcta, sin errores ni advertencias |
| Pruebas de dominio + catálogo SQL embebido | 36 aprobadas |
| Integración con SQL Server real | Implementada (`SqlIntegrationTests`); **omitida** en esta sesión: no había `RUTAVIVA_TEST_SQL` y Docker Desktop no estaba disponible |
| Compilación Angular de producción | Correcta |
| `docker compose up` | **No ejecutado**: el motor Linux de Docker Desktop no respondió |
| `scripts/smoke.py` / `ui_smoke.py` | **No ejecutados** en esta sesión |

## Qué cubren las pruebas SQL (cuando se ejecuten)

- Doce solicitudes simultáneas con la misma clave → una sola ejecución y misma respuesta.
- Lecturas concurrentes + evaluación diaria → máximo odómetro y una sola orden.
- Diez evaluaciones simultáneas → un aviso por ciclo.
- Misma clave con otro contenido → conflicto.
- Índice filtrado rechaza orden abierta duplicada insertada a mano.
- Finalización inválida revierte orden, rutina, eventos y recibo.
- Iniciar y cancelar concurrentes → una sola transición.
- Completar + replay idempotente → un solo reinicio.
- Consultas embebidas parametrizadas (también cubierto sin SQL por `SqlQueryCatalogTests`).

## Límites

No afirmar que la concurrencia SQL quedó «comprobada» hasta levantar SQL Server y exportar `RUTAVIVA_TEST_SQL`. El código y las pruebas están listos; la ejecución depende del entorno.
