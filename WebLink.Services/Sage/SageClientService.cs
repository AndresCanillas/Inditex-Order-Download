using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Sage;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Xml;
using Services.Core;

namespace WebLink.Services.Sage
{

    public class SageException : SystemException
    {
        public SageException(string message) : base(message) { }
        public SageException(string message, Exception innerException): base(message, innerException) { }
    }

    public class SageObjectNotFoundException : SageException
    {
        public SageObjectNotFoundException(string message) : base(message) { }
        public SageObjectNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    }

    public class SageRegisterOrderException : SageException
    {
        public SageRegisterOrderException(string message) : base(message) { }
        public SageRegisterOrderException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SageQueryItemException : SageException
    {
        public SageQueryItemException(string message) : base(message) { }
        public SageQueryItemException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SageClientService : ISageClientService
    {
        private static HttpClient client;
        private IAppConfig appConfig;
        private IFactory factory;
        private ILocalizationService g;
        private bool enableLog;
        public bool enableFullReadResponseLog { get; private set; }

        public SageClientService(
            IFactory factory,
            IAppConfig appInfo,
            ILocalizationService g
            )
        {
            this.factory = factory;
            this.appConfig = appInfo;
            this.g = g;
            enableLog = appConfig.GetValue<bool>("WebLink.Sage.LogMessages");
            enableFullReadResponseLog = appConfig.GetValue<bool>("WebLink.Sage.FullReadResponseLog", false);
        }

        public async Task<string> Request(string xml, string url, string soapAction)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Post
            };

            request.Content = new StringContent(xml, Encoding.UTF8, "text/xml");
            request.Headers.Clear();
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            //request.Content.Headers.ContentEncoding = Encoding.UTF8;
            request.Headers.Add("SOAPAction", new List<string> { soapAction });

            var result = await GetClient().SendAsync(request);

            return await result.Content.ReadAsStringAsync();
        }

        private async Task<WsResponseRead> Request(WsRequestRead rq, string msgKey)
        {
            var str = XmlTools.ToXmlString(rq);
            LogMessage($"Sage Read Request [{msgKey}]", str);

            var response = await Request(str, rq.Url, rq.Body.Operation.SoapAction());
            WsResponseRead result = XmlTools.GetObjectFromXml<WsResponseRead>(response);

            if (enableFullReadResponseLog)
            {
                LogMessage($"Sage Full READ Response [{msgKey}]", response);
            }

            if (string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                LogMessage($"Sage ReadError Response [{msgKey}]", response);
            }

            result.Body.Response.Return.Messages = GetResponseMessages(response).ToList();

            return result;
        }

        private async Task<WsResponseSave> Request(WsRequestSave rq, string msgKey)
        {
            var str = XmlTools.ToXmlString(rq);
            LogMessage($"Sage Save Request [{msgKey}]", str);

            var response = await Request(str, rq.Url, rq.Body.Operation.SoapAction());
           
            WsResponseSave result = XmlTools.GetObjectFromXml<WsResponseSave>(response);

            if (string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                LogMessage($"Sage SaveError Response [{msgKey}]", response);
            }

            result.Body.Response.Return.Messages = GetResponseMessages(response).ToList();

            return result;
        }

        private async Task<WsResponseQuery> Request(WsRequestQuery rq)
        {
            var str = XmlTools.ToXmlString(rq);
            LogMessage("Query Request", str);

            var response = await Request(str, rq.Url, rq.Body.Operation.SoapAction());

            WsResponseQuery result = XmlTools.GetObjectFromXml<WsResponseQuery>(response);

            if (string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                LogMessage("Sage QueryError Response", response);
            }

            result.Body.Response.Return.Messages = GetResponseMessages(response).ToList();

            return result;
        }

        private async Task<WsResponseUpdate> Request(WsRequestUpdate rq, string msgKey)
        {
            var str = XmlTools.ToXmlString(rq);
            LogMessage($"Sage Update Request [{msgKey}]", str);

            var response = await Request(str, rq.Url, rq.Body.Operation.SoapAction());

            WsResponseUpdate result = XmlTools.GetObjectFromXml<WsResponseUpdate>(response);

            if (string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                LogMessage($"Sage UpdateError Response [{msgKey}]", response);
            }

            result.Body.Response.Return.Messages = GetResponseMessages(response).ToList();

            return result;
        }

