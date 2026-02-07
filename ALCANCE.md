# Alcance del proyecto (Inditex Order Download)

## Iteración 4 (actual)
**Objetivo:** Reaplicar el escape RFC4180 en el CSV de Inditex (Zara External Labels) y reforzar pruebas con casos de comillas y delimitadores.

### En curso
- Usar `Rfc4180Writer.QuoteValue` con el delimitador `;` en el generador de CSV de Zara.
- Añadir pruebas unitarias que verifiquen el escape de valores con `;`.

### Fuera de alcance (referencia)
- Cambios en `OrderJsonColor` (Mango) fuera del alcance actual.
- Definición de contrato para plugins internos (WLZ/WPZ/PLZ/OTZ) pendiente de insumos.

### Pendiente para próximas iteraciones
- Definir contrato de columnas para plugins internos.
- Revisar naming/contrato de columnas (posible prefijo para diferenciar componentes vs assets) si se aprueba.

## Iteración 3 (completada)
**Objetivo:** Asegurar escape RFC4180 en el CSV de Inditex (Zara External Labels) cuando los valores contienen el delimitador `;`.

## Iteración 2 (completada)
**Objetivo:** Ajustar el conversor de Zara para piggybacks (BLUE/RED), normalización de URLs en componentes/assets, excepción de QR_product y eliminación de duplicados en header.
