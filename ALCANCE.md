# Alcance del proyecto (Inditex Order Download)

## Iteración 3 (actual)
**Objetivo:** Corrección de code smells en el flujo de descarga de pedidos y en el controlador de órdenes, incluyendo pruebas unitarias TDD.

### En curso
- Evitar `NullReferenceException` al procesar pedidos no encontrados.
- Eliminar sync-over-async en el endpoint `/order/get`.
- Agregar pruebas unitarias para ambos cambios.

### Fuera de alcance (referencia)
- Feature ImageManagement (en planificación, pendiente de iniciar).

### Pendiente para próximas iteraciones
- Definir alcance técnico completo de ImageManagement (modelo, UI, notificaciones, reglas de retención).