        private IEnumerable<WsMessage> GetResponseMessages(string xml)
        {

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var messagesNodes = doc.SelectNodes("//messages");
            var multiRefNodes = doc.SelectNodes("//multiRef");

            List<WsMessage> messages = new List<WsMessage>();

            Action<XmlNode> getMessage = (nd) =>
            {
                string type = nd.SelectSingleNode("type").InnerText;
                string msg = nd.SelectSingleNode("message").InnerText;

                if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(msg))
                {
                    messages.Add(new WsMessage()
                    {
                        Type = (WsMessageType)Enum.Parse(typeof(WsMessageType), type),
                        Message = msg
                    });
                }
            };

            foreach (XmlNode nd in messagesNodes)
                getMessage(nd);
            

            foreach (XmlNode nd in multiRefNodes)
                getMessage(nd);



            return messages;
        }


        private HttpClient GetClient()
        {
            if (client == null)
            {
                client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip, ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; } }) { Timeout = TimeSpan.FromMinutes(2) };
                // add credentials
                //var byteArray = Encoding.ASCII.GetBytes($"wstest:$defaultSysConfigPWD128");
                var byteArray = Encoding.ASCII.GetBytes($"{appConfig.GetValue<string>("WebLink.Sage.User")}:{appConfig.GetValue<string>("WebLink.Sage.Password")}");
                var header = new AuthenticationHeaderValue(
                           "Basic", Convert.ToBase64String(byteArray));
                client.DefaultRequestHeaders.Authorization = header;

            }

            return client;
        }

        private void LogMessage(string msg, string xml)
        {
            if (enableLog == false) return;

            var appLog = factory.GetInstance<ILogService>().GetSection("Sage");
            appLog.LogMessage(msg);
            if (!string.IsNullOrEmpty(xml))
            {
                appLog.LogMessage(xml);
            }
        }

        private WsContext GetContext()
        {
            return new WsContext()
            {
                CodeLang = appConfig["WebLink.Sage.CodeLang"],
                PoolAlias = appConfig["WebLink.Sage.PoolName"],
                PoolId = appConfig["WebLink.Sage.PoolID"],
                RequestConfig = appConfig["WebLink.Sage.RequestConfig"],
            };
        }

        #region Article / Items
        public async Task<ISageItem> GetItemDetail(string itemRef)
        {
            var msg = new WsRequestRead()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodyRead()
                {
                    Operation = new WsRead()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Article"],
                        ObjectKeys = new List<WsKey>() {
                            new WsKey() { Key = "ITMREF", Value = itemRef }
                        }
                    }
                }
            };

            var result = await Request(msg, itemRef);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageObjectNotFoundException(String.Format("{0} - {1}:'{2}' - Sage Text: '{3}'", g["Item not found"], "ITEMREF", itemRef, string.Join( '-', result.Body.Response.Return.Messages.Select(m => m.Message)) ));
            }

            var item = new Itm()
            {
                Result = XmlTools.GetObjectFromXml<ResultObject>(result.Body.Response.Return.ResultXml)
            };

            return item;
        }

        public async Task<IEnumerable<ISageItemQuery>> GetAllItemsByTerm(IEnumerable<IWsKey> keys, int listSize)
        {
            var msg = new WsRequestQuery()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodyQuery()
                {
                    Operation = new WsQuery()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Article"],
                        ListSize = listSize.ToString()
                    }
                }
            };
            
            // ???: esto no esta bien hecho, las interfaces no deberian convertirse a una clase concreta
            // https://stackoverflow.com/a/6019812
            // pero el xmlserializer se queja
            msg.Body.Operation.ObjectKeys = keys.Cast<WsKey>().ToList<WsKey>();

            var result = await Request(msg);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageQueryItemException("Errot to execute Query to " + msg.Body.Operation.PublicName);
            }

            QueryItemResultObject itemsFound = XmlTools.GetObjectFromXml<QueryItemResultObject>(result.Body.Response.Return.ResultXml);

            var ret = new List<ISageItemQuery>();

            foreach(var lin in itemsFound.Lines)
            {
                ret.Add(new ItemQuery() { Result = lin });
            }

            return ret;
        }

        #endregion Article / Items

        #region Providers
        public async Task<ISageBpc> GetCustomerDetail(string customerRef)
        {
            var msg = new WsRequestRead()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodyRead()
                {
                    Operation = new WsRead()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Customer"],
                        ObjectKeys = new List<WsKey>() {
                            new WsKey() { Key = "BPCNUM", Value = customerRef }
                        }
                    }
                }
            };

            var result = await Request(msg, customerRef);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageObjectNotFoundException(String.Format("{0} - {1}:'{2}' - Sage Text: '{3}'", g["Company/Customer not found"], "BPCNUM", customerRef, string.Join('-', result.Body.Response.Return.Messages.Select(m => m.Message))));
            }

            var customer = new Bpc()
            {
                Result = XmlTools.GetObjectFromXml<ResultObject>(result.Body.Response.Return.ResultXml)
            };

            return customer;
        }

        #endregion Providers

        #region Orders
        // return soh object -> registered order
        public async Task<ISageOrder> RegisterOrder(ISageOrder order)
        {
            IXmlTransfer param = order.Param;

            var msg = new WsRequestSave()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodySave()
                {
                    Operation = new WsSave()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Order"],
                        //ObjectXml = new ObjectXml() { Text = XmlTools.ToXmlString(param) }
                        ObjectXml = XmlTools.ToXmlString(param)
                    }
                }
            };

            WsResponseSave result = await Request(msg, order.CustomerOrderReference);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageRegisterOrderException($"Error to Register Order [{order.CustomerOrderReference}] {Environment.NewLine} {result.Body.Response.Return.AllMessages()}");
            }

            var soh = new Soh()
            {
                Param = XmlTools.GetObjectFromXml<ResultObject>(result.Body.Response.Return.ResultXml)
            };

            return soh;
        }

        public async Task<ISageOrder> GetOrderDetailAsync(string reference)
        {
            var msg = new WsRequestRead()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodyRead()
                {
                    Operation = new WsRead()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Order"],
                        ObjectKeys = new List<WsKey>() {
                            new WsKey() { Key = "SOHNUM", Value = reference }
                        }
                    }
                }
            };

            WsResponseRead result = await Request(msg, reference);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageObjectNotFoundException($"Error to Get Order '{reference}'");
            }

            var soh = new Soh()
            {
                Param = XmlTools.GetObjectFromXml<ResultObject>(result.Body.Response.Return.ResultXml)
            };

            return soh;
        }

        /// <summary>
        /// Looking if inner the SAGE order reference exist a detail for PRINT orderID
        /// the field YWSREF is a custom field added to SAGE
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="orderID"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfOrderExistAsync(string reference, int orderID)
        {
            var orderExist = false;

            try
            {
                var soh = await GetOrderDetailAsync(reference);
                orderExist = soh.Reference == reference && soh.ExistItem("YWSREF", orderID.ToString());
            }catch
            {
                LogMessage($"Cannot found order reference [{0}]", null);
            }

            return orderExist;
        }

        public async Task<ISageOrder> UpdateOrderItemsAsync(ISageOrder order, string reference)
        {
            IXmlTransfer param = order.Param;

            var msg = new WsRequestUpdate()
            {
                Url = appConfig["WebLink.Sage.Url"],
                Body = new BodyUpdate()
                {
                    Operation = new WsUpdate()
                    {
                        CallContext = GetContext(),
                        PublicName = appConfig["WebLink.Sage.PublicName.Order"],
                        ObjectKeys = new List<WsKey>() {
                            new WsKey() { Key = "SOHNUM", Value = reference }
                        },
                        ObjectXml = XmlTools.ToXmlString(param)
                    }
                }
            };

            WsResponseUpdate result = await Request(msg, order.CustomerOrderReference);

            if (result.Body.Response.Return.Status != 0 || string.IsNullOrEmpty(result.Body.Response.Return.ResultXml))
            {
                throw new SageRegisterOrderException($"Error to Update Order [{order.CustomerOrderReference}] {Environment.NewLine} {result.Body.Response.Return.AllMessages()}");
            }

            var soh = new Soh()
            {
                Param = XmlTools.GetObjectFromXml<ResultObject>(result.Body.Response.Return.ResultXml)
            };

            return soh;
        }



        #endregion Orders

        public bool IsEnabled()
        {
            return !string.IsNullOrEmpty(appConfig["WebLink.Sage.IsActive"]) && appConfig.Bind<bool>("WebLink.Sage.IsActive");
        }

        
    }
}
