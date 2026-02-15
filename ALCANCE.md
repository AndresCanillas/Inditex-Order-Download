# Alcance del proyecto (Inditex Order Download)

## Iteración 14 (actual)
**Objetivo:** Sustituir literalmente los nombres antiguos por los nuevos (sin estrategia de alias) en plugin Zara y sincronización QR para alineación total con el contrato V26.

### Completado en esta iteración
- Eliminación de enfoque por alias:
  - Se removió la resolución por alias de componentes/assets en `JsonToTextConverter`.
  - Se volvió a resolución directa por nombre exacto.
- Sustitución literal de nombres en esquema externo:
  - `ExternalSchema` actualizado a nombres nuevos de componentes/asset (`PRODUCT_QR`, `PRODUCT_BARCODE`, `ICON_BUYER_GROUP`, `SIZE_GEOGRAPHIC_*`, `PRICE_*`, `PURCHASE_CENTER_ID`, `SIZE_ID`, `ICON_RFID`, etc.).
- Sustitución literal en sincronización de QR:
  - `QrProductSyncService` ahora procesa exclusivamente `PRODUCT_QR` (se removió compatibilidad dual con `QR_product`).
  - Actualización de mensajes de log asociados.
- Ajuste de pruebas unitarias (enfoque TDD de contrato nuevo):
  - Tests de `JsonToTextConverter` ajustados para validar nombres nuevos exactos.
  - Tests de `QrProductSyncService` actualizados para usar `PRODUCT_QR`.

### Pendiente para próximas iteraciones
- Ejecutar batería completa en entorno con .NET SDK para validar no-regresión integral.
- Definir estrategia de versionado de contrato para coexistencia controlada entre V25/V26 en caso de requerimiento de negocio.


## Iteración 15 (actual)
**Objetivo:** Ajustar la salida CSV para que el componente `PRODUCT_QR` use el valor de `PRODUCT_BARCODE` según el `valueMap` correspondiente cuando exista.

### Completado en esta iteración
- TDD en plugin Zara:
  - Se agregaron pruebas para validar que en CSV final `PRODUCT_QR` se completa con el mismo valor de `PRODUCT_BARCODE` por clave de `valueMap`.
  - Se actualizó el escenario V26 para reflejar el nuevo contrato esperado en `PRODUCT_QR`.
- Implementación:
  - `JsonToTextConverter` ahora resuelve `PRODUCT_QR` usando primero el `valueMap` de `PRODUCT_BARCODE` y, si no existe valor, mantiene el flujo original de `PRODUCT_QR`.

### Pendiente para próximas iteraciones
- Validar la batería de pruebas completa en entorno con .NET SDK instalado.
- Revisar con negocio si este comportamiento debe limitarse por versión de contrato (V26+) o por tipo de etiqueta.

## Iteración 16 (actual)
**Objetivo:** Asociar explícitamente el barcode de `PRODUCT_BARCODE` con cada asset de `PRODUCT_QR` durante la sincronización de imágenes a PrintCentral, evitando depender del nombre del archivo QR.

### Completado en esta iteración
- TDD en `QrProductSyncService`:
  - Se agregó prueba unitaria para el escenario donde la URL de `PRODUCT_QR` no contiene barcode y el barcode se toma desde `PRODUCT_BARCODE` por la misma clave/path de `valueMap`.
- Implementación en servicio de sincronización QR:
  - `ExtractQrProductAssets` ahora construye pares `QR URL + Barcode` correlacionando por ruta de `valueMap`.
  - Se prioriza el barcode proveniente de `PRODUCT_BARCODE`; si no existe, se mantiene fallback al parseo por nombre de archivo QR.
  - Se eliminó código no utilizado de configuración (`CompanyConfig`, `BrandConfig`) para reducir ruido.

### Pendiente para próximas iteraciones
- Validar compilación y pruebas en entorno con .NET SDK disponible.
- Confirmar con negocio si la correlación por path de `valueMap` requiere reglas adicionales para estructuras anidadas complejas.

## Iteración 17 (actual)
**Objetivo:** Refactorizar la UI de obtención de pedidos para visualizar el progreso por fases del flujo operativo (búsqueda, descarga, imágenes y envíos a Print Central) con detalle expandible.

