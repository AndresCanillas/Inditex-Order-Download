const fs = require("fs");
const path = require("path");

describe("GetOrdersDialog layout", () => {
  const viewPath = path.join(__dirname, "../OrderDownloadWebApi/Pages/Orders/GetOrdersDialog.cshtml");
  const cssPath = path.join(__dirname, "../OrderDownloadWebApi/wwwroot/css/views.css");

  test("renders form and process tracker in side-by-side layout containers", () => {
    const view = fs.readFileSync(viewPath, "utf8");

    expect(view).toContain('class="get-orders-layout"');
    expect(view).toContain('class="get-orders-layout__form-column"');
    expect(view).toContain('class="get-orders-layout__tracker-column"');
    expect(view).toContain('class="order-process-tracker mt-3 mt-lg-0 d-none" name="processTrackerContainer"');
  });

  test("defines responsive styles for two-column Get Orders layout", () => {
    const css = fs.readFileSync(cssPath, "utf8");

    expect(css).toContain('.get-orders-layout {');
    expect(css).toContain('display: flex;');
    expect(css).toContain('.get-orders-layout__form-column {');
    expect(css).toContain('.get-orders-layout__tracker-column {');
    expect(css).toContain('@media (max-width: 991.98px)');
  });
});
