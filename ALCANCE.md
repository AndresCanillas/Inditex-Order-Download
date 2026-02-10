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
