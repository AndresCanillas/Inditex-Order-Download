using Service.Contracts;

namespace WebLink.Services.Automated
{
    public class FileOrdersManagerEvent : EQEventInfo
    {
        public int ProjectID { get; set; }
        public string FileName { get; set; }
        public FileOrdersManagerEvent(int projectID, string fileName)
        {
            ProjectID = projectID;
            FileName = fileName;
            //NOTE: Empty constructor is required by APM Workflow Constraints
        }

    }
}
