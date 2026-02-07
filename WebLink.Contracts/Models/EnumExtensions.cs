namespace WebLink.Contracts.Models
{
    public static class EnumExtensions
    {

        #region OrderStatus 
        public static string GetText (this OrderStatus os, bool IsIDT = false)
        {
            
            if (IsIDT)
            {
                return GetTextForOwner(os);
            }

            return GetTextForCustomers(os);
        }

        public static string GetTextForCustomers(this OrderStatus os)
        {
            string ret = "Unknown Order State";

            switch (os)
            {
                case OrderStatus.None:
                    ret = "All";
                    break;

                case OrderStatus.Received:
                    ret = "Received";
                    break;

                case OrderStatus.Processed:
                    ret = "Processed";
                    break;

                case OrderStatus.InFlow:
                    ret = "In Validation";
                    break;

                case OrderStatus.Validated:
                    ret = "Validated";
                    break;

                case OrderStatus.Billed:
                    ret = "Sync With ERP";
                    break;

                case OrderStatus.ProdReady:
                    ret = "Ready to Print";
                    break;

                case OrderStatus.Printing:
                    ret = "Printing";
                    break;

                case OrderStatus.Completed:
                    ret = "Completed";
                    break;

                case OrderStatus.Cancelled:
                    ret = "Cancelled";
                    break;
                case OrderStatus.CompoAuditNeeded:
                    ret = "Composition Audit Needed";
                    break; 
                default:
                    ret = "Unknown status";
                    break;

            }


            return ret;
        }

        public static string GetTextForOwner(this OrderStatus os)
        {
            string ret = "Unknown Order State";

            switch (os)
            {
                case OrderStatus.None:
                    ret = "All";
                    break;

                case OrderStatus.Received:
                    ret = "Received";
                    break;

                case OrderStatus.Processed:
                    ret = "Processed";
                    break;

                case OrderStatus.InFlow:
                    ret = "In Validation";
                    break;

                case OrderStatus.Validated:
                    ret = "Validated";
                    break;

                case OrderStatus.Billed:
                    ret = "Sync With ERP";
                    break;

                case OrderStatus.ProdReady:
                    ret = "Ready to Print";
                    break;

                case OrderStatus.Printing:
                    ret = "Printing";
                    break;

                case OrderStatus.Completed:
                    ret = "Completed";
                    break;

                case OrderStatus.Cancelled:
                    ret = "Cancelled";
                    break;

                default:
                    ret = "Unknown status";
                    break;
            }

            return ret;
        }

        #endregion OrderStatus

        #region OrderReportFilter
        public static string GetText (this BilledFilter en)
        {
            string ret = string.Empty;

            switch(en)
            {
                case BilledFilter.No:
                    ret = "No";
                    break;

                case BilledFilter.Yes:
                    ret = "Yes";
                    break;

                case BilledFilter.Ignore:
                default:
                    ret = "All";
                    break;
            }

            return ret;
        }

        public static string GetText(this StopFilter en)
        {
            string ret = string.Empty;

            switch (en)
            {
                case StopFilter.NoStoped:
                    ret = "Running";
                    break;

                case StopFilter.Stoped:
                    ret = "In Hold";
                    break;

                case StopFilter.Ignore:
                default:
                    ret = "All";
                    break;
            }

            return ret;
        }

        public static string GetText(this ConflictFilter en)
        {
            string ret = string.Empty;

            switch (en)
            {
                case ConflictFilter.NoConflict:
                    ret = "Without Conflict";
                    break;

                case ConflictFilter.InConflict:
                    ret = "In Conflict";
                    break;

                case ConflictFilter.Ignore:
                default:
                    ret = "All";
                    break;
            }

            return ret;
        }
        #endregion OrderReportFilter


        public static string GetText(this ProductionType en)
        {

            string ret = string.Empty;

            switch (en)
            {

                case ProductionType.IDTLocation:
                    ret = "IDT";
                    break;

                case ProductionType.CustomerLocation:
                    ret = "Local";
                    break;

                case ProductionType.All:
                default:
                    ret = "All";

                    break;
            }

            return ret;
        }

        public static string GetText(this ProjectSetProductionType en)
        {
            string ret = string.Empty;

            switch (en)
            {
                case ProjectSetProductionType.SetAsLocalStrategy:
                    ret = "As Local";
                    break;

                case ProjectSetProductionType.SetLocalAsFirstStrategy:
                    ret = "Like Local (if possible)";
                    break;

                case ProjectSetProductionType.SetIDTFactoryStrategy:
                default:
                    ret = "As IDT Production";
                    break;
            }

            return ret;
        }

        public static string GetText(this OrderLogLevel en)
        {
            string ret = string.Empty;

            switch (en)
            {
                case OrderLogLevel.ERROR:
                    ret = "ERROR";
                    break;

                case OrderLogLevel.WARN:
                    ret = "WARN";
                    break;

                case OrderLogLevel.INFO:
                    ret = "WARN";
                    break;

                case OrderLogLevel.DEBUG:
                default:
                    ret = "DEBUG";
                    break;
            }

            return ret;
        }
    
    
        public static string GetText (this SageOrderStatus en)
        {
            var ret = string.Empty;

            switch(en)
            {
                case SageOrderStatus.Closed:
                    ret = "Closed";
                    break;

                case SageOrderStatus.Open:
                    ret = "Open";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }

        public static string GetText(this SageInvoiceStatus en)
        {
            var ret = string.Empty;

            switch (en)
            {
                case SageInvoiceStatus.Invoiced:
                    ret = "Invoiced";
                    break;

                case SageInvoiceStatus.NoInvoiced:
                    ret = "Not Invoiced";
                    break;

                case SageInvoiceStatus.PartialInvoiced:
                    ret = "Partial Invoiced";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }

        public static string GetText(this SageDeliveryStatus en)
        {
            var ret = string.Empty;

            switch (en)
            {
                case SageDeliveryStatus.Shipped:
                    ret = "Shipped";
                    break;

                case SageDeliveryStatus.NoShipped:
                    ret = "Not Shipped";
                    break;

                case SageDeliveryStatus.PartialShipped:
                    ret = "Partial Shipped";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }

        public static string GetText(this SageCreditStatus en)
        {
            var ret = string.Empty;

            switch (en)
            {
                case SageCreditStatus.OK:
                    ret = "Ok";
                    break;

                case SageCreditStatus.Locked:
                    ret = "Locked";
                    break;

                case SageCreditStatus.PrepaymentNotDeposited:
                    ret = "Prepayment Not Deposited";
                    break;

                case SageCreditStatus.LimitExceeded:
                    ret = "Limit Exceeded";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }


        public static string GetText(this NotificationType en)
        {
            var ret = string.Empty;

            switch (en)
            {
                case NotificationType.All:
                    ret = "All";
                    break;

                case NotificationType.Error:
                    ret = "Errors";
                    break;

                case NotificationType.OrderTracking:
                    ret = "Order Tracking";
                    break;

                case NotificationType.OrderImportError:
                    ret = "Import Error";
                    break;

                case NotificationType.FTPFileWhatcher:
                    ret = "FTP Import Service";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }


        public static string GetText(this ErrorNotificationType en)
        {
            var ret = string.Empty;

            switch (en)
            {
                case ErrorNotificationType.ArticleNotFound:
                    ret = "Article not found";
                    break;

                case ErrorNotificationType.CompanyNotFound:
                    ret = "Company Provider not found";
                    break;

                case ErrorNotificationType.MappingNotFound:
                    ret = "Mapping Configuration not found";
                    break;

                case ErrorNotificationType.SageError:
                    ret = "SAGE Error";
                    break;

                //case ErrorNotificationType.SystemError:
                default:
                    ret = "System Error";
                    break;
            }

            return ret;
        }


        public static string GetText(this CITemplateConfig en)
        {

            string ret = string.Empty;

            switch (en)
            {
                case CITemplateConfig.Enabled:
                    ret = "Enabled";
                    break;
                case CITemplateConfig.Forced:
                    ret = "Forced";
                    break;
                case CITemplateConfig.Disabled:
                default:
                    ret = "Disabled";
                    break;
            }
            return ret;
        }

        public static string GetText(this MadeInEnable en)
        {
            string ret = string.Empty;

            switch (en)
            {
                case MadeInEnable.YES:
                    ret = "Yes";
                    break;
                case MadeInEnable.VISIBLE:
                    ret = "Visible";
                    break;
                case MadeInEnable.NO:
                default:
                    ret = "No";
                    break;
            }
            return ret;
        }

        public static string GetText(this DeliveryStatus en)
        {
            var ret = string.Empty;

            switch(en)
            {
                case DeliveryStatus.NotSet:
                    ret = "";
                    break;

                case DeliveryStatus.Pending:
                    ret = "Pending";
                    break;

                case DeliveryStatus.Shipped:
                    ret = "Shipped";
                    break;

                case DeliveryStatus.PartiallyShipped:
                    ret = "Partial Shipped";
                    break;

                case DeliveryStatus.Delivered:
                    ret = "Delivered";
                    break;

                default:
                    ret = "Unknow";
                    break;
            }

            return ret;
        }


        public static string GetText(this DocumentDownloadOption en)
        {
            string ret = string.Empty;

            switch(en)
            {
                case DocumentDownloadOption.NO:
                    ret = "No";
                    break;
                case DocumentDownloadOption.YES:
                default:
                    ret = "Yes";
                    break;
            }
            return ret;
        }
    }
}
