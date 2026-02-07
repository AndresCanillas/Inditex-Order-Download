using System;

namespace WebLink.Contracts.Models.Repositories.Artifact.DTO
{
    public class ArticleArtifactDashboardDTO
    {
        public string ArticleCode { get; set; }
        public string ArticleDescription { get; set; }
        public string LabelName { get; set; }
        public string Comments { get; set; }
        public bool IsValid { get; set; }
        public int LabelID { get; set; }
        public DateTime MyProperty { get; set; }
        public DateTime UpdatedDateLabel { get; set; }
        public int IsMain { get; set; }
        public int ProjectID { get; set; }
        public string FileNameLabel { get; set; }
        

    }
}
