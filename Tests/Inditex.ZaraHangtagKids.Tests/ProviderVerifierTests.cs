using JsonColor;
using Moq;
using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;
using Xunit;

namespace Inditex.ZaraHangtagKids.Tests
{
    public class ProviderVerifierTests
    {
        [Fact]
        public void ValidateProviderData_WhenProviderMissing_CreatesNotification()
        {
            var repository = new Mock<IProviderRepository>();
            var notifier = new Mock<INotificationWriter>();
            var log = new Mock<ILogService>();
            var db = new Mock<IDBX>();
            var companyInfo = new CompanyInfo { CompanyCode = "ACME" };

            repository
                .Setup(repo => repo.ProviderExists(db.Object, "123", 5))
                .Returns(false);
            repository
                .Setup(repo => repo.GetCompanyInfo(db.Object, 5))
                .Returns(companyInfo);

            var verifier = new ProviderVerifier(repository.Object, notifier.Object);

            verifier.ValidateProviderData(
                5,
                new Supplier { supplierCode = "123" },
                "ORD-99",
                "10",
                db.Object,
                log.Object,
                "order.json");

            notifier.Verify(
                writer => writer.CreateNotification(
                    db.Object,
                    5,
                    It.Is<string>(title => title.Contains("ORD-99") && title.Contains("5")),
                    It.Is<string>(message => message.Contains("ACME")),
                    1,
                    0,
                    It.IsAny<string>(),
                    It.Is<string>(json => json.Contains("supplierCode"))),
                Times.Once);
        }

        [Fact]
        public void ValidateProviderData_WhenSupplierCodeIsNumeric_NormalizesReference()
        {
            var repository = new Mock<IProviderRepository>();
            var notifier = new Mock<INotificationWriter>();
            var log = new Mock<ILogService>();
            var db = new Mock<IDBX>();

            repository
                .Setup(repo => repo.ProviderExists(db.Object, "123", 7))
                .Returns(true);

            var verifier = new ProviderVerifier(repository.Object, notifier.Object);

            verifier.ValidateProviderData(
                7,
                new Supplier { supplierCode = "00123" },
                "ORD-1",
                "10",
                db.Object,
                log.Object,
                "order.json");

            repository.Verify(repo => repo.ProviderExists(db.Object, "123", 7), Times.Once);
            notifier.Verify(writer => writer.CreateNotification(
                It.IsAny<IDBX>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }
    }
}
