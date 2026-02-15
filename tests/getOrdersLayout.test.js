const fs = require("fs");
const path = require("path");

describe("GetOrdersDialog layout", () => {
  const viewPath = path.join(__dirname, "../OrderDownloadWebApi/Pages/Orders/GetOrdersDialog.cshtml");
  const cssPath = path.join(__dirname, "../OrderDownloadWebApi/wwwroot/css/views.css");

  test("renders form and process tracker in dedicated side-by-side panel containers", () => {
    const view = fs.readFileSync(viewPath, "utf8");

    expect(view).toContain('class="get-orders-flow"');
    expect(view).toContain('class="get-orders-flow__brand"');
    expect(view).toContain('class="get-orders-layout"');
    expect(view).toContain('class="get-orders-layout__form-column"');
    expect(view).toContain('class="get-orders-layout__tracker-column"');
    expect(view).toContain('class="get-orders-layout__tracker-panel"');
    expect(view).toContain('class="get-orders-flow__nielsen"');
    expect(view).toContain('class="order-process-tracker d-none" name="processTrackerContainer"');
  });

  test("defines responsive styles for two-column Get Orders layout with fixed tracker area", () => {
    const css = fs.readFileSync(cssPath, "utf8");

    expect(css).toContain('.get-orders-layout {');
    expect(css).toContain('#GetOrdersDialog .get-orders-layout {');
    expect(css).toContain('display: grid !important;');
    expect(css).toContain('#GetOrdersDialog .get-orders-flow__card .form-control {');
    expect(css).toContain('#GetOrdersDialog .order-process-tracker__list {');
    expect(css).toContain('background: #ffffff !important;');
    expect(css).toContain('.get-orders-flow {');
    expect(css).toContain('background: linear-gradient(135deg, rgba(17, 30, 54, 0.95), rgba(29, 50, 86, 0.9));');
    expect(css).toContain('display: flex;');
    expect(css).toContain('.get-orders-layout__form-column {');
    expect(css).toContain('.get-orders-layout__tracker-column {');
    expect(css).toContain('.get-orders-layout__tracker-panel {');
    expect(css).toContain('min-height: 460px;');
    expect(css).toContain('@media (max-width: 991.98px)');
  });
});
