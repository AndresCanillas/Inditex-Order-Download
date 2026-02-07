using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Sage
{
    public interface ISageSyncService
    {
		Task<string> ImportItemsAsync(IEnumerable<string> items, int projectID, string identifier, string family, string brand);
		Task<string> ImportItemsAsync(PrintDB ctx, IEnumerable<string> items, int projectID, string identifier, string family, string brand);
		Task<ICompany> SyncCompanyAsync(int companyID, string sageReference);
		Task<ICompany> SyncCompanyAsync(PrintDB ctx, int companyID, string sageReference);
		Task<IArticle> SyncItemAsync(int articleId, string sageReference);
		Task<IArticle> SyncItemAsync(PrintDB ctx, int articleId, string sageReference);
		Task<IArtifact> SyncArtifact(int id, string reference);
		Task<IArtifact> SyncArtifact(PrintDB ctx, int id, string reference);
	}
}
