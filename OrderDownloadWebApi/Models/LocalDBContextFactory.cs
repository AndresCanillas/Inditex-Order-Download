using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Service.Contracts;
using System.Reflection;


namespace OrderDownloadWebApi.Models
{
    public interface ILocalDBContextFactory
    {
        LocalDB CreateContext(string connStr);
    }

    public class LocalDBContextFactory : ILocalDBContextFactory, IDesignTimeDbContextFactory<LocalDB>
    {
        private IAppConfig configuration;

        public LocalDBContextFactory()
        {
            var factory = new ServiceFactory();
            configuration = factory.GetInstance<IAppConfig>();
        }

        public LocalDB CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<LocalDB>();
            var connStr = configuration.GetValue<string>("Databases.LocalDB.ConnStr");
            builder.UseSqlServer(connStr, optionsBuilder => optionsBuilder.MigrationsAssembly(typeof(LocalDB).GetTypeInfo().Assembly.GetName().Name));
            return new LocalDB(builder.Options);
        }

        public LocalDB CreateContext(string connStr)
        {
            var builder = new DbContextOptionsBuilder<LocalDB>();
            builder.UseSqlServer(connStr);
            var db = new LocalDB(builder.Options);
            return db;
        }
    }
}
