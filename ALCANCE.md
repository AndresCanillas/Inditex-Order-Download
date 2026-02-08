# Alcance del proyecto (Inditex Order Download)

## Iteración 4 (actual)
**Objetivo:** Implementar el backend de ImageManagement (persistencia, descarga, validación y bloqueo en APM) con pruebas unitarias TDD.

### En curso
- Persistencia de imágenes en LocalDB con hash SHA256 y estados (Nuevo/InFont/Rechazado/Obsoleto/Actualizado).
- Descarga de imágenes y notificación por correo al equipo de diseño.
- Bloqueo en APM (SendFileToPrintCentral) cuando hay imágenes pendientes.
- Endpoints base para consultar imágenes y actualizar estados.

### Fuera de alcance (referencia)
- Interfaz gráfica de ImageManagement (se abordará en próxima iteración).

### Pendiente para próximas iteraciones
- UI para equipo de diseño (listado, filtros, actualización de estado).
- Recordatorio diario y notificación a PrintCentral por pedidos retenidos.
