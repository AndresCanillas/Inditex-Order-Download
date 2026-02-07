namespace WebLink.Contracts.Models
{
    public interface IFtpAccountRepository
    {
        FtpAccountInfo GetCompanyFtpAccount(int companyid, bool unprotect = false);
        FtpAccountInfo GetCompanyFtpAccount(PrintDB ctx, int companyid, bool unprotect = false);

        void SaveCompanyFtpAccount(FtpAccountInfo accInfo);
        void SaveCompanyFtpAccount(PrintDB ctx, FtpAccountInfo accInfo);

        void AddCompanyFtpDirectory(int companyid, string directoryName);
        void AddCompanyFtpDirectory(PrintDB ctx, int companyid, string directoryName);

        void DeleteCompanyFtpDirectory(int companyID, string fTPFolder);
        void DeleteCompanyFtpDirectory(PrintDB ctx, int companyID, string fTPFolder);

        void DeleteCompanyFtpAccount(string accountName);
        string GetCompanyHomeDirectory(int companyid);
        bool IsValidFtpPassword(string ftpPassword);
        bool IsValidFtpUser(string ftpUser);
        bool IsValidFtpDirectory(string directory);
    }
}
