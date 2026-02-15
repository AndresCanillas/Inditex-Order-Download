const fs = require("fs");
const path = require("path");

describe("Language selector localization coverage", () => {
  const resourcesDir = path.join(__dirname, "../OrderDownloadWebApi");
  const navbarPath = path.join(resourcesDir, "Pages/Components/NavBar.cshtml");

  function getMap(fileName) {
    const content = fs.readFileSync(path.join(resourcesDir, fileName), "utf8");
    const resources = JSON.parse(content).Resources || [];
    return new Map(resources.map((entry) => [entry.Key, entry.Value]));
  }

  test("includes all supported culture display names in NavBar resource declarations", () => {
    const navbar = fs.readFileSync(navbarPath, "utf8");

    expect(navbar).toContain('g["Catalan (Catalan)"]');
    expect(navbar).toContain('g["Turkish (Turkey)"]');
  });

  test("adds missing language keys for Catalan and Turkish in all resource files", () => {
    const files = [
      "Resources.es-ES.json",
      "Resources.ca-ES.json",
      "Resources.fr-FR.json",
      "Resources.tr-TR.json"
    ];

    for (const file of files) {
      const map = getMap(file);
      expect(map.has("Catalan (Catalan)")).toBe(true);
      expect(map.has("Turkish (Turkey)")).toBe(true);
    }
  });

  test("uses Spanish labels for all language options in Spanish resources", () => {
    const map = getMap("Resources.es-ES.json");

    expect(map.get("English (United States)")).toBe("Inglés (Estados Unidos)");
    expect(map.get("Spanish (Spain, International Sort)")).toBe("Español (España, internacional)");
    expect(map.get("Catalan (Catalan)")).toBe("Catalán (Catalán)");
    expect(map.get("French (France)")).toBe("Francés (Francia)");
    expect(map.get("Turkish (Turkey)")).toBe("Turco (Turquía)");
  });
});
