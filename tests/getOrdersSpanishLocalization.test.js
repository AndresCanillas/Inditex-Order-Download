const fs = require("fs");
const path = require("path");

describe("Spanish localization for Get Orders and Image Management", () => {
  const resourcesPath = path.join(__dirname, "../OrderDownloadWebApi/Resources.es-ES.json");

  function getMap() {
    const content = fs.readFileSync(resourcesPath, "utf8");
    const resources = JSON.parse(content).Resources || [];
    return new Map(resources.map((entry) => [entry.Key, entry.Value]));
  }

  test("contains translated labels for Get Orders flow", () => {
    const map = getMap();

    expect(map.get("Get Orders by Inditex API")).toBe("Obtener pedidos por API de Inditex");
    expect(map.get("Please introduce the order number")).toBe("Por favor, introduce el número de pedido");
    expect(map.get("Please introduce the campaign code")).toBe("Por favor, introduce el código de campaña");
    expect(map.get("Please introduce the vendor id")).toBe("Por favor, introduce el identificador del proveedor");
    expect(map.get("Follow the flow to capture and validate each stage of the process.")).toBe(
      "Sigue el flujo para capturar y validar cada etapa del proceso."
    );
    expect(map.get("Visibility of system status and clear feedback at every step.")).toBe(
      "Visibilidad del estado del sistema y retroalimentación clara en cada paso."
    );
  });

  test("contains translated labels for Image Management", () => {
    const map = getMap();

    expect(map.get("Image Management")).toBe("Gestión de imágenes");
    expect(map.get("Refresh")).toBe("Actualizar");
    expect(map.get("Status filter")).toBe("Filtro de estado");
    expect(map.get("Apply filters")).toBe("Aplicar filtros");
    expect(map.get("Search by name or URL")).toBe("Buscar por nombre o URL");
  });
});
