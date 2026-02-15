# Alcance de iteraciones - Inditex-Order-Download

## Iteración 1 (actual)
### Objetivo
Ajustar el flujo de descarga para usar correctamente los endpoints separados de **token OAuth** y **pedido de etiquetas**, tomando configuración desde `appsettings` y credenciales desde `InditexCredentials.json`.

### Alcance incluido
- Token OAuth2 por `client_credentials` usando Basic Auth (`client_id:secret`) y `User-Agent` requerido.
- Uso de URL base + controlador de token desde configuración.
- Soporte de `scope` configurable desde `appsettings`.
- Fallback de `id_token` cuando `access_token` venga vacío.
- Construcción del endpoint de etiquetas desde `ControllerLabels` con `orderNumber` y `campaignCode`.
- Cobertura de pruebas unitarias para los escenarios críticos del token.

### Pendientes potenciales para siguiente iteración
- Pruebas integrales (con `HttpMessageHandler` fake) para validar headers HTTP reales (`Authorization`, `User-Agent`, `x-vendorid`).
- Validar con un entorno real de Inditex los parámetros exactos del endpoint de etiquetas (query/body) y ajustar contrato si fuese necesario.
- Revisar y corregir naming inconsistente (`Creadentials`, `Autentication`) para mejorar mantenibilidad.

## Iteración 2 (actual)
### Objetivo
Corregir la llamada de etiquetas para enviarla por **POST con body JSON**, sin query params, siguiendo el contrato final de Inditex.

### Alcance incluido
- La llamada de etiquetas usa el body:
  - `productionOrderNumber` (numérico)
  - `campaign`
  - `supplierCode`
- Se mantiene autenticación por `Authorization: Bearer` y header `x-vendorid`.
- Se agrega validación de `orderNumber` numérico antes de enviar la petición.
- Se añaden pruebas unitarias del flujo `FetchOrderAsync` validando que el body enviado al `IApiCallerService` cumple el contrato.

### Pendientes potenciales para siguiente iteración
- Prueba de integración HTTP real (handler fake) para validar método `POST`, content-type `application/json` y serialización exacta del payload.

## Iteración 3 (actual)
### Objetivo
Corregir code smells de mantenibilidad sin romper compatibilidad funcional.

### Alcance incluido
- Normalización de naming técnico: `AuthenticationResult`, `LoadInditexCredentials`, `CallGetOrderByNumber`.
- Compatibilidad retroactiva para nombres antiguos mediante wrappers marcados como obsoletos.
- Extracción de claves de configuración a constantes (`DownloadServiceConfig`) para eliminar strings mágicos repetidos en servicios principales.
- Ajustes menores de legibilidad (`historyDirectory`).
- Prueba unitaria adicional para validar compatibilidad del método legado.

### Pendientes potenciales para siguiente iteración
- Seguir normalizando typos históricos (`ClenerFiles`, `ProyectCode`) en una iteración controlada de refactor transversal.

## Iteración 4 (actual)
### Objetivo
Reutilizar `BaseServiceClient` de `Service.Contracts-NetCore21` en `ApiCallerService` para unificar la capa de cliente HTTP y sus utilidades comunes.

### Alcance incluido
- `ApiCallerService` ahora hereda de `BaseServiceClient`.
- `Start(string url)` delega la configuración de endpoint al `Url` base del `BaseServiceClient`.
- `GetLabelOrders` reutiliza `PostAsync<Input,Output>` del base para enviar requests y manejar serialización/respuesta.
- Se agregan pruebas unitarias enfocadas en:
  - normalización del `Url` al usar `Start`.
  - retorno `default` cuando el token está vacío.

### Pendientes potenciales para siguiente iteración
- Introducir pruebas de integración con `HttpMessageHandler` fake para validar headers efectivos (`Bearer`) en llamadas vía `BaseServiceClient`.
- Evaluar inyección de `HttpClient` en `ApiCallerService` para mejorar testabilidad del flujo de token OAuth sin dependencia de red.

## Iteración 5 (actual)
### Objetivo
Garantizar que la llamada de etiquetas cumpla el contrato HTTP requerido (`POST`, `User-Agent`, `Authorization Bearer`) y corregir code smells detectados en `ApiCallerService`.

