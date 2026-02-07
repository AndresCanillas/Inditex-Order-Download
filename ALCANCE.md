# Alcance del proyecto (Inditex Order Download)

## Iteración 1 (actual)
**Objetivo:** Definir y aplicar el contrato fijo de columnas por tipo de plugin en el conversor de Zara, y asegurar que los `childrenLabels` se traten como etiquetas completas (incluyendo assets).

### En curso
- Contrato de columnas fijo por tipo de plugin (EXTERNAL) basado en estructura compartida, con base fields resueltos por reflexión.
- Soporte de `assets` en `childrenLabels` y pruebas asociadas.

### Fuera de alcance (referencia)
- Cambios en `OrderJsonColor` (solo contexto).
- Definición de contrato para plugins internos (WLZ/WPZ/PLZ/OTZ) pendiente de insumos.

### Pendiente para próximas iteraciones
- Definir contrato de columnas para plugins internos.
- Revisar naming/contrato de columnas (posible prefijo para diferenciar componentes vs assets) si se aprueba.
