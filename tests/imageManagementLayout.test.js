const fs = require("fs");
const path = require("path");

describe("ImageManagement branded layout", () => {
  const viewPath = path.join(__dirname, "../OrderDownloadWebApi/Pages/ImageManagement/ImageManagementView.cshtml");
  const cssPath = path.join(__dirname, "../OrderDownloadWebApi/wwwroot/css/views.css");

  test("renders branded shell with Zara logo and structured sections", () => {
    const view = fs.readFileSync(viewPath, "utf8");

    expect(view).toContain('class="image-management-shell"');
    expect(view).toContain('class="image-management-shell__brand"');
    expect(view).toContain('class="image-management-shell__logo"');
    expect(view).toContain('src="~/images/zara-logo.jpg"');
    expect(view).toContain('class="card image-management-shell__filters mb-3"');
    expect(view).toContain('class="card image-management-shell__table"');
    expect(view).toContain('class="card image-management-shell__preview"');
  });

  test("defines visual style rules for branded image management shell", () => {
    const css = fs.readFileSync(cssPath, "utf8");

    expect(css).toContain('.image-management-shell {');
    expect(css).toContain('background: linear-gradient(135deg, #111e36 0%, #203b63 100%);');
    expect(css).toContain('.image-management-shell__header {');
    expect(css).toContain('.image-management-shell__brand {');
    expect(css).toContain('.image-management-shell__logo {');
  });
});
