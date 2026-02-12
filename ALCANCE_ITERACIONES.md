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
