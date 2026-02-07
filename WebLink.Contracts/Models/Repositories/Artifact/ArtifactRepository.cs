using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models.Repositories.Artifact.DTO;

namespace WebLink.Contracts.Models
{
    public class ArtifactRepository : GenericRepository<IArtifact, Artifact>, IArtifactRepository
    {

        private string baseQuery = null;
        private IDBConnectionManager connManager;
        public ArtifactRepository(IFactory factory, IDBConnectionManager connManager)
            : base(factory, (ctx) => ctx.Artifacts)
        {
            this.connManager = connManager;
        }


        protected override string TableName => "Artifacts";


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Artifact actual, IArtifact data)
        {
            if(!userData.Admin_Artifact_CanEdit)
            {
                return;
            }

            actual.ArticleID = data.ArticleID;
            actual.LabelID = data.LabelID;
            actual.LayerLevel = data.LayerLevel;
            actual.Location = data.Location;
            actual.Name = data.Name;
            actual.Position = data.Position;
            actual.SageRef = data.SageRef;
            actual.SyncWithSage = data.SyncWithSage;
            actual.Description = data.Description;
            actual.IsTail = data.IsTail;
            actual.IsHead = data.IsHead;
            actual.EnablePreview = data.EnablePreview;
            actual.IsMain = data.IsMain;
        }


