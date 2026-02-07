# Alcance del proyecto (Inditex Order Download)

## Iteración 2 (actual)
**Objetivo:** Ajustar el conversor de Zara para piggybacks (BLUE/RED), normalización de URLs en componentes/assets, excepción de QR_product y eliminación de duplicados en header.

### En curso
- Piggybacks: no crear filas propias, concatenar sufijo en referencia HPZ (1 o 2), y copiar componentes/assets a la línea base.
- Normalización de valores con URL a nombre de archivo (sin extensión), excepto QR_product (valor fijo "Por resolver").
- Eliminar duplicado de "Icono RFID" en el header (solo en assets).

### Fuera de alcance (referencia)
- Cambios en `OrderJsonColor` (solo contexto).
- Definición de contrato para plugins internos (WLZ/WPZ/PLZ/OTZ) pendiente de insumos.

### Pendiente para próximas iteraciones
- Definir contrato de columnas para plugins internos.
- Revisar naming/contrato de columnas (posible prefijo para diferenciar componentes vs assets) si se aprueba.
