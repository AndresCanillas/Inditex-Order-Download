# Alcance del proyecto (iterativo)

## Iteración 1
- Objetivo: Completar la localización al español de los textos del tracker de proceso de obtención de pedidos y del mensaje dinámico de resultado.
- Incluye:
  - Traducciones faltantes en `Resources.es-ES.json` para títulos, estados y detalles del tracker.
  - Localización de mensajes dinámicos generados por backend (patrones con número de pedido y cola).
  - Pruebas unitarias para validar la localización dinámica en el controlador.
- No incluye:
  - Refactor mayor del servicio `OrderServices`.
  - Cambios en otros idiomas distintos de español.
