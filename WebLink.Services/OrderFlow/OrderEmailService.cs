using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class OrderEmailService : IOrderEmailService
    {
        private IFactory factory;
        private IUserManager userManager;
        private ILocalizationService g;
        private IAppConfig configuration;
        private IFileStore filestore;
        private IEmailTemplateService emailTemplateSrv;
        private ILogService log;

        public OrderEmailService(
            IFactory factory,
            IUserManager userManager,
            ILocalizationService g,
            IAppConfig configuration,
            IFileStoreManager storeManager,
            IEmailTemplateService emailTemplateSrv,
            ILogService log)
        {
            this.factory = factory;
            this.userManager = userManager;
            this.g = g;
            this.configuration = configuration;
            this.emailTemplateSrv = emailTemplateSrv;
            this.log = log;//.GetSection("OrderEmailSender");

            filestore = storeManager.OpenStore("OrderStore");
        }



        #region Emails list of the stakeholders

        public IEnumerable<string> GetSysAdminUsers()
        {
            throw new NotImplementedException();
        }


        public IEnumerable<string> GetIDTStakeHoldersUsers(int projectID, int? locationID)
        {
            var stakeholers = GetCustomersUsersByProject(projectID);
            IEnumerable<string> productionManagers = new List<string>();
            if(locationID.HasValue)
                productionManagers = GetProductionManagersUsersByLocation(locationID.Value);

            stakeholers.ToList().AddRange(productionManagers);

            return stakeholers;
        }


        public IEnumerable<string> GetCustomersUsersByOrder(int orderID)
        {
            var orderRepo = factory.GetInstance<IOrderRepository>();
            var projectRepo = factory.GetInstance<IProjectRepository>();

            var order = orderRepo.GetByID(orderID, true);
            return projectRepo.GetCustomerEmails(order.ProjectID);
        }

        public IEnumerable<string> GetCustomersUsersByProject(int projectID)
        {
            var projectRepo = factory.GetInstance<IProjectRepository>();
            return projectRepo.GetCustomerEmails(projectID);
        }

        public IEnumerable<string> GetProductionManagersUsersByLocation(int locationID)
        {
            var userRepo = factory.GetInstance<IUserRepository>();
            var prodManagers = userRepo.GetProdManagers(locationID)
                .Select(s => s.Id);
            return prodManagers;
        }

        public IEnumerable<string> GetProvidersUsersByOrder(int orderID)
        {
            var orderRepo = factory.GetInstance<IOrderRepository>();
            var companyrepo = factory.GetInstance<ICompanyRepository>();

            var order = orderRepo.GetByID(orderID, true);

            return companyrepo.GetContactEmails(order.SendToCompanyID);
        }

        public IEnumerable<string> GetClientUsersByProject(int projectID)
        {
            var projectRepo = factory.GetInstance<IProjectRepository>();

            return projectRepo.GetClientEmails(projectID);
        }

        #endregion Emails list of the stakeholders


        public IEmailToken GetTokenFromUser(string userid, EmailType type)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var existing = ctx.EmailTokens.Where(t => t.UserId == userid && t.Type == type).FirstOrDefault();
                if(existing == null)
                {
                    using(var userdb = factory.GetInstance<IdentityDB>())
                    {
                        var user = userdb.Users.FirstOrDefault(u => u.Id == userid);
                        if(user == null) return null;
                    }

                    Random rnd = new Random();
                    var usrHash = userid.GetHashCode();
                    var prefix = usrHash > 0 ? "A" : "N";
                    prefix += (char)rnd.Next(97, 123);
                    var code = prefix + Math.Abs(usrHash).ToString("D6") + ((int)type).ToString() + rnd.Next(0, 100000).ToString("D3");
                    existing = new EmailToken()
                    {
                        UserId = userid,
                        Code = code,
                        Type = type
                    };
                    ctx.EmailTokens.Add(existing);
                    ctx.SaveChanges();
                }
                return existing;
            }
        }


        public IEmailToken GetTokenFromCode(string code)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var existing = ctx.EmailTokens.Where(t => t.Code == code).FirstOrDefault();
                return existing;
            }
        }


        public void AddOrder(IEmailToken token, int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var item = ctx.EmailTokenItems.Where(t => t.EmailTokenID == token.ID && t.OrderID == orderid).FirstOrDefault();
                if(item == null)
                {
                    item = new EmailTokenItem()
                    {
                        EmailTokenID = token.ID,
                        OrderID = orderid,
                        Notified = false,
                        NotifyDate = null,
                        Seen = false,
                        SeenDate = null
                    };
                    ctx.EmailTokenItems.Add(item);
                }
                else
                {
                    item.Seen = false;
                    item.SeenDate = null;
                    item.Notified = false;
                    item.NotifyDate = null;
                }
                ctx.SaveChanges();
            }
        }


        public void AddOrderIfNotExists(IEmailToken token, int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var item = ctx.EmailTokenItems.Where(t => t.EmailTokenID == token.ID && t.OrderID == orderid).FirstOrDefault();
                if(item == null)
                {
                    item = new EmailTokenItem()
                    {
                        EmailTokenID = token.ID,
                        OrderID = orderid,
                        Notified = false,
                        NotifyDate = null,
                        Seen = false,
                        SeenDate = null
                    };
                    ctx.EmailTokenItems.Add(item);
                    ctx.SaveChanges();
                }
            }
        }



        public void MarkAsSeen(IEmailToken token, int[] orderids)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var items = from item in ctx.EmailTokenItems
                            join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                            where
                               tk.ID == token.ID &&
                               orderids.Contains(item.OrderID)
                            select item;

                foreach(var item in items)
                {
                    item.Seen = true;
                    item.SeenDate = DateTime.Now;
                }
                ctx.SaveChanges();
            }
        }


        public void AddErrorIfNotExist(IEmailToken token, ErrorNotificationType errorType, string title, string message, string key, int? projectId, int? locationId, int? orderId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var item = ctx.EmailTokenItemErrors.Where(t => t.EmailTokenID == token.ID && t.TokenKey == key).FirstOrDefault();

                if(item == null)
                {
                    item = new EmailTokenItemError()
                    {
                        EmailTokenID = token.ID,
                        TokenKey = key,
                        TokenType = errorType,
                        Title = title,
                        Message = message,
                        Notified = false,
                        NotifyDate = null,
                        Seen = false,
                        SeenDate = null,
                        ProjectID = projectId,
                        LocationID = locationId
                    };
                    ctx.EmailTokenItemErrors.Add(item);
                    ctx.SaveChanges();
                }
                // else maybe update counter
            }
        }


        public void MarkErrorAsSeen(IEmailToken token, int[] seenIDs)
        {
            //var seenIds = seen.Select(s => s.ID).ToList();

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var items = from item in ctx.EmailTokenItemErrors
                            join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                            where
                               tk.ID == token.ID &&
                               seenIDs.Contains(item.ID)
                            select item;

                foreach(var item in items)
                {
                    item.Seen = true;
                    item.SeenDate = DateTime.Now;
                }
                ctx.SaveChanges();
            }
        }

        public List<OrderEmailDetail> GetTokenOrders(IEmailToken token, bool includeNotified, bool includeSeen)
        {
            var user = GetUserInfo(token, out var _e);

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var testing = new List<int?>() { 1, Company.TEST_COMPANY_ID };

                var orders = from item in ctx.EmailTokenItems
                             join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                             join ord in ctx.CompanyOrders on item.OrderID equals ord.ID
                             join pj in ctx.PrinterJobs on ord.ID equals pj.CompanyOrderID
                             join art in ctx.Articles on pj.ArticleID equals art.ID
                             join proj in ctx.Projects on ord.ProjectID equals proj.ID
                             join brand in ctx.Brands on proj.BrandID equals brand.ID
                             join comp in ctx.Companies on brand.CompanyID equals comp.ID

                             join locmap in ctx.Locations on ord.LocationID equals locmap.ID into Locations
                             from loc in Locations.DefaultIfEmpty()

                                 //join tvmap in ctx.ProviderTrewView on ord.ProviderRecordID equals tvmap.ProviderRecordID into Providers
                             join tvmap in ctx.CompanyProviders on ord.ProviderRecordID equals tvmap.ID into Providers
                             from prv in Providers.DefaultIfEmpty()

                             where
                                tk.Code == token.Code
                                && (includeNotified || item.Notified == false)
                                && (includeSeen || item.Seen == false)
                                && (testing.Contains(user.CompanyID) || ord.SendToCompanyID != Company.TEST_COMPANY_ID)
                             orderby ord.OrderDate descending
                             select new OrderEmailDetail()
                             {
                                 OrderID = ord.ID,
                                 OrderNumber = ord.OrderNumber,
                                 CBP = $"{comp.Name} > {brand.Name} > {proj.Name}",
                                 Article = art.Name,
                                 Quantity = pj.Quantity,
                                 OrderDate = ord.OrderDate,
                                 ValidationDate = ord.ValidationDate,
                                 Status = ord.OrderStatus,
                                 ClientReference = prv != null ? prv.ClientReference : string.Empty,
                                 LocationName = loc != null ? loc.Name : string.Empty,
                                 ERPReference = ord.SageReference
                             };

                var ret = orders.AsNoTracking().ToList();
                //log.LogMessage($" Email: {_e} TokenType: {token.Type.ToString()} found orders to notify '{ret.Count}'");

                return ret;

            }



        }

        public List<OrderEmailDetail> GetPoolOrders(IEmailToken token, bool includeNotified, bool includeSeen)
        {

            var user = GetUserInfo(token, out string email);

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var orders = (from item in ctx.EmailTokenItems
                              join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                              join pool in ctx.OrderPools on item.OrderID equals pool.ID
                              join pj in ctx.Projects on pool.ProjectID equals pj.ID
                              join bn in ctx.Brands on pj.BrandID equals bn.ID

                              where
                              tk.Code == token.Code
                              && (includeNotified || item.Notified == false)
                              && (includeSeen || item.Seen == false)

                              orderby pool.CreationDate

                              select new OrderEmailDetail()
                              {
                                  OrderID = pool.ID,
                                  OrderNumber = pool.OrderNumber,
                                  Article = pool.ArticleCode,
                                  OrderDate = pool.CreationDate
                              }).Take(100).AsNoTracking();

                var ret = orders.ToList();

                return ret;
            }
        }


        public IEmailServiceSettings GetEmailServiceSettings(IEmailToken token)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var settings = ctx.EmailServiceSettings.AsNoTracking().FirstOrDefault(s => s.UserID == token.UserId);
                if(settings == null)
                {
                    settings = new EmailServiceSettings()
                    {
                        UserID = token.UserId,
                        NotifyOrderReceived = true,
                        NotifyOrderPendingValidation = true,
                        NotifyOrderValidated = true,
                        NotifyOrderConflict = true,
                        NotifyOrderCompleted = true,
                        NotifyOrderProcesingErrors = true,
                        NotifyOrderCancelled = false, // avoid to notify cancelled orders by default
                        NotificationPeriodInDays = 0, // no wait to receive the next notification
                        NotifyOrderPoolUpdate = true
                    };
                    ctx.EmailServiceSettings.Add(settings);
                    ctx.SaveChanges();
                }
                return settings;
            }
        }


        public void UpdateEmailServiceSettings(IEmailToken token, IEmailServiceSettings settings)
        {
            if(token.UserId != settings.UserID)
                throw new Exception("Invalid operation: Cannot update the settings of this user.");
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var actual = ctx.EmailServiceSettings.FirstOrDefault(s => s.UserID == token.UserId);
                if(actual == null) return;
                actual.NotifyOrderReceived = settings.NotifyOrderReceived;
                actual.NotifyOrderPendingValidation = settings.NotifyOrderPendingValidation;
                actual.NotifyOrderValidated = settings.NotifyOrderValidated;
                actual.NotifyOrderReadyForProduction = settings.NotifyOrderReadyForProduction;
                actual.NotifyOrderConflict = settings.NotifyOrderConflict;
                actual.NotifyOrderCompleted = settings.NotifyOrderCompleted;
                actual.NotificationPeriodInDays = settings.NotificationPeriodInDays;
                actual.NotifyOrderCancelled = settings.NotifyOrderCancelled;
                actual.NotifyOrderProcesingErrors = settings.NotifyOrderProcesingErrors;
                actual.NotifyOrderPoolUpdate = settings.NotifyOrderPoolUpdate;
                ctx.SaveChanges();
            }
        }


        /// <summary>
        /// Returns a list of all tokens (user/type combinations) that have items with the Notified flag set to false.
        /// </summary>
        public IEnumerable<IEmailToken> GetTokensWithPendingNotifications()
        {
            var connManager = factory.GetInstance<IDBConnectionManager>();
            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<EmailToken>(@"
                        select distinct token.* 
                        from EmailTokenItems item
                        join EmailTokens token on item.EmailTokenID = token.ID
                        where item.Notified = 0 and item.Seen = 0
                        UNION
                        (SELECT DISTINCT token.*
                        FROM EmailTokens token
                        INNER JOIN EmailTokenItemErrors errorItem
                        ON token.ID = errorItem.EmailTokenID
                        WHERE errorITem.Notified = 0 and errorItem.Seen = 0)
				");
            }
        }


        public IAttachmentData GetOrderPreview(IEmailToken token, int orderid)
        {
            var orders = GetTokenOrders(token, true, false);
            var found = orders.FirstOrDefault(o => o.OrderID == orderid);
            if(found != null)
            {
                IFileData file;
                if(filestore.TryGetFile(orderid, out file))
                {
                    var attachments = file.GetAttachmentCategory("Documents");
                    if(attachments.TryGetAttachment($"OrderPreview_{orderid}.pdf", out var attachment))
                        return attachment;
                }
            }
            return null;
        }

        public async Task SendNotifications()
        {
            try
            {
                var isQA = configuration.GetValue<bool>("WebLink.IsQA");
                if(!configuration.GetValue<bool>("WebLink.Email.Enabled"))
                    return;
                var tokens = GetTokensWithPendingNotifications();

                //log.LogEvent(LogLevel.Verbose, $"found '{tokens.Count()}' tokens to Send Notification Email", null, LogEntryType.Message);

                foreach(var token in tokens)
                {
                    try
                    {
                        var canSendEmail = CanSendEmail(token);
                        var user = GetUserInfo(token, out var email);
                        //log.LogMessage("User {0}  CanSendEmail {1} Token {2}", user.Email,  canSendEmail, Newtonsoft.Json.JsonConvert.SerializeObject(token));

                        // Ignore if the email is null or empty or user disable notifications
                        if(!canSendEmail || String.IsNullOrWhiteSpace(email))
                            continue;

                        // Ignore if QA flag is set and the email is not "@smartdots.es" or "@indetgroup.com"
                        var lowerCaseEmail = email.ToLower();
                        if(isQA && !(lowerCaseEmail.EndsWith("@smartdots.es") || lowerCaseEmail.EndsWith("@indetgroup.com")))
                            continue;

                        switch(token.Type)
                        {
                            case EmailType.OrderReceived:
                                await SendOrderReceivedEmail(token, email);
                                break;
                            case EmailType.OrderPendingValidation:
                                await SendOrderPendingValidationEmail(token, email);
                                break;
                            case EmailType.OrderValidated:
                                await SendOrderValidatedEmail(token, email);
                                break;
                            case EmailType.OrderConflict:
                                await SendOrderInConflictEmail(token, email);
                                break;
                            case EmailType.OrderReadyForProduction:
                                await SendOrderReadyForProductionEmail(token, email);
                                break;
                            case EmailType.OrderCompleted:
                                await SendOrderCompletedEmail(token, email);
                                break;
                            case EmailType.OrderProcessingError:
                                await SendOrderProcessingErrorEmail(token, email);
                                break;
                            case EmailType.OrderCancelled:
                                await SendOrderCancelledEmail(token, email);
                                break;
                            case EmailType.OrderResetValidation:
                                await SendOrderResetValidationEmail(token, email);
                                break;
                            case EmailType.OrderPoolUpdated:
                                await SendOrderPoolReceivedEmail(token, email);
                                break;
                        }
                    }
                    catch(Exception tkEx)
                    {
                        log.LogException($"Error to send Email for token [{token.ID}]", tkEx);
                    }

                }
            }
            catch(Exception ex)
            {

                log.LogException(ex);
            }
        }

        private async Task SendOrderReceivedEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false);
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of received orders"];
            info.Subtitle = g["The following orders have been received by the system, and should be ready to be validated."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Order Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "OrderDate", "ClientReference", "LocationName");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["We have received {0} new orders, this list shows only the latest ones.<br /> To see a complete list, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["We have received {0} new orders.<br /> To see additional information, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderPendingValidationEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, true, false).ToList();
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of orders pending validation"];
            info.Subtitle = g["The following orders are waiting to be validated, please remember that orders will not be produced until validated."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Order Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "OrderDate", "ClientReference", "LocationName");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["There are {0} orders waiting validation, this list shows only the latest ones.<br /> To see a complete list, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["There are {0} orders waiting validation.<br /> Please follow the link below to see additional information: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderValidatedEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false).ToList();
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of validated orders"];
            info.Subtitle = g["The following orders have been validated, production of these orders should start soon."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Validation Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.Column6 = g["Status"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "ValidationDate", "ClientReference", "LocationName", "Status");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["A total of {0} orders have been validated since the last time you visited our portal, this list shows only the latest ones. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["A total of {0} orders have been validated since the last time you visited our portal. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];
            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderInConflictEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false).ToList();
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of conflicting orders"];
            info.Subtitle = g["The following orders have been marked as <b>InConflict</b>, this is due to having received the same order múltiple times with different information."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Order Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "OrderDate", "ClientReference", "LocationName");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            info.HTMLComment = g["There are {0} notifications of this type pending your review, this list shows only the latest ones. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderReadyForProductionEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false).ToList();
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of orders ready for production"];
            info.Subtitle = g["The following orders have been processed and are ready for production."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Validation Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.Column6 = g["Status"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "ValidationDate", "ClientReference", "LocationName", "Status");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            info.HTMLComment = g["There are {0} notifications of this type pending your review, this list shows only the latest ones. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderCompletedEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false).ToList();
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Summary of completed orders"];
            info.Subtitle = g["The following orders have been completed and should be delivered to the designated address soon."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Validation Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.Column6 = g["Status"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "CBP", "Article", "Quantity", "ValidationDate", "ClientReference", "LocationName", "Status");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["A total of {0} orders have been completed since the last time you visited our portal, this list shows only the latest ones. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["A total of {0} orders have been validated since the last time you visited our portal. Please, follow the link below to dismiss these notifications: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, orders);
        }


        private async Task SendOrderProcessingErrorEmail(IEmailToken token, string email)
        {
            var errors = GetTokenErrors(token, false, false).ToList();
            if(errors.Count == 0) return;

            var info = new ErrorEmailtTemplateInfo();
            info.Title = g["Summary of Error Notifications found in Order Processing"];
            info.Subtitle = g["The following list is a summary of the different issues found"];
            info.Column1 = g["Type"];
            info.Column2 = g["Message"];
            info.Column3 = g["Factory"];
            info.Column4 = g["Customer"];
            info.HTMLTable = CreateOrderProcessingErrorTable(errors, "TypeDescription", "Message", "Location", "Company");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(errors.Count > 10)
                info.HTMLComment = g["A total of {0} issues have been found since the last time you visited our portal, this list shows only the latest ones. Please, follow the link below to dismiss these notifications: <br /> {1}email/error?{2}", errors.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["A total of {0} issues have been found since the last time you visited our portal. Please, follow the link below to dismiss these notifications: <br /> {1}email/error?{2}", errors.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info);
            MarkItemsAsNotified(token, errors);
        }

        private async Task SendOrderCancelledEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false);
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Orders Cancelled"];
            info.Subtitle = g["The following orders have been cancelled, and should be required to update ERP Order."];
            info.Column1 = g["Order N°"];
            info.ERPReference = g["ERP Ref."];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Order Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "ERPReference", "CBP", "Article", "Quantity", "OrderDate", "ClientReference", "LocationName");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["Cancelled orders ({0}), this list shows only the latest ones.<br /> To see a complete list, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["Cancelled orders ({0}).<br /> To see additional information, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info, "OrderWithERPEmailTemplate.html");
            MarkItemsAsNotified(token, orders);
        }

        private async Task SendOrderResetValidationEmail(IEmailToken token, string email)
        {
            var orders = GetTokenOrders(token, false, false);
            if(orders.Count == 0) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Orders to Repeat Validation"];
            info.Subtitle = g["The following orders have been reseted validation process, and should be required to update ERP Order."];
            info.Column1 = g["Order N°"];
            info.ERPReference = g["ERP Ref."];
            info.Column2 = g["Client > Brand > Project"];
            info.Column3 = g["Article"];
            info.Column4 = g["Quantity"];
            info.Column5 = g["Order Date"];
            info.ClientReference = g["Client Refrence"];
            info.LocationName = g["Factory"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "ERPReference", "CBP", "Article", "Quantity", "OrderDate", "ClientReference", "LocationName");

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
            if(orders.Count > 10)
                info.HTMLComment = g["Repeat validation process for {0} orders, this list shows only the latest ones.<br /> To see a complete list, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            else
                info.HTMLComment = g["Repeat validation process for {0} orders.<br /> To see additional information, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];
            info.Copyright = g["Copyright"];

            await SendMessage(email, info, "OrderWithERPEmailTemplate.html");
            MarkItemsAsNotified(token, orders);
        }

        private async Task SendOrderPoolReceivedEmail(IEmailToken token, string email)
        {

            var orders = GetPoolOrders(token, false, false);
            if(orders.Count < 1) return;

            var info = new OrderEmailTemplateInfo();
            info.Title = g["Orders Received"];
            info.Subtitle = g["The following orders have been received and are awaiting supplier assignment."];
            info.Column1 = g["Order N°"];
            info.Column2 = g["Article"];
            info.Column3 = g["Order Date"];
            info.HTMLOrders = CreateOrderTable(orders, "OrderNumber", "Article", "OrderDate");
            info.Copyright = g["Copyright"];

            var baseUrl = configuration.GetValue<string>("WebLink.BaseUrl");
           
            info.HTMLComment = g["Here are listing only the latest ones.<br /> To see a complete list, please follow the link below: <br /> {1}email?{2}", orders.Count, baseUrl, token.Code];

            await SendMessage(email, info, "OrderPoolReceivedEmailTemplate.html");

            MarkItemsAsNotified(token, orders);
        }

        public IEnumerable<ErrorEmailDetail> GetTokenErrors(IEmailToken token, bool includeNotified, bool includeSeen)
        {
            var user = GetUserInfo(token, out var _e);

            var connManager = factory.GetInstance<IDBConnectionManager>();
            using(var conn = connManager.OpenWebLinkDB())
            {
                var tokenID = token.ID;

                var errors = conn.Select<ErrorEmailDetail>(@"
						SELECT 
							errorItem.ID AS ItemID
							,errorItem.EmailTokenID
							,errorItem.Title
							,errorItem.Message
							,errorItem.TokenType AS [Type]
							,l.Name as [Location]
							,c.Name as [Company]
						FROM EmailTokens token
						INNER JOIN EmailTokenItemErrors errorItem ON token.ID = errorItem.EmailTokenID
						LEFT JOIN Locations l ON l.ID = errorItem.LocationID
						LEFT JOIN Projects p ON p.ID = errorItem.ProjectID
						LEFT JOIN Brands b ON b.ID = p.BrandID
						LEFT JOIN Companies c ON c.ID = b.CompanyID
						WHERE errorItem.ID IN (
							SELECT  
							MIN(ID) AS ID
							--,[EmailTokenID]
							--,[TokenKey]
							FROM [EmailTokenItemErrors]
							WHERE EmailTokenID = @tokenID
							AND (@includeNotified = 1 OR Notified = 0)
							AND (@includeSeen = 1 OR Seen = 0)
                            AND ( @userId in (@smartdotsID,@TestingCompanyID) OR c.ID <>  @TestingCompanyID2 )
							GROUP BY [EmailTokenID], [TokenKey]
						)
				", tokenID, includeNotified, includeSeen, user.CompanyID.HasValue ? user.CompanyID.Value : 0, 1, Company.TEST_COMPANY_ID, Company.TEST_COMPANY_ID).ToList();

                return errors;
            }
        }

        //private async Task SendMessage(string email, OrderEmailTemplateInfo info)
        //{

        //	// Change to check if configuration is set to QA
        //	//#if DEBUG
        //	//			email = "rafael.guerrero@indetgroup.com";
        //	//#endif
        //	var msg = emailTemplateSrv.CreateFromTemplate($"wwwroot\\OrderEmailTemplate.html", info);
        //	msg.To = email;
        //	msg.Subject = info.Title;
        //	msg.EmbbedImage("logo", "wwwroot\\images\\SDS_LOGOMail.png");
        //	await msg.SendAsync();
        //}

        private async Task SendMessage(string email, OrderEmailTemplateInfo info, string htmlTemplate = "OrderEmailTemplate.html")
        {
            if(string.IsNullOrEmpty(htmlTemplate))
            {
                htmlTemplate = "OrderEmailTemplate.html";

            }

            var msg = emailTemplateSrv.CreateFromTemplate($"wwwroot\\EmailTemplates\\{htmlTemplate}", info);

#if DEBUG

            info.Title = info.Title + " To: " + email.Replace("@", "[at]");
            email = "rafael.smartdots@gmail.com";

#endif

            msg.To = email;
            msg.Subject = info.Title;
            msg.EmbbedImage("logo", "wwwroot\\images\\SDS_LOGOMail.png");
            await msg.SendAsync();
        }

        private async Task SendMessage(string email, ErrorEmailtTemplateInfo info)
        {
            // Change to check if configuration is set to QA
#if DEBUG

            info.Title = info.Title + " To: " + email.Replace("@", "[at]");
            email = "rafael.smartdots@gmail.com";

#endif
            var msg = emailTemplateSrv.CreateFromTemplate("wwwroot\\EmailTemplates\\ErrorEmailTemplate.html", info);
            msg.To = email;
            msg.Subject = info.Title;
            msg.EmbbedImage("logo", "wwwroot\\images\\SDS_LOGOMail.png");
            await msg.SendAsync();
        }


        public async Task SendMessage(string email, string subject, string body, List<string> files)
        {
            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if(string.IsNullOrWhiteSpace(subject))
                throw new ArgumentNullException(nameof(subject));
            if(string.IsNullOrWhiteSpace(body))
                throw new ArgumentNullException(nameof(body));

            var msg = factory.GetInstance<EmailObject>();
            msg.To = email;
            msg.Subject = subject;
            msg.Body = body;
            if(files != null)
            {
                foreach(string file in files)
                    msg.AttachFile(file);
            }
            await msg.SendAsync();
        }

        private void MarkItemsAsNotified(IEmailToken token, List<OrderEmailDetail> orders)
        {
            var orderids = (from o in orders select o.OrderID).ToList();
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var items = from item in ctx.EmailTokenItems
                            join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                            where
                               tk.ID == token.ID &&
                               orderids.Contains(item.OrderID)
                            select item;

                foreach(var item in items)
                {
                    item.Notified = true;
                    item.NotifyDate = DateTime.Now;
                }
                ctx.SaveChanges();
            }
        }

        private void MarkItemsAsNotified(IEmailToken token, List<ErrorEmailDetail> details)
        {
            var ids = details.Select(s => s.ItemID);
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var items = from item in ctx.EmailTokenItemErrors
                            join tk in ctx.EmailTokens on item.EmailTokenID equals tk.ID
                            where
                               tk.ID == token.ID &&
                               ids.Contains(item.ID)
                            select item;

                foreach(var item in items)
                {
                    item.Notified = true;
                    item.NotifyDate = DateTime.Now;
                }
                ctx.SaveChanges();
            }
        }


        private bool CanSendEmail(IEmailToken token)
        {
            var settings = GetEmailServiceSettings(token);
            switch(token.Type)
            {
                case EmailType.OrderReceived:
                    if(!settings.NotifyOrderReceived)
                        return false;
                    break;
                case EmailType.OrderPendingValidation:
                    if(!settings.NotifyOrderPendingValidation)
                        return false;
                    break;
                case EmailType.OrderValidated:
                    if(!settings.NotifyOrderValidated)
                        return false;
                    break;
                case EmailType.OrderConflict:
                    if(!settings.NotifyOrderConflict)
                        return false;
                    break;
                case EmailType.OrderReadyForProduction:
                    if(!settings.NotifyOrderReadyForProduction)
                        return false;
                    break;
                case EmailType.OrderCompleted:
                    if(!settings.NotifyOrderCompleted)
                        return false;
                    break;
                case EmailType.OrderProcessingError:
                    if(!settings.NotifyOrderProcesingErrors)
                        return false;
                    break;
                case EmailType.OrderCancelled:
                    if(!settings.NotifyOrderCancelled)
                        return false;
                    break;
            }
            var latestEmail = GetLatestEmailNotification(token.ID);
            if((DateTime.Now - latestEmail).TotalDays > settings.NotificationPeriodInDays)
                return true;
            else
                return false;
        }


        private IAppUser GetUserInfo(IEmailToken token, out string email)
        {
            email = null;
            var user = userManager.FindByIdAsync(token.UserId).Result;
            if(user != null)
            {
                if(!String.IsNullOrWhiteSpace(user.Language))
                    CultureInfo.CurrentUICulture = new CultureInfo(user.Language);
                email = user.Email;
            }

            return user;
        }


        private DateTime GetLatestEmailNotification(int tokenid)
        {
            var connManager = factory.GetInstance<IDBConnectionManager>();
            using(var conn = connManager.OpenWebLinkDB())
            {
                var date = conn.SelectColumn<DateTime?>(@"select Max(NotifyDate) as NotifyDate from EmailTokenItems where EmailTokenID = @tokenid", tokenid)[0];
                if(date == null) return DateTime.MinValue;
                else return date.Value;
            }
        }


        private string CreateOrderTable(List<OrderEmailDetail> orders, params string[] columns)
        {
            if(columns == null || columns.Length == 0)
                throw new Exception("columns cannot be null or empty");
            var sb = new StringBuilder(1000);
            var count = 0;
            foreach(var order in orders)
            {
                sb.Append("<tr>");
                foreach(var col in columns)
                {
                    sb.Append($"<td>{Reflex.GetMember(order, col)}</td>");
                }
                sb.Append("</tr>\r\n");
                count++;
                if(count >= 10) break;
            }
            return sb.ToString();
        }

        private string CreateOrderProcessingErrorTable(List<ErrorEmailDetail> items, params string[] columns)
        {
            if(columns == null || columns.Length == 0)
                throw new Exception("columns cannot be null or empty");
            var sb = new StringBuilder(1000);
            var count = 0;
            foreach(var order in items)
            {
                sb.Append("<tr>");
                foreach(var col in columns)
                {
                    sb.Append($"<td>{Reflex.GetMember(order, col)}</td>");
                }
                sb.Append("</tr>\r\n");
                count++;
                if(count >= 10) break;
            }
            return sb.ToString();
        }
    }

    class OrderEmailTemplateInfo
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Column1 = string.Empty;
        public string Column2 = string.Empty;
        public string Column3 = string.Empty;
        public string Column4 = string.Empty;
        public string Column5 = string.Empty;
        public string Column6 = string.Empty;
        public string ClientReference = string.Empty;
        public string ERPReference = string.Empty;
        public string LocationName = string.Empty;
        public string HTMLOrders = string.Empty;
        public string HTMLComment = string.Empty;
        public string Copyright = string.Empty;
        public string BaseUrl = string.Empty;
    }

    class ErrorEmailtTemplateInfo
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Column1 = string.Empty;
        public string Column2 = string.Empty;
        public string Column3 = string.Empty;
        public string Column4 = string.Empty;
        internal string HTMLComment = string.Empty;
        internal string Copyright = string.Empty;
        internal string HTMLTable = string.Empty;
        public string BaseUrl = string.Empty;
    }

}
