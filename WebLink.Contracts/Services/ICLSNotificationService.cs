using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface ICLSNotificationService
    {
        bool SendDuplicatedEPCEMail(string factoryName, int orderId, List<string> epcList);
    }

    public class CLSNotificationService : ICLSNotificationService
    {
        private IFactory factory;
        private ILogSection log;
        private IAppConfig config;

        public CLSNotificationService(
            IFactory factory,
            ILogService log,
            IAppConfig config)
        {
            this.factory = factory;
            this.log = log.GetSection("DuplicatedEPCNotification");
            this.config = config;
        }

        public bool SendDuplicatedEPCEMail(string factoryName, int orderId, List<string> epcList)
        {
            try
            {
                var roles = config.GetValue<string>("WebLink.DuplicatedEPCMailRoles").Split(',').ToList();

                using (var userCtx = factory.GetInstance<IdentityDB>())
                {
                    var users = (from u in userCtx.Users
                                 join ur in userCtx.UserRoles on u.Id equals ur.UserId
                                 join r in userCtx.Roles on ur.RoleId equals r.Id
                                 where roles.Contains(r.Name) && u.CompanyID == 1
                                 select u).Distinct().ToList();
                    epcList.Insert(0, $"<table><tr><td> Factory: {factoryName} - OrderId: {orderId}</td></tr>");
                    epcList.Insert(1, $"<tr><td>The following EPC list seems to be duplicated after PrintLocal Synchronization:</td></tr>");

                    epcList.Add("</table>");
                    
                    var mailService = factory.GetInstance<IOrderEmailService>();

                    foreach (var user in users)
                    {
                        mailService.SendMessage(user.Email, "Duplicated EPC ", String.Join("\r\n", epcList), null).Wait();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.LogException($"Error while generating Duplicated EPC email", ex);

                return false;
            }
        }
    }
}