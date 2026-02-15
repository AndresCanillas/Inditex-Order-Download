using System;
using System.IO;
using Xunit;

namespace OrderDownloadWebApi.Tests
{
    public class ImageManagementLocalizationTests
    {
        private static string ReadRepoFile(string relativePath)
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            return File.ReadAllText(Path.Combine(repoRoot, relativePath));
        }

        [Fact]
        public void ImageManagementView_ShouldUseLocalizationServiceForMainLabels()
        {
            var view = ReadRepoFile("OrderDownloadWebApi/Pages/ImageManagement/ImageManagementView.cshtml");

            Assert.Contains("@g[\"Image Management\"]", view);
            Assert.Contains("@g[\"Status filter\"]", view);
            Assert.Contains("@g[\"Apply filters\"]", view);
            Assert.Contains("@g[\"No images match the selected filters.\"]", view);
            Assert.Contains("@g[\"Save status\"]", view);
        }

        [Fact]
        public void ImageManagementScript_ShouldLocalizeStatusNames()
        {
            var script = ReadRepoFile("OrderDownloadWebApi/Pages/ImageManagement/ImageManagementView.js.cshtml");

            Assert.Contains("GetLocalizedStatus", script);
            Assert.Contains("@g[\"New\"]", script);
            Assert.Contains("@g[\"Updated\"]", script);
            Assert.Contains("@g[\"Rejected\"]", script);
            Assert.Contains("@g[\"Obsolete\"]", script);
            Assert.Contains("@g[\"In Font\"]", script);
            Assert.Contains("this.GetLocalizedStatus(status)", script);
        }
    }
}