        public IEnumerable<IArtifact> GetByArticle(int articleID, bool loadLabels = false, bool loadArticles = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByArticle(ctx, articleID, loadLabels, loadArticles);
            }
        }


        public IEnumerable<ArticleArtifactDashboardDTO> GetDashboardData(int page, int pageSize, int projectid)
        {
            if(page < 1) page = 1;
            if(pageSize < 1) pageSize = 20;

            this.baseQuery = $@"
                   FROM(
                   SELECT
                       a.ArticleCode AS ArticleCode,
                       a.[Description] AS ArticleDescription,
                       l.[Name] AS LabelName,
                       l.[Comments] AS Comments,
                       l.IsValid AS IsValid,
                       l.ID AS LabelID,
                       CAST(l.UpdatedFileDate AS DATETIME2(0)) AS UpdatedDateLabel,
                        1 AS IsMain,
                        l.ProjectID AS ProjectID,
		                l.[FileName] AS FileNameLabel
                    FROM Articles a
                    LEFT JOIN Labels l ON l.ID = a.LabelID
                    UNION
                    SELECT
                        a.ArticleCode AS ArticleCode, 
		                a.[Description] AS ArticleDescription,
                        l.[Name] AS LabelName,
                        l.[Comments] AS Comments,
                        l.IsValid AS IsValid,
                        l.ID AS LabelID, 
                        CAST(l.UpdatedFileDate AS DATETIME2(0)) AS UpdatedDateLabel,
                        r1.IsMain AS IsMain,
                        l.ProjectID AS ProjectID,
		                l.FileName AS FileNameLabel
                    FROM Articles a
                    LEFT JOIN Artifacts r1 ON r1.ArticleID = a.ID
                    LEFT JOIN Labels l ON r1.LabelID = l.ID
                ) report
                WHERE report.labelID IS NOT NULL AND report.ProjectID = {projectid} ";

            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<ArticleArtifactDashboardDTO>($"SELECT * {this.baseQuery} ORDER BY report.UpdatedDateLabel DESC, ArticleCode OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY").ToList();
            }
        }

        public IEnumerable<ArticleArtifactDashboardDTO> GetSearchArticlesArtifactsData(int projectID, int page, int pageSize, string value = null)
        {
            if(page < 1) page = 1;
            if(pageSize < 1) pageSize = 20;

            var extraFilter = value != null ? $"{value}" : "";

            this.baseQuery = $@"
                   FROM(
                   SELECT
                       a.ArticleCode AS ArticleCode,
                       a.[Description] AS ArticleDescription,
                       l.[Name] AS LabelName,
                       l.[Comments] AS Comments,
                       l.IsValid AS IsValid,
                       l.ID AS LabelID,
                       CAST(l.UpdatedFileDate AS DATETIME2(0)) AS UpdatedDateLabel,
                        1 AS IsMain,
                        l.ProjectID AS ProjectID,
		                l.[FileName] AS FileNameLabel
                    FROM Articles a
                    LEFT JOIN Labels l ON l.ID = a.LabelID
                    UNION
                    SELECT
                        a.ArticleCode AS ArticleCode, 
		                a.[Description] AS ArticleDescription,
                        l.[Name] AS LabelName,
                        l.[Comments] AS Comments,
                        l.IsValid AS IsValid,
                        l.ID AS LabelID, 
                        CAST(l.UpdatedFileDate AS DATETIME2(0)) AS UpdatedDateLabel,
                        r1.IsMain AS IsMain,
                        l.ProjectID AS ProjectID,
		                l.FileName AS FileNameLabel
                    FROM Articles a
                    LEFT JOIN Artifacts r1 ON r1.ArticleID = a.ID
                    LEFT JOIN Labels l ON r1.LabelID = l.ID
                ) report
                WHERE report.labelID IS NOT NULL AND report.ProjectID = {projectID} {value} ";

            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<ArticleArtifactDashboardDTO>($@"SELECT * {this.baseQuery} ORDER BY report.UpdatedDateLabel DESC, ArticleCode OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY").ToList();
            }
        }

        public int GetSearchArticlesArtifactsCount(int projectID)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                return (int)conn.ExecuteScalar($@"
                       SELECT COUNT(*)
                       {this.baseQuery}");
            }

        }


        public int GetDashboardDataCount(int projectid)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                return (int)conn.ExecuteScalar($@"
                       SELECT COUNT(*)
                       {this.baseQuery}");
            }
        }

        public IEnumerable<IArtifact> GetByArticle(PrintDB ctx, int articleID, bool loadLabels = false, bool loadArticles = false)
        {
            var query = ctx.Artifacts
                .Where(w => w.ArticleID.Equals(articleID));

            if(loadLabels)
            {
                query = query.Include(a => a.Label);
            }

            if(loadArticles)
            {
                query = query.Include(a => a.Article);
            }

            return query.ToList();
        }


        public IArtifact AddArtifactToArticle(int articleid, int labelid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return AddArtifactToArticle(ctx, articleid, labelid);
            }
        }


        public IArtifact AddArtifactToArticle(PrintDB ctx, int articleid, int labelid)
        {
            var label = ctx.Labels.Single(l => l.ID == labelid);
            Artifact artifact = new Artifact()
            {
                ArticleID = articleid,
                LabelID = labelid,
                Name = label.Name
            };
            return Insert(ctx, artifact);
        }


        public List<ArtifactDTO> GetByArticles(List<int> articleIds, bool loadLabels = false, bool loadArticles = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByArticles(ctx, articleIds, loadLabels, loadArticles);
            }
        }

        public List<ArtifactDTO> GetByArticles(PrintDB ctx, List<int> articleIds, bool loadLabels = false, bool loadArticles = false)
        {
            List<ArtifactDTO> lst = new List<ArtifactDTO>();

            foreach(int articleid in articleIds)
            {
                var artifacts = ctx.Artifacts.Where(w => w.ArticleID.Equals(articleid));

                if(loadLabels)
                {
                    artifacts = artifacts.Include(a => a.Label);
                }

                if(loadArticles)
                {
                    artifacts = artifacts.Include(a => a.Article);
                }

                lst.Add(new ArtifactDTO { ArticleID = articleid, Artifacts = artifacts.ToList() });
            }
            return lst;
        }
    }

    public class ArtifactDTO
    {
        public int ArticleID { get; set; }
        public List<Artifact> Artifacts { get; set; }
    }
}
