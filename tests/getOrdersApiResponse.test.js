const responseParser = require("../OrderDownloadWebApi/wwwroot/js/GetOrdersApiResponse");

describe("GetOrdersApiResponse", () => {
  test("normaliza respuesta de AppContext.HttpPost (objeto JSON)", async () => {
    const parsed = await responseParser.parse({
      Success: true,
      Message: "File sent to Print Central for order 14313."
    });

    expect(parsed.ok).toBe(true);
    expect(parsed.result.Success).toBe(true);
    expect(parsed.message).toBe("File sent to Print Central for order 14313.");
  });

  test("normaliza respuesta tipo fetch Response", async () => {
    const parsed = await responseParser.parse({
      ok: false,
      text: async () => JSON.stringify({ Success: false, Message: "Operation could not be completed." })
    });

    expect(parsed.ok).toBe(false);
    expect(parsed.result.Success).toBe(false);
    expect(parsed.message).toBe("Operation could not be completed.");
  });

  test("tolera body inválido y conserva fallback de ok", async () => {
    const parsed = await responseParser.parse({
      ok: true,
      text: async () => "not-json"
    });

    expect(parsed.ok).toBe(true);
    expect(parsed.result).toBeNull();
    expect(parsed.message).toBe("");
  });
});
