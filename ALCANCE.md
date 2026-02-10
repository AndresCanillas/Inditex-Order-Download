# Alcance del proyecto (Inditex Order Download)

## Iteración 11 (actual)
**Objetivo:** Corregir code smells del flujo `QR_product` aplicando refactor SOLID sin alterar comportamiento funcional.

### Completado en esta iteración
- Refactor de responsabilidad en `ImageManagementService`:
  - Se extrajo la sincronización de `QR_product` a un servicio dedicado `IQrProductSyncService`/`QrProductSyncService`.
  - `ImageManagementService` ahora solo orquesta imágenes de aprobación en fuente + delega sincronización QR.
- Reducción de acoplamiento y complejidad:
  - Se removieron dependencias directas a `IPrintCentralService` y `IConnectionManager` desde `ImageManagementService`.
  - Se corrigió duplicación accidental en validación de `ExtractImageUrlsFromValueMap`.
- Mejora de mantenibilidad en `PrintCentralService`:
  - Se reemplazaron strings mágicos de endpoints de imágenes por constantes de plantilla.
- Pruebas unitarias (TDD de refactor):
  - Nuevos tests para `QrProductSyncService` (sube/no sube/no login con credenciales incompletas).
  - Nuevo test en `ImageManagementService` para validar delegación hacia `IQrProductSyncService`.

### Pendiente para próximas iteraciones
- Confirmar endpoint final del controlador de imágenes de Print para eliminar supuestos de convención.
- Extraer política común de detección de URL de imagen para evitar duplicación entre servicios.
- Incluir pruebas de integración contra entorno real/sandbox de PrintCentral.
