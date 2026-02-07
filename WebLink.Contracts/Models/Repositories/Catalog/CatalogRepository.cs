using Microsoft.AspNetCore.Builder;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace WebLink.Contracts.Models
{
    public class CatalogRepository : GenericRepository<ICatalog, Catalog>, ICatalogRepository
    {
        private IAppConfig config;
        private RequestLocalizationOptions langs;
        private string connstr;

        public CatalogRepository(
            IFactory factory,
            IAppConfig config,
            RequestLocalizationOptions langs
            )
            : base(factory, (ctx) => ctx.Catalogs)
        {
            this.config = config;
            this.langs = langs;
            connstr = config["Databases.CatalogDB.ConnStr"];
            var cfg = factory.GetInstance<IDBConfiguration>();
            cfg.ProviderName = CommonDataProviders.SqlServer;
            cfg.ConnectionString = connstr;
            cfg.EnsureCreated();
        }


        protected override string TableName { get => "Catalogs"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Catalog actual, ICatalog data)
        {
            #region detect catalog changes
            // notify about order affected
            if(IsDefinitionChanged(actual, (Catalog)data))
            {
                events.Send(new CatalogStructureUpdatedEvent()
                {
                    ProjectID = actual.ProjectID,
                    CatalogID = actual.ID, // print.catalogs.id
                    CatalogName = actual.Name
                });
            }

            if(actual.Name != data.Name)
            {
                events.Send(new CatalogNameUpdatedEvent()
                {
                    ProjectID = actual.ProjectID,
                    CatalogID = actual.ID, // print.catalogs.id
                    CatalogName = actual.Name,
                    NameBeforeUpdate = actual.Definition,
                    NameAfterUpdate = data.Definition
                });
            }

            #endregion Compare Catalog

            actual.Name = data.Name;
            actual.Captions = data.Captions;
            actual.Definition = data.Definition;
            actual.SortOrder = data.SortOrder;
            actual.IsHidden = data.IsHidden;
            actual.IsReadonly = data.IsReadonly;
            actual.CatalogType = data.CatalogType;
            actual.RequiredRoles = data.RequiredRoles;
        }


        protected override void BeforeInsert(PrintDB ctx, IUserData userData, Catalog actual, out bool cancelOperation)
        {
            cancelOperation = false;
            if(string.IsNullOrWhiteSpace(actual.Definition))
            {
                actual.Definition = JsonConvert.SerializeObject(typeof(DefaultCatalog).GetCatalogDefinition().Fields, Formatting.Indented);
            }

            using(var scope = new TransactionScope())
            {
                using(var db = factory.GetInstance<DynamicDB>())
                {
                    db.Open(connstr);
                    var cat = new CatalogDefinition();
                    cat.Name = actual.Name;
                    cat.Definition = actual.Definition;
                    cat.IsHidden = actual.IsHidden;
                    cat.IsReadonly = actual.IsReadonly;
                    cat.CatalogType = actual.CatalogType;
                    db.IsValidName(cat.Name);
                    db.CreateCatalog(cat);
                    actual.CatalogID = cat.ID;
                    scope.Complete();
                }
            }
        }


        protected override void BeforeUpdate(PrintDB ctx, IUserData userData, Catalog actual, out bool cancelOperation)
        {
            cancelOperation = false;
            using(var scope = new TransactionScope())
            {
                using(var db = factory.GetInstance<DynamicDB>())
                {
                    db.Open(connstr);
                    var catalogFieldList = actual.Fields.ToList();
                    var languages = langs.SupportedUICultures.Select(x => x.Name).ToList();
                    cancelOperation = db.ValidateFields(catalogFieldList, languages);
                    if(!cancelOperation)
                    {
                        var cat = db.GetCatalog(actual.CatalogID);
                        cat.Name = actual.Name;
                        cat.IsHidden = actual.IsHidden;
                        cat.IsReadonly = actual.IsReadonly;
                        cat.CatalogType = actual.CatalogType;
                        cat.Definition = actual.Definition;
                        db.IsValidName(cat.Name);
                        db.AlterCatalog(cat);
                    }
                    scope.Complete();
                }
            }
        }


        protected override void BeforeDelete(PrintDB ctx, IUserData userData, Catalog actual, out bool cancelOperation)
        {
            cancelOperation = true;
            var relatedTables = new Dictionary<string, string>();

            using(var scope = new TransactionScope())
            {
                using(var db = factory.GetInstance<DynamicDB>())
                {
                    db.Open(connstr);
                    relatedTables = db.DropCatalog(actual.CatalogID);
                    scope.Complete();
                }
            }

            //Remove fk from related catalogs definition
            foreach(var table in relatedTables)
            {
                var catalogId = table.Key.Split('_').Last();
                var childCatalog = GetByCatalogID(int.Parse(catalogId));
                var definition = childCatalog.Fields;
                var column = definition.FirstOrDefault(x => x.Name.Equals(table.Value));
                if(column != null)
                {
                    column.CatalogID = null;
                    column.Type = ColumnType.Int;
                    column.Length = 10;
                }

                childCatalog.Definition = JsonConvert.SerializeObject(definition);
                ctx.Catalogs.Update((Catalog)childCatalog);
                //ctx.Entry((Catalog)childCatalog).State = EntityState.Detached;
            }

            cancelOperation = false;
        }


        public ICatalog GetByCatalogID(int catalogid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCatalogID(ctx, catalogid);
            }
        }


        public ICatalog GetByCatalogID(PrintDB ctx, int catalogid)
        {
            return All(ctx).Where(p => p.CatalogID == catalogid)
                //.AsNoTracking()
                .FirstOrDefault();
        }


        public ICatalog GetByName(int projectid, string name)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByName(ctx, projectid, name);
            }
        }


        public ICatalog GetByName(PrintDB ctx, int projectid, string name)
        {
            return All(ctx)
                .Where(p => p.ProjectID == projectid && p.Name == name)
                .FirstOrDefault();
        }


        public List<ICatalog> GetByProjectID(int projectid, bool byPassChecks = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProjectID(ctx, projectid, byPassChecks);
            }
        }


        public List<ICatalog> GetByProjectID(PrintDB ctx, int projectid, bool byPassChecks = false)
        {
            var userData = factory.GetInstance<IUserData>();
            if(!userData.IsIDT && byPassChecks == false)
                throw new Exception("User does not have permission to execute this action.");
            return new List<ICatalog>(All(ctx).Where(p => Equals(p.ProjectID, projectid)).OrderBy(o => o.SortOrder));
        }


        public List<ICatalog> GetByProjectIDWithRoles(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProjectIDWithRoles(ctx, projectid);
            }
        }


        public List<ICatalog> GetByProjectIDWithRoles(PrintDB ctx, int projectid)
        {
            var userData = factory.GetInstance<IUserData>();
            var result = new List<ICatalog>(All(ctx).Where(p => Equals(p.ProjectID, projectid)).OrderBy(o => o.SortOrder));
            return result.Where(p => userData.Principal.ValidateRoles(p.RequiredRoles)).ToList();
        }


        public void PrepareDelete(int id)
        {
            var catalog = GetByID(id);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                dynamicDB.DropConstraints(catalog.CatalogID);
            }
        }


        public List<string> GetCatalogRoles(int catalogid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCatalogRoles(ctx, catalogid);
            }
        }


        public List<string> GetCatalogRoles(PrintDB ctx, int catalogid)
        {
            var actual = GetByCatalogID(ctx, catalogid);
            return actual.RequiredRoles?.Split(",").ToList();
        }


        public void AssignCatalogRoles(int catalogid, string roles)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AssignCatalogRoles(ctx, catalogid, roles);
            }
        }


        public void AssignCatalogRoles(PrintDB ctx, int catalogid, string roles)
        {
            var cat = ctx.Catalogs.Where(c => c.ID == catalogid).FirstOrDefault();
            if(cat == null)
                throw new Exception($"Catalog could not be found ({catalogid}).");
            cat.RequiredRoles = roles;
            ctx.SaveChanges();
        }


        public bool IsDefinitionChanged(Catalog actual, Catalog data)
        {
            var currentFields = actual.Fields.ToList();
            var newFields = data.Fields.ToList();

            if(currentFields.Count != newFields.Count)
            {
                if(currentFields.Count > newFields.Count)
                {
                    // fields was removed
                    return true;
                }
            }

            // same fields or new field was added


            for(int i = 0; i < currentFields.Count; i++)
            {
                var cf = currentFields[i];
                var nf = newFields.Find(f => f.Name.Equals(cf.Name));

                if(nf == null)
                {
                    return true; // name was changed
                }

                if(cf.Type != nf.Type)
                {
                    return true; // type was changed
                }
            }
            return false;
        }


        public void ImportCatalog(int id, Catalog catalog, string tableDefinition)
        {
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                var cat = dynamicDB.GetCatalog(id);
                cat.Definition = catalog.Definition;
                catalog.Definition = tableDefinition;
                dynamicDB.ImportCatalog(cat, tableDefinition);
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    ctx.Catalogs.Update(catalog);
                    ctx.SaveChanges();
                }
            }
        }

        public void AlterCatalog(Catalog catalog)
        {
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);

                var cat = dynamicDB.GetCatalog(catalog.CatalogID);

                cat.Definition = catalog.Definition;
                dynamicDB.IsValidName(cat.Name);
                dynamicDB.AlterCatalog(cat);
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    ctx.Catalogs.Update(catalog);
                    ctx.SaveChanges();
                }

            }
        }
    }


    class DefaultCatalog
    {
        [PK, Identity]
        public int ID { get; set; }
    }
}