### Alcance incluido
- `GetLabelOrders` envía header `User-Agent: BusinessPlatform/1.0` junto al `Authorization: Bearer`.
- Se valida entrada nula de `request` y se mantiene retorno temprano cuando no hay token.
- Refactor de `GetToken` para eliminar `async` innecesario (`Task.FromResult`) y disponer correctamente `HttpRequestMessage`/`HttpResponseMessage`.
- Inclusión de `scope` en el body de token cuando viene informado.
- Nueva prueba unitaria de contrato HTTP para `GetLabelOrders` validando método, URL final, headers y body JSON.

### Pendientes potenciales para siguiente iteración
- Extraer `tokenClient` detrás de una abstracción para evitar acoplamiento a infraestructura y habilitar pruebas unitarias puras del flujo OAuth.
- Evaluar migración de excepciones genéricas a excepciones de dominio para mejorar diagnóstico y resiliencia.

## Iteración 6 (actual)
### Objetivo
Alinear el payload del endpoint de búsqueda de pedidos para enviar `productionOrderNumber` como string, según el contrato de integración validado con `curl`.

### Alcance incluido
- `LabelOrderRequest.ProductionOrderNumber` cambia de `long` a `string` para preservar formato del payload esperado.
- `CreateLabelOrderRequest` mantiene validación numérica de `orderNumber` pero serializa el valor como string en el body JSON.
- Actualización de pruebas unitarias para verificar que `productionOrderNumber` se envía como string en el contrato HTTP y en el flujo de `OrderServices`.

### Pendientes potenciales para siguiente iteración
- Confirmar con entorno real de Inditex si `supplierCode` también requiere normalización/casteo específico (padding o formato fijo).
- Incorporar prueba de integración de extremo a extremo del POST con `HttpMessageHandler` fake y snapshot de payload.


## Iteración 7 (actual)
### Objetivo
Mitigar respuestas `403 Forbidden` asegurando que el token OAuth usado en la llamada de etiquetas sea el correcto para flujo `client_credentials`.

### Alcance incluido
- Se normaliza `AuthenticationResult` para usar `access_token` como fallback cuando `id_token` no viene informado.
- Se agrega validación defensiva cuando la respuesta de autenticación es nula.
- Se amplía cobertura unitaria de `OrderDownloadHelper` para validar el escenario OAuth real (`access_token` presente, `id_token` ausente).

### Pendientes potenciales para siguiente iteración
- Incorporar mapeo de errores HTTP por tipo (`401/403/5xx`) con logs enriquecidos para diagnóstico de permisos y scopes.
- Evaluar mover la normalización de token a un componente dedicado (SRP) para reducir responsabilidades en `OrderDownloadHelper`.


## Iteración 8 (actual)
### Objetivo
Corregir el contrato HTTP de búsqueda de etiquetas para incluir el header `x-vendorid`, requerido por el gateway de autorización y evitar `403 Forbidden`.

### Alcance incluido
- Se actualiza `IApiCallerService.GetLabelOrders` para recibir `vendorId` explícito.
- Se envía `x-vendorid` junto con `Authorization: Bearer` y `User-Agent: BusinessPlatform/1.0` en la llamada POST de etiquetas.
- `OrderServices` propaga `vendorId` al cliente HTTP en el flujo principal.
- Se extienden pruebas unitarias de `ApiCallerService` para validar presencia de `x-vendorid` y validar error cuando `vendorId` es vacío.

### Pendientes potenciales para siguiente iteración
- Agregar prueba de integración contra handler fake para validar comportamiento ante `403` y logging enriquecido de headers de contexto (sin exponer secretos).
- Confirmar con Inditex si el scope mínimo productivo debe incluir `market openid` además de `inditex` para el endpoint objetivo.


## Iteración 9 (actual)
### Objetivo
Alinear el cliente HTTP con el comportamiento observado en el API Portal: `x-vendorid` puede no venir en todas las ejecuciones y no debe bloquear la llamada.

### Alcance incluido
- `x-vendorid` pasa a ser header opcional en `ApiCallerService.GetLabelOrders`.
- Se elimina validación rígida que lanzaba excepción cuando `vendorId` era vacío.
- Se agrega prueba unitaria para validar que, sin `vendorId`, la request continúa y no envía el header.

### Pendientes potenciales para siguiente iteración
- Añadir estrategia de selección de scope por endpoint (`inditex` vs `market openid`) configurable por canal/cliente.
- Mapear respuesta `403` a error funcional con trazabilidad de request-id para soporte operativo.

## Iteración 10 (actual)
### Objetivo
Evitar envíos duplicados desde la UI de `Get Order` bloqueando el botón mientras la primera solicitud HTTP está en curso.

