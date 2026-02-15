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

## Iteración 2
- Objetivo: Completar la localización del selector de idioma con todos los idiomas habilitados en la configuración del sistema.
- Incluye:
  - Cobertura de pruebas para validar claves de idioma faltantes (`Catalan` y `Turkish`) y literales de soporte en la barra de navegación.
  - Traducciones en español para los nombres de idiomas mostrados en el selector.
  - Declaraciones adicionales en `NavBar.cshtml` para que ResourceGen contemple todos los idiomas soportados.
- No incluye:
  - Refactor de arquitectura del sistema de localización.
  - Cambios funcionales en autenticación o en el flujo de pedidos.
