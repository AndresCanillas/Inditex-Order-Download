namespace WebLink.Contracts.Models
{
    public interface IOrderWorkflowConfigRepository : IGenericRepository<IOrderWorkflowConfig>
    {
        IOrderWorkflowConfig GetByProjectID(int projectid);

        IOrderWorkflowConfig GetByProjectID(PrintDB ctx, int projectid);
    }
}
