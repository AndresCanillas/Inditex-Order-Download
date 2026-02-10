# Alcance del proyecto (Inditex Order Download)

## Iteración 10 (actual)
**Objetivo:** Sincronizar assets del componente `QR_product` con PrintCentral por temporada/proyecto para evitar cargas duplicadas y habilitar su uso en etiquetado.

### Completado en esta iteración
- `ImageManagementService` ahora incluye flujo de sincronización de `QR_product`:
  - Detecta URLs dentro de `ComponentValues` con `Name=QR_product`.
  - Resuelve el proyecto de PrintCentral a partir de campaña (o `DownloadServices.ImageManagement.QRProduct.ProjectID` como override).
  - Extrae el código de barras desde la URL del QR.
  - Consulta existencia en PrintCentral y sube sólo si no existe.
- Se mantuvo la validación existente de imágenes generales para aprobación en fuente sin mezclar QR con ese pipeline.
- Se extendió `IPrintCentralService` y `PrintCentralService` con operaciones para:
  - Verificar existencia de imagen de proyecto por código de barras.
  - Subir imagen de proyecto para el código de barras.
- Se añadieron pruebas unitarias para el nuevo comportamiento de `QR_product`:
  - Carga cuando no existe en PrintCentral.
  - No carga cuando ya existe.

### Pendiente para próximas iteraciones
- Confirmar y alinear endpoints definitivos del controlador de imágenes en PrintCentral (los actuales se dejaron con convención esperada).
- Añadir pruebas de integración de extremo a extremo contra entorno de PrintCentral (sandbox).
- Evaluar mover la resolución de proyecto (campaña -> projectId) a un servicio dedicado para reducir responsabilidad en `ImageManagementService`.
