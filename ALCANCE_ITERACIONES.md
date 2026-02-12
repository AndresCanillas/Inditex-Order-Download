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