### Completado en esta iteración
- TDD frontend:
  - Se agregaron pruebas unitarias para un nuevo módulo de tracking de fases (`GetOrderProcessTracker`) cubriendo estados `pending`, `in-progress`, `completed`, `failed` y `pending-validation`.
- Refactor de UI/UX en diálogo de pedidos:
  - Se incorporó un tracker desplegable con listado de fases y estado por fase.
  - Se integró renderizado dinámico de detalle por paso dentro del flujo de `GetOrder`.
- Implementación de lógica de proceso:
  - Nuevo módulo reusable `GetOrderProcessTracker.js` para gestionar transiciones de estado y sincronización de fases desde mensajes de backend.
  - Manejo explícito de escenario "pendiente por validación de imagen" para el envío de archivo a Print Central.
- Consistencia de validaciones:
  - Se alineó la validación de número de pedido con el criterio funcional: 10 dígitos y sin cero inicial.

### Pendiente para próximas iteraciones
- Evolucionar el backend para devolver eventos de fase estructurados (en lugar de inferir pasos por parsing de mensaje).
- Añadir pruebas de integración UI (Playwright) para verificar visualización del tracker en flujos exitosos/error.

## Iteración 18 (actual)
**Objetivo:** Corregir regresión en validación de número de pedido para admitir rango 5-10 dígitos (sin cero inicial), en coherencia con el requerimiento funcional.

### Completado en esta iteración
- TDD de regresión:
  - Se ajustó la prueba unitaria de `GetOrdersValidation` para validar explícitamente el rango 5-10 dígitos.
- Implementación:
  - Se actualizó `validateOrderNumber` para aceptar longitudes entre 5 y 10 dígitos y mantener la restricción de no iniciar en cero.
  - Se actualizó el mensaje de validación en la UI para reflejar correctamente la regla funcional.

### Pendiente para próximas iteraciones
- Evaluar centralización de mensajes/reglas de validación para evitar desalineaciones entre tests, lógica y copy UI.

## Iteración 19 (actual)
**Objetivo:** Reubicar visualmente el bloque de fases del proceso en la vista de GetPhoto/Get Orders para mostrarlo al lado de los campos de entrada y mejorar legibilidad.

### Completado en esta iteración
- TDD frontend de layout:
  - Se agregó prueba unitaria (`getOrdersLayout.test.js`) para verificar estructura de dos columnas en `GetOrdersDialog.cshtml` y reglas CSS responsivas asociadas en `views.css`.
- Implementación UI:
  - Se reorganizó el markup de `GetOrdersDialog` en un contenedor de layout con columna de formulario y columna de tracker.
  - Se movió el bloque `processTrackerContainer` a la columna lateral, manteniendo compatibilidad de selectores existentes (`name` attributes) para no romper la lógica JS.
- Estilos responsivos:
  - Se añadieron clases `get-orders-layout*` para disposición horizontal en escritorio y apilado en móvil.

### Pendiente para próximas iteraciones
- Validar visualmente en entorno ejecutable .NET con captura funcional de pantalla.
- Ajustar proporciones/espaciados finales con feedback UX del negocio (alineación exacta con mock).

## Iteración 20 (actual)
**Objetivo:** Mejorar la experiencia visual y el aprovechamiento del espacio en las vistas de `Get Orders` e `Image Management`, incorporando un layout de flujo paso a paso más claro, alegre y alineado con heurísticas de usabilidad de Jacob Nielsen.

### Completado en esta iteración
- Se consolidó un layout de flujo formal en `Get Orders` con:
  - cabecera contextual,
  - identidad visual con logo de Zara,
  - recordatorio explícito de “visibilidad del estado del sistema”,
  - panel lateral de fases reforzado para lectura secuencial.
- Se modernizó el estilo del tracker de proceso para mejorar escaneabilidad de estados (`in-progress`, `completed`, `failed`, etc.) con mayor contraste visual.
- Se rediseñó `Image Management` en un shell visual branded, manteniendo la funcionalidad actual (filtros, tabla, preview y acciones).
- Se aplicó enfoque TDD en frontend:
  - ampliación de pruebas de layout de `Get Orders`,
  - nuevas pruebas de layout/branding para `Image Management`.

### Pendiente para próximas iteraciones
- Ejecutar validación UX con usuarios de negocio y medir KPIs de uso (tiempo de tarea, satisfacción, reducción de errores).
- Revisar accesibilidad cromática completa (WCAG) de los nuevos estilos en distintos temas/pantallas.
