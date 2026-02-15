using System;
using System.IO;
using Xunit;

namespace OrderDownloadWebApi.Tests
{
    public class GetOrderProcessTrackerLocalizationTests
    {
        private static string ReadRepoFile(string relativePath)
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            return File.ReadAllText(Path.Combine(repoRoot, relativePath));
        }

        [Fact]
        public void TrackerModule_ShouldSupportLocalizedLabels()
        {
            var script = ReadRepoFile("OrderDownloadWebApi/wwwroot/js/GetOrderProcessTracker.js");

            Assert.Contains("function getLocalizedText(localization, key, fallback)", script);
            Assert.Contains("buildDefaultSteps(localization)", script);
            Assert.Contains("\"stepTitle.\" + definition.id", script);
            Assert.Contains("syncStepsFromMessage(steps, message, localization)", script);
        }

        [Fact]
        public void GetOrdersDialog_ShouldPassLocalizationDictionaryToTracker()
        {
            var script = ReadRepoFile("OrderDownloadWebApi/Pages/Orders/GetOrdersDialog.js.cshtml");

            Assert.Contains("getTrackerLocalization: function ()", script);
            Assert.Contains("\"stepTitle.download-images\": \"@g[\"Image download (File Manager)\"]\"", script);
            Assert.Contains("GetOrderProcessTracker.buildDefaultSteps(this.getTrackerLocalization())", script);
            Assert.Contains("GetOrderProcessTracker.syncStepsFromMessage(this.processSteps, resultMessage, this.getTrackerLocalization())", script);
        }
    }
}