### Alcance incluido
- Se introduce `GetOrdersRequestState` para encapsular estado de request en progreso y la habilitación/deshabilitación del botón.
- `GetOrdersDialog` usa el estado para prevenir re-entradas del handler `GetOrder` antes de recibir respuesta.
- El botón `Get Order` se deshabilita al iniciar la petición y se habilita en `finally` tanto en éxito como en error.
- Se agregan pruebas unitarias de `GetOrdersRequestState` para validar bloqueo de reenvío y restauración del estado del botón.

### Pendientes potenciales para siguiente iteración
- Agregar prueba de integración UI (con DOM/JSDOM) para validar que múltiples clics rápidos sólo disparan una llamada a `AppContext.HttpPost`.
- Unificar reglas de validación de `orderNumber` entre mensajes de UI y `GetOrdersValidation` para eliminar inconsistencia funcional/documental.

## Iteración 11 (actual)
### Objetivo
Mejorar la trazabilidad visual del workflow de `Get Order` y eliminar inconsistencias entre mensajes de éxito/error mostrados en UI.

### Alcance incluido
- El tracker de fases ahora pinta cada fila por estado (verde completado, rojo error, azul en progreso, etc.) para identificar rápidamente por dónde pasó el flujo.
- `GetOrderProcessTracker` reconoce patrones de error y marca fallo en el paso activo o en el siguiente pendiente cuando llega un mensaje mixto (éxito parcial + error).
- `GetOrdersDialog` deja de asumir JSON válido en todas las respuestas, parsea de forma defensiva y evita mostrar éxito si el mensaje contiene errores funcionales.
- Se reemplaza el mensaje genérico hardcoded en inglés por texto localizado de error de comunicación en la vista.
- `OrdersController` detecta errores de forma case-insensitive para evitar falsos positivos de éxito por diferencias de mayúsculas/minúsculas.
- Se amplía cobertura unitaria del tracker para escenarios de mensaje mixto y detección de error.

### Pendientes potenciales para siguiente iteración
- Añadir pruebas unitarias/UI del archivo `GetOrdersDialog.js.cshtml` (JSDOM) para validar render final de alertas y clases CSS por paso.
- Normalizar el contrato backend para devolver un objeto estructurado por fases (en vez de texto libre) y reducir heurísticas de parsing.

## Iteración 12 (actual)
### Objetivo
Eliminar falso error de comunicación en `GetOrdersDialog` cuando la llamada `AppContext.HttpPost` retorna objeto JSON (no `fetch Response`) y el flujo ya completó el envío de archivo a Print Central.

### Alcance incluido
- Se agrega `GetOrdersApiResponse` para normalizar respuestas de API desde ambos contratos soportados (objeto JSON y `Response` con `text()`).
- `GetOrdersDialog` consume el normalizador y deja de llamar siempre `response.text()`/`response.ok`, evitando el `catch` falso que marcaba error final.
- Se registra el nuevo script en `GetOrdersDialog.cshtml` para disponibilidad en runtime.
- Se agregan pruebas unitarias de parser para validar escenarios: objeto JSON exitoso, `Response` fallido y body inválido.

### Pendientes potenciales para siguiente iteración
- Extraer lógica de render del resultado (`htmlResult`) a un helper testeable para reducir complejidad ciclomática del handler `GetOrder`.
- Incorporar prueba de integración UI (JSDOM) que simule click y valide que no aparece `Server communication error` cuando backend retorna `{ Success: true }`.

## Iteración 13 (actual)
### Objetivo
Asegurar que la vista de **Image Management** renderice todos los textos visibles mediante `ILocalizationService`, incluyendo los estados mostrados dinámicamente.

### Alcance incluido
- Se agrega mapeo de estados en `ImageManagementView.js.cshtml` para convertir valores internos (`New`, `Updated`, etc.) a textos localizados con `@g[...]`.
- `RenderStatusBadge` deja de pintar el valor crudo del estado y usa el texto localizado.
- Se agregan pruebas unitarias de regresión para validar uso de localización en la vista y en el script de Image Management.

### Pendientes potenciales para siguiente iteración
- Incorporar prueba de integración UI (JSDOM o Playwright) que valide en runtime el idioma renderizado según cultura activa.
- Revisar otras vistas legacy para detectar textos hardcodeados fuera de `@g[...]` y consolidar checklist de localización.

## Iteración 14 (actual)
### Objetivo
Reubicar visualmente el bloque de fases del proceso en `Get Orders` para mostrarlo en un panel dedicado a la derecha del formulario, mejorando legibilidad y aprovechamiento del espacio.

