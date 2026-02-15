const fs = require("fs");
const path = require("path");

describe("Index stylesheet references", () => {
  const indexPath = path.join(__dirname, "../OrderDownloadWebApi/Pages/Index.cshtml");

  test("loads views.css so page-level layouts are available at runtime", () => {
    const index = fs.readFileSync(indexPath, "utf8");

    expect(index).toContain('href="/css/views.css?@mark"');
  });
});
