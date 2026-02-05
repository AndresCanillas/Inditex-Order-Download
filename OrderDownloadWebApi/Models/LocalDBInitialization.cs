using Microsoft.EntityFrameworkCore;
using Service.Contracts;

namespace OrderDownloadWebApi.Models
{
    public class LocalDBInitialization
    {
        private IFactory factory;

        public LocalDBInitialization(IFactory factory)
        {
            this.factory = factory;
        }

        public bool EnsureDBInitialized(LocalDB ctx)
        {
            ctx.Database.Migrate();
            return true;
        }
    }
}
