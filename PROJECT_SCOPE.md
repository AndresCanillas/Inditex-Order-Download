# Alcance del proyecto (iterativo)

## Iteración 1 - Seguimiento en tiempo real de GetOrdersDialog
- Revisar los JS asociados a `GetOrdersDialog` y al tracker de flujo.
- Validar el contexto de stream de eventos (`AppContext.AppEventListener`) para recibir mensajes backend->frontend.
- Cubrir con pruebas unitarias la extracción/filtrado de eventos en frontend.
- Implementar suscripción y sincronización del tracker con eventos en tiempo real filtrados por número de orden.
- Mantener compatibilidad con el flujo actual por respuesta HTTP final.

## Iteración 2 - Alineación con eventos tipados (`EventName`) estilo DashboardRefreshEvent
- Replicar el patrón de `VMenuBar`: manejar eventos explícitos por `EventName`.
- Definir un evento tipado backend `OrderGetProgressEvent` y permitir forwarding en `Startup`.
- Publicar transiciones de estado del flujo de descarga por cada fase del proceso.
- Actualizar `GetOrdersDialog` para reaccionar solo a `OrderGetProgressEvent` y aplicar cambios por `StepId/Status`.
- Ampliar pruebas unitarias del tracker para cubrir aplicación de eventos tipados.

## Iteración 3 - Emisión explícita de evento al descargar pedido desde backend
- Garantizar en `OrderServices.GetOrder` el `OrderGetProgressEvent` para el paso `download-order` inmediatamente después de `FetchOrderAsync` exitoso.
- Añadir prueba unitaria backend que verifique la publicación del evento de estado esperado.
- Mantener compatibilidad con el tracker frontend basado en eventos tipados (`EventName`).
