using Service.Contracts;

namespace WebLink.Contracts.Models
{
    public interface IRFIDConfigRepository : IGenericRepository<IRFIDConfig>
    {
        IRFIDConfig GetByCompanyID(int companyid);
        IRFIDConfig GetByCompanyID(PrintDB ctx, int companyid);

        IRFIDConfig GetByBrandID(int brandid);
        IRFIDConfig GetByBrandID(PrintDB ctx, int brandid);

        IRFIDConfig GetByProjectID(int projectid);
        IRFIDConfig GetByProjectID(PrintDB ctx, int projectid);

        IRFIDConfig SearchRFIDConfig(int projectid);
        IRFIDConfig SearchRFIDConfig(PrintDB ctx, int projectid);

        void UpdateSequence(int id, int serial);
        void UpdateSequence(PrintDB ctx, int id, int serial);

        ITagEncodingProcess GetEncodingProcess(int projectid);
        ITagEncodingProcess GetEncodingProcess(PrintDB ctx, int projectid);
    }
}
