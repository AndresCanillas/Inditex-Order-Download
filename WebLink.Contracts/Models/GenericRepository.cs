using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Models
{
    public interface ICompanyFilter<T> where T : class
    {
        int GetCompanyID(PrintDB ctx);
        Task<int> GetCompanyIDAsync(PrintDB ctx);
        IQueryable<T> FilterByCompanyID(PrintDB ctx, int companyid);
    }

    public interface ISortableSet<T> where T : class
    {
        IQueryable<T> ApplySort(IQueryable<T> qry);
    }

    public enum DBOperation
    {
        Insert = 0,
        Update = 1,
        Delete = 2
    }

    public interface IGenericRepository<T>
        where T : IEntity
    {
        T Create();

        T Insert(T data);
        T Insert(PrintDB ctx, T data);

        Task<T> InsertAsync(T data);
        Task<T> InsertAsync(PrintDB ctx, T data);

        T Update(T data);
        T Update(PrintDB ctx, T data);

        Task<T> UpdateAsync(T data);
        Task<T> UpdateAsync(PrintDB ctx, T data);

        void Delete(int id);
        void Delete(PrintDB ctx, int id);

        Task DeleteAsync(int id);
        Task DeleteAsync(PrintDB ctx, int id);

        void Rename(int id, string name);
        void Rename(PrintDB ctx, int id, string name);

        Task RenameAsync(int id, string name);
        Task RenameAsync(PrintDB ctx, int id, string name);

        T GetByID(int id, bool byPassChecks = false);
        T GetByID(PrintDB ctx, int id, bool byPassChecks = false);

        Task<T> GetByIDAsync(int id, bool byPassChecks = false);
        Task<T> GetByIDAsync(PrintDB ctx, int id, bool byPassChecks = false);

        List<T> GetList();
        List<T> GetList(PrintDB ctx);

        Task<List<T>> GetListAsync();
        Task<List<T>> GetListAsync(PrintDB ctx);

        IEnumerable<T> All();
        IQueryable<T> All(PrintDB ctx);
    }


    public abstract class GenericRepository<TInterface, TImplementation> : IGenericRepository<TInterface>
        where TInterface : class, IEntity
        where TImplementation : class, TInterface, new()
    {
        protected IFactory factory;
        protected Func<PrintDB, DbSet<TImplementation>> getTargetDbSet;
        protected IEventQueue events;

        public bool TriggerEntityEvents { get; set; } = true;

        protected abstract string TableName { get; }
        protected abstract void UpdateEntity(PrintDB ctx, IUserData userData, TImplementation actual, TInterface data);
        protected virtual void OnCreate(TImplementation actual) { }
        protected virtual void BeforeInsert(PrintDB ctx, IUserData userData, TImplementation actual, out bool cancelOperation) { cancelOperation = false; }
        protected virtual void AfterInsert(PrintDB ctx, IUserData userData, TImplementation actual) { }
        protected virtual void BeforeUpdate(PrintDB ctx, IUserData userData, TImplementation actual, out bool cancelOperation) { cancelOperation = false; }
        protected virtual void AfterUpdate(PrintDB ctx, IUserData userData, TImplementation actual) { }
        protected virtual void BeforeDelete(PrintDB ctx, IUserData userData, TImplementation actual, out bool cancelOperation) { cancelOperation = false; }
        protected virtual void AfterDelete(PrintDB ctx, IUserData userData, TImplementation actual) { }
        protected virtual void BeforeRename(PrintDB ctx, IUserData userData, TImplementation actual, out bool cancelOperation) { cancelOperation = false; }
        protected virtual void AfterRename(PrintDB ctx, IUserData userData, TImplementation actual) { }

        public GenericRepository(IFactory factory, Func<PrintDB, DbSet<TImplementation>> getTargetDbSet)
        {
            this.factory = factory;
            this.getTargetDbSet = getTargetDbSet;
            this.events = factory.GetInstance<IEventQueue>();
        }


        public virtual TInterface Create()
        {
            var actual = new TImplementation();
            OnCreate(actual);
            return actual;
        }


        public virtual TInterface Insert(TInterface data)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return Insert(ctx, data);
            }
        }


        public virtual TInterface Insert(PrintDB ctx, TInterface data)
        {
            var actual = new TImplementation();
            Reflex.Copy(actual, data);

            var userData = factory.GetInstance<IUserData>();

            int companyid = 0;
            var set = getTargetDbSet(ctx);

            AuthorizeOperation(ctx, userData, actual);
            SetAutoFields(DBOperation.Insert, userData, actual);
            BeforeInsert(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Insert operation cancelled due to rule set.");

            set.Add(actual);
            ctx.SaveChanges();

            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Insert));
            AfterInsert(ctx, userData, actual);
            return actual;
        }


        public virtual async Task<TInterface> InsertAsync(TInterface data)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return await InsertAsync(ctx, data);
            }
        }


        public virtual async Task<TInterface> InsertAsync(PrintDB ctx, TInterface data)
        {
            var actual = new TImplementation();
            Reflex.Copy(actual, data);

            var userData = factory.GetInstance<IUserData>();

            int companyid = 0;
            var set = getTargetDbSet(ctx);

            await AuthorizeOperationAsync(ctx, userData, actual);
            SetAutoFields(DBOperation.Insert, userData, actual);
            BeforeInsert(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Insert operation cancelled due to rule set.");

            set.Add(actual);
            await ctx.SaveChangesAsync();

            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Insert));
            AfterInsert(ctx, userData, actual);
            return actual;
        }


        public virtual TInterface Update(TInterface data)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return Update(ctx, data);
            }
        }


        public virtual TInterface Update(PrintDB ctx, TInterface data)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = set.Find(data.ID);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {data.ID}.");

            AuthorizeOperation(ctx, userData, actual);
            UpdateEntity(ctx, userData, actual, data);
            SetAutoFields(DBOperation.Update, userData, actual);
            BeforeUpdate(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Update operation cancelled due to rule set.");

            ctx.SaveChanges();

            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Update));
            AfterUpdate(ctx, userData, actual);
            return actual;
        }


        public virtual async Task<TInterface> UpdateAsync(TInterface data)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return await UpdateAsync(ctx, data);
            }
        }


        public virtual async Task<TInterface> UpdateAsync(PrintDB ctx, TInterface data)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = await set.FindAsync(data.ID);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {data.ID}.");

            await AuthorizeOperationAsync(ctx, userData, actual);
            UpdateEntity(ctx, userData, actual, data);
            SetAutoFields(DBOperation.Update, userData, actual);
            BeforeUpdate(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Update operation cancelled due to rule set.");

            await ctx.SaveChangesAsync();
            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);


            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Update));
            AfterUpdate(ctx, userData, actual);
            return actual;
        }


        public virtual void Delete(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                Delete(ctx, id);
            }
        }


        public virtual void Delete(PrintDB ctx, int id)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = set.Find(id);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {actual.ID}.");

            AuthorizeOperation(ctx, userData, actual);
            BeforeDelete(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Delete operation cancelled due to rule set.");

            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

            set.Remove(actual);
            ctx.SaveChanges();

            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Delete));
            AfterDelete(ctx, userData, actual);
        }


        public virtual async Task DeleteAsync(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                await DeleteAsync(ctx, id);
            }
        }


        public virtual async Task DeleteAsync(PrintDB ctx, int id)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = await set.FindAsync(id);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {actual.ID}.");

            await AuthorizeOperationAsync(ctx, userData, actual);
            BeforeDelete(ctx, userData, actual, out var cancelOperation);

            if(cancelOperation)
                throw new Exception("Delete operation cancelled due to rule set.");

            if(actual is ICompanyFilter<TImplementation>)
                companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

            set.Remove(actual);
            await ctx.SaveChangesAsync();

            if(TriggerEntityEvents)
                events.Send(new EntityEvent(companyid, actual, DBOperation.Delete));
            AfterDelete(ctx, userData, actual);
        }


        public virtual void Rename(int id, string name)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                Rename(ctx, id, name);
            }
        }


        public virtual void Rename(PrintDB ctx, int id, string name)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = set.Find(id);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {actual.ID}.");

            var renameService = actual as ICanRename;
            if(renameService != null)
            {
                AuthorizeOperation(ctx, userData, actual);
                BeforeRename(ctx, userData, actual, out var cancelOperation);
                if(cancelOperation)
                    throw new Exception("Rename operation cancelled due to rule set.");

                renameService.Rename(name);
                ctx.SaveChanges();

                if(actual is ICompanyFilter<TImplementation>)
                    companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

                if(TriggerEntityEvents)
                    events.Send(new EntityEvent(companyid, actual, DBOperation.Update));
                AfterRename(ctx, userData, actual);
            }
            else throw new Exception($"Interface does not implement Rename functionality.");
        }


        public virtual async Task RenameAsync(int id, string name)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                await RenameAsync(ctx, id, name);
            }
        }


        public virtual async Task RenameAsync(PrintDB ctx, int id, string name)
        {
            TImplementation actual;
            int companyid = 0;

            var userData = factory.GetInstance<IUserData>();
            var set = getTargetDbSet(ctx);

            actual = await set.FindAsync(id);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {actual.ID}.");

            var renameService = actual as ICanRename;
            if(renameService != null)
            {
                await AuthorizeOperationAsync(ctx, userData, actual);
                BeforeRename(ctx, userData, actual, out var cancelOperation);

                if(cancelOperation)
                    throw new Exception("Rename operation cancelled due to rule set.");

                renameService.Rename(name);
                await ctx.SaveChangesAsync();

                if(actual is ICompanyFilter<TImplementation>)
                    companyid = (actual as ICompanyFilter<TImplementation>).GetCompanyID(ctx);

                if(TriggerEntityEvents)
                    events.Send(new EntityEvent(companyid, actual, DBOperation.Update));
                AfterRename(ctx, userData, actual);
            }
            else throw new Exception($"Interface does not implement Rename functionality.");
        }


        public virtual TInterface GetByID(int id, bool byPassChecks = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByID(ctx, id, byPassChecks);
            }
        }


        public virtual TInterface GetByID(PrintDB ctx, int id, bool byPassChecks = false)
        {
            var set = getTargetDbSet(ctx);

            var actual = set.Find(id);
            if(actual == null && !byPassChecks)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {id}.");

            if(!byPassChecks)
            {
                var userData = factory.GetInstance<IUserData>();
                AuthorizeOperation(ctx, userData, actual);
            }

            return actual;
        }


        public virtual async Task<TInterface> GetByIDAsync(int id, bool byPassChecks = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return await GetByIDAsync(ctx, id, byPassChecks);
            }
        }


        public virtual async Task<TInterface> GetByIDAsync(PrintDB ctx, int id, bool byPassChecks = false)
        {
            var set = getTargetDbSet(ctx);

            var actual = await set.FindAsync(id);
            if(actual == null)
                throw new Exception($"Record not found. {typeof(TImplementation).Name} {id}.");

            if(!byPassChecks)
            {
                var userData = factory.GetInstance<IUserData>();
                await AuthorizeOperationAsync(ctx, userData, actual);
            }

            return actual;
        }


        public virtual List<TInterface> GetList()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetList(ctx);
            }
        }


        public virtual List<TInterface> GetList(PrintDB ctx)
        {
            var userData = factory.GetInstance<IUserData>();

            var set = getTargetDbSet(ctx);
            IQueryable<TImplementation> qry;
            var actual = new TImplementation();
            if(userData.IsIDT)
            {
                qry = set.AsNoTracking();
            }
            else
            {
                var filter = actual as ICompanyFilter<TImplementation>;
                if(filter != null)
                    qry = filter.FilterByCompanyID(ctx, userData.SelectedCompanyID).AsNoTracking();
                else
                    qry = set.AsNoTracking();
            }
            var orderby = actual as ISortableSet<TImplementation>;
            if(orderby != null)
                qry = orderby.ApplySort(qry);

            return new List<TInterface>(qry);
        }


        public virtual async Task<List<TInterface>> GetListAsync()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return await GetListAsync(ctx);
            }
        }


        public virtual async Task<List<TInterface>> GetListAsync(PrintDB ctx)
        {
            var userData = factory.GetInstance<IUserData>();

            var set = getTargetDbSet(ctx);
            IQueryable<TImplementation> qry;
            var actual = new TImplementation();
            if(userData.IsIDT)
            {
                qry = set.AsNoTracking();
            }
            else
            {
                var filter = actual as ICompanyFilter<TImplementation>;
                if(filter != null)
                    qry = filter.FilterByCompanyID(ctx, userData.SelectedCompanyID).AsNoTracking();
                else
                    qry = set.AsNoTracking();
            }

            var orderby = actual as ISortableSet<TImplementation>;
            if(orderby != null)
                qry = orderby.ApplySort(qry);

            var result = await qry.ToListAsync();
            return new List<TInterface>(result);
        }

        public virtual IEnumerable<TInterface> All()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return All(ctx).ToList();
            }
        }

        public virtual IQueryable<TInterface> All(PrintDB ctx)
        {
            var userData = factory.GetInstance<IUserData>();

            var set = getTargetDbSet(ctx);
            IQueryable<TImplementation> qry;
            var actual = new TImplementation();
            if(userData.IsIDT)
            {
                qry = set.AsNoTracking();
            }
            else
            {
                var filter = actual as ICompanyFilter<TImplementation>;
                if(filter != null)
                    qry = filter.FilterByCompanyID(ctx, userData.SelectedCompanyID).AsNoTracking();
                else
                    qry = set.AsNoTracking();
            }
            var orderby = actual as ISortableSet<TImplementation>;
            if(orderby != null)
                qry = orderby.ApplySort(qry);

            return qry;
        }


        protected virtual void AuthorizeOperation(PrintDB ctx, IUserData userData, TImplementation data)
        {
            //if (userData.IsIDT || userData.UserName == "SYSTEM") return;  // Do not restrict access to IDT Users/SYSTEM
            //var filter = data as ICompanyFilter<TImplementation>;
            //if (filter != null)
            //{
            //             var companyID = filter.GetCompanyID(ctx);
            //             //var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyID && x.ProviderCompanyID == userData.SelectedCompanyID);
            //             var isProvider = IsProvider(ctx, companyID, userData.SelectedCompanyID);

            //             if (companyID != userData.SelectedCompanyID && !isProvider)
            //		throw new Exception($"Not authorized [{userData.Principal.Identity}]");
            //}

            AuthorizeOperationAsync(ctx, userData, data).GetAwaiter().GetResult();

        }


        protected virtual async Task AuthorizeOperationAsync(PrintDB ctx, IUserData userData, TImplementation data)
        {
            if(userData.IsIDT || userData.UserName == "SYSTEM") return;  // Do not restrict access to IDT Users/SYSTEM
            var filter = data as ICompanyFilter<TImplementation>;
            if(filter != null)
            {
                var companyID = await filter.GetCompanyIDAsync(ctx);
                //var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyID && x.ProviderCompanyID == userData.SelectedCompanyID);
                //var isProvider = IsProvider(ctx, companyID, userData.SelectedCompanyID);
                //var isBrand = IsProvider(ctx, userData.SelectedCompanyID, companyID);

                if(companyID != userData.SelectedCompanyID && !IsProvider(ctx, companyID, userData.SelectedCompanyID) && !IsProvider(ctx, userData.SelectedCompanyID, companyID))
                    throw new Exception($"Not authorized access for [{userData.Principal.Identity}] on the Entity {typeof(TImplementation).ToString()}");
            }
        }


        protected void SetAutoFields(DBOperation operation, IUserData userData, TImplementation data)
        {
            var basicTracking = data as IBasicTracing;
            if(basicTracking != null)
            {
                if(operation == DBOperation.Insert)
                {
                    basicTracking.CreatedBy = userData != null ? userData.Principal.Identity.Name : "SYSTEM";
                    basicTracking.CreatedDate = DateTime.Now;
                }
                basicTracking.UpdatedBy = userData != null ? userData.Principal.Identity.Name : "SYSTEM";
                basicTracking.UpdatedDate = DateTime.Now;
            }
        }

        protected bool IsProvider(PrintDB ctx, int CompanyID, int ProviderCompanyID, string ClientReference = null)
        {

            var range = new List<int>() { CompanyID };

            var resultFound = false;

            var currentLevel = 0;

            while(!resultFound && currentLevel < 3)
            {
                var pv = ctx.CompanyProviders
                    .Where(w => range.Any(a => a == w.CompanyID))
                    //.Where(w => w.ProviderCompanyID == ProviderCompanyID)
                    .ToList();

                currentLevel++;

                var found = pv.Where(w => w.ProviderCompanyID == ProviderCompanyID)
                    .Where(w => string.IsNullOrEmpty(ClientReference) || w.ClientReference == ClientReference);

                if(found.Any())
                {
                    resultFound = true;
                }
                else
                {
                    range.AddRange(pv.Select(s => s.ProviderCompanyID));
                    // try again
                }
            }

            return resultFound;

        }
    }
}
