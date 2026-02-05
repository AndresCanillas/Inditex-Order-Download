using OrderDownloadWebApi.Models.Repositories;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Linq;

namespace OrderDownloadWebApi.Processing
{
    public class ProcessUserChange : EQEventHandler<SmartdotsUserChangedEvent>
    {
        private IUserRepository repo;
        private IAppLog log;

        public ProcessUserChange(IUserRepository repo, IAppLog log)
        {
            this.repo = repo;
            this.log = log;
        }

        public override EQEventHandlerResult HandleEvent(SmartdotsUserChangedEvent e)
        {
            try
            {
                log.LogMessage($"Handling smartdots user change event. Operation: {e.Operation}, User: {e.UserName}.");
                switch(e.Operation)
                {
                    case Operation.ProfileChanged:
                        HandleProfileChanged(e);
                        break;
                    case Operation.RoleAdded:
                        HandleRoleAdded(e);
                        break;
                    case Operation.RoleRemoved:
                        HandleRoleRemoved(e);
                        break;
                    case Operation.UserDeleted:
                        HandleUserDeleted(e);
                        break;
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
            return new EQEventHandlerResult();
        }

        private void HandleProfileChanged(SmartdotsUserChangedEvent e)
        {
            var user = repo.GetByName(e.UserName);
            if(user == null)
            {
                user = repo.Create();
                user.CreatedDate = DateTime.Now;
            }
            user.UpdatedDate = DateTime.Now;
            user.UserId = e.Id;
            user.Name = e.UserName;
            user.FirstName = e.FirstName;
            user.LastName = e.LastName;
            user.PhoneNumber = e.PhoneNumber;
            user.Email = e.Email;
            user.Language = e.Language;
            if(!String.IsNullOrWhiteSpace(e.PwdHash))
                user.PwdHash = e.PwdHash;
            if(e.Roles != null)
                user.Roles = e.Roles;
            if(user.IsNew)
                repo.Insert(user);
            else
                repo.Update(user);
        }

        private void HandleRoleAdded(SmartdotsUserChangedEvent e)
        {
            var user = repo.GetByName(e.UserName);
            if(user == null) return;
            var roles = user.Roles.Split(',').ToList();
            var existingRole = roles.Where(p => String.Compare(p, e.Role, true) == 0).FirstOrDefault();
            if(existingRole != null) return; // Role is already regisered
            roles.Add(e.Role);
            user.Roles = roles.Merge(",");
            repo.Update(user);
        }

        private void HandleRoleRemoved(SmartdotsUserChangedEvent e)
        {
            var user = repo.GetByName(e.UserName);
            if(user == null) return;
            var roles = user.Roles.Split(',').ToList();
            roles.RemoveAll(p => String.Compare(p, e.Role, true) == 0);
            user.Roles = roles.Merge(",");
            repo.Update(user);
        }

        private void HandleUserDeleted(SmartdotsUserChangedEvent e)
        {
            var user = repo.GetByName(e.UserName);
            if(user == null) return;
            repo.Delete(user);
        }
    }
}
