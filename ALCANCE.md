# Alcance del proyecto (Inditex Order Download)

## Iteración 7 (actual)
**Objetivo:** Centralizar los BaseFields del esquema de Inditex para evitar duplicidad y cambios en múltiples archivos.

### En curso
- Definir BaseFields en un único lugar usando `nameof` y reutilizarlos en `LabelSchemaRegistry`.
- Agregar pruebas unitarias que validen que el esquema usa rutas derivadas de las propiedades reales.

### Pendiente para próximas iteraciones
- Retomar corrección del error de navegación en el menú lateral (VMenuBar) que provoca `Invalid viewContainer`.
- Retomar la UI de ImageManagement con enfoque de usabilidad y mejoras pendientes.
- Recordatorio diario y notificación a PrintCentral por pedidos retenidos.
