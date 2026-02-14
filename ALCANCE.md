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