### Alcance incluido
- Se ajusta el markup de `GetOrdersDialog.cshtml` para envolver el tracker dentro de `get-orders-layout__tracker-panel` en la columna derecha.
- Se actualizan estilos en `views.css` para reservar un área visual fija (`min-height`) al tracker y mantener comportamiento responsive.
- Se amplían pruebas unitarias de layout para validar la nueva estructura del panel y la regla de altura mínima.

### Pendientes potenciales para siguiente iteración
- Evaluar mover el output de resultado (`information2`) al mismo panel derecho para consolidar feedback del flujo en una sola zona.
- Añadir prueba de integración visual (DOM) para verificar que el tracker no desplace el formulario cuando cambia entre `d-none` y visible.

## Iteración 15 (actual)
### Objetivo
Rediseñar visualmente las vistas `Get Orders` e `Image Management` para mejorar uso del espacio, reforzar visualización de flujo por etapas y aplicar principios de usabilidad de Nielsen con identidad visual de Zara.

### Alcance incluido
- TDD frontend:
  - Se amplían pruebas de `getOrdersLayout.test.js` para validar nuevo contenedor de flujo, bloque de recomendación de usabilidad y ajustes visuales del panel de etapas.
  - Se agrega `imageManagementLayout.test.js` para validar estructura de layout visual de Image Management y presencia de reglas CSS de branding.
- Implementación en `Get Orders`:
  - Se crea un contenedor formal `get-orders-flow` con cabecera de contexto, subtítulo de flujo, branding de Zara y recordatorio de heurística de Nielsen (`Visibility of system status`).
  - Se elimina estilo inline repetido en labels y se centraliza en clases CSS para mantener consistencia visual.
  - Se refuerza el panel derecho del tracker con altura útil mayor y paleta de estado más visible (progreso/completado/error/pendiente validación).
- Implementación en `Image Management`:
  - Se encapsula la vista en `image-management-shell` con tema visual y header de marca.
  - Se añade bloque de marca con logo de Zara y jerarquía visual más clara entre filtros, tabla y preview.
  - Se mantienen `name` y `action` usados por JS para no romper comportamiento existente.

### Pendientes potenciales para siguiente iteración
- Validar contraste AA/AAA de toda la paleta nueva en revisión UX formal.
- Incorporar métricas de usabilidad (tiempo para completar tarea, tasa de error) para confirmar mejora real post-rediseño.

## Iteración 16 (actual)
### Objetivo
Corregir regresiones de maquetado en `Get Orders` por herencia de estilos globales y habilitar traducciones reales en castellano para textos clave de `Get Orders` e `Image Management`.

### Alcance incluido
- TDD:
  - Se agrega `getOrdersSpanishLocalization.test.js` para validar traducciones `es-ES` en textos críticos del flujo y gestión de imágenes.
  - Se amplía `getOrdersLayout.test.js` para exigir selectores con mayor especificidad (`#GetOrdersDialog`) y overrides defensivos frente a CSS heredado.
- Implementación de CSS robusto:
  - `#GetOrdersDialog .get-orders-layout` ahora fuerza grid con `display: grid !important` para evitar colapso a una sola columna por estilos globales.
  - Se endurece estilo de inputs (`background`/`color`) para evitar herencia de temas oscuros que dejaban campos negros e ilegibles.
  - Se refuerzan estilos de lista/item del tracker con selectores scopiados para evitar viñetas/formato por defecto del navegador.
- Implementación de localización:
  - Se actualizan valores en `Resources.es-ES.json` para mostrar textos en castellano en `Get Orders` e `Image Management`, incluyendo los nuevos mensajes de flujo/Nielsen.

### Pendientes potenciales para siguiente iteración
- Replicar traducciones faltantes en otros idiomas (`ca-ES`, `fr-FR`, `tr-TR`) para evitar inconsistencia multi-idioma.
- Evaluar externalizar tokens de color y espaciado en variables de tema para reducir riesgo de sobreescritura futura.

## Iteración 17 (actual)
### Objetivo
Reemitir PR con el mismo alcance funcional ya implementado en UI/UX (`Get Orders` + `Image Management`) y localización (`es-ES`), debido a fallo en la generación previa del mensaje de PR.

### Alcance incluido
- No se introducen cambios funcionales adicionales en esta iteración.
- Se registra trazabilidad de reemisión para mantener histórico de alcance por iteración.

### Pendientes potenciales para siguiente iteración
- Completar validación E2E en entorno .NET desplegado con cambio de idioma en caliente.
