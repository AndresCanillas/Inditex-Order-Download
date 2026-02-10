# Alcance del proyecto (Inditex Order Download)

## Iteración 9 (actual)
**Objetivo:** Robustecer la extracción de URLs de imágenes en `ImageManagementService` para incluir componentes con `Type=string` y validar seguridad por tipo de recurso.

### Completado en esta iteración
- Extensión de `ExtractUrlAssets` para contemplar:
  - Assets de tipo `url` que realmente sean URLs de imagen.
  - `ComponentValues` cuyo `ValueMap` (plano o anidado) contenga URLs de imagen válidas.
- Validación de seguridad para aceptar únicamente URLs HTTP/HTTPS con extensión de imagen permitida.
- Deduplicación de URLs para evitar descargas repetidas.
- Nuevas pruebas unitarias para escenarios de componentes:
  - URL de imagen válida en componente.
  - URL no imagen ignorada.
  - URL de imagen en `ValueMap` anidado.

### Pendiente para próximas iteraciones
- Evaluar externalizar la política de “extensiones permitidas” a configuración.
- Validar contenido por MIME/sniffing durante descarga para una segunda capa de seguridad.
- Revisar convergencia de extracción de medios en una capa dedicada para reducir complejidad del servicio.
