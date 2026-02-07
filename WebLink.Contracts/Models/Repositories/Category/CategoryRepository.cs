using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class CategoryRepository : GenericRepository<ICategory, Category>, ICategoryRepository
    {
        private IDBConnectionManager connManager;

        public CategoryRepository(
            IFactory factory,
            IDBConnectionManager connManager
        ) : base(factory, (ctx) => ctx.Categories)
        {
            this.connManager = connManager;
        }


        protected override string TableName { get => "Categories"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Category entity, ICategory data)
        {
            entity.Name = data.Name;
            entity.ProjectID = data.ProjectID;
        }


        protected override void BeforeDelete(PrintDB ctx, IUserData userData, Category actual, out bool cancelOperation)
        {
            cancelOperation = false;
            ctx.Database.ExecuteSqlCommand("update Articles set CategoryID = null where ProjectID = @projectid and CategoryID = @categoryid", new SqlParameter("@projectID", actual.ProjectID), new SqlParameter("@categoryid", actual.ID));
            //using (IDBX conn = connManager.OpenWebLinkDB())
            //{
            //	conn.ExecuteNonQuery("update Articles set CategoryID = null where ProjectID = @projectid and CategoryID = @categoryid", actual.ProjectID, actual.ID);
            //}
        }


        public List<ICategory> GetByProject(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProject(ctx, id);
            }
        }


        public List<ICategory> GetByProject(PrintDB ctx, int id)
        {
            return new List<ICategory>(
                from a in ctx.Categories
                where a.ProjectID == id
                select a)
            .OrderBy(x => x.Name)
            .ToList();
        }
    }
}
