using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderEmailService
	{
		/// <summary>
		/// Retrieves the token associated to the specified user
		/// </summary>
		/// <param name="userid">ID of the user</param>
		/// <param name="type">The type of email or "communication" that is being registered.</param>
		/// <returns>The token for the given user</returns>
		/// <remarks>
		/// If a token for the specified user/type combination does not exist, then this call will create one.
		/// This method is meant to be used when adding orders to the list of items (AddOrder, AddOrderIfNotExists)
		/// </remarks>
		IEmailToken GetTokenFromUser(string userid, EmailType type);

		/// <summary>
		/// Retrieves the token associated to the specified token code
		/// </summary>
		/// <param name="code">Token code, this is usually included as part of the URL when a user clicks on a ling to a landing page.</param>
		/// <returns>The token for the given code, or null if the specified code is not valid.</returns>
		/// <remarks>
		/// This call WILL NOT create the token if it does not exist, instead null will be returned.
		/// This method is meant to be used when a user access any of the landing pages.
		/// </remarks>
		IEmailToken GetTokenFromCode(string code);

		/// <summary>
		/// Adds the specified order to the email token, or resets the state of the item if the order is already being referenced by the token.
		/// </summary>
		/// <param name="token">The token on which the order will be referenced</param>
		/// <param name="orderid">The order to add to the token</param>
		/// <remarks>
		/// If the order is already being referenced by the token, then this call will reset the Notified and Seen flags of the corresponding item, which will in turn
		/// cause the order to appear again in the landing page and possible in an email sent to the user. This call is intended to be used when registering orders
		/// due to events such as OrderReceived and OrderValidated.
		/// </remarks>
		void AddOrder(IEmailToken token, int orderid);

		/// <summary>
		/// Adds the specified order to the email token but only if the order is not already being referenced by the token.
		/// </summary>
		/// <param name="token">The token on which the order will be referenced</param>
		/// <param name="orderid">The order to add to the token</param>
		/// <remarks>
		/// If the order is already being referenced by the token (regardless of the state of the item), then this call will have no effect.
		/// This method is meant to be used when dealing with automated processes that might report on orders many times. One example is the process
		/// that reminds users of orders that are pending validation, the objective here, is to avoid notifying on the same orders over and over.
		/// </remarks>
		void AddOrderIfNotExists(IEmailToken token, int orderid);

		/// <summary>
		/// Marks the related items as seen (to prevent they from appearing again in the report)
		/// </summary>
		/// <param name="token">The token on which the order will be referenced</param>
		/// <param name="orderIDs">IDs of the orders associated to the items that have been seen</param>
		void MarkAsSeen(IEmailToken token, int[] orderIDs);

		/// <summary>
		/// Returns a list of all the orders referenced by the token (only items that have not been marked as seen are returned).
		/// </summary>
		/// <param name="token">The token to be used in the query</param>
		/// <returns>Returns the list of orders referenced by the token.</returns>
		List<OrderEmailDetail> GetTokenOrders(IEmailToken token, bool includeNotified, bool includeSeen);

		/// <summary>
		/// Returns a list of all tokens (user/type of email combinations) that have items with the Notified flag set to false.
		/// </summary>
		IEnumerable<IEmailToken> GetTokensWithPendingNotifications();

		/// <summary>
		/// Gets the email settings for the specified user.
		/// </summary>
		/// <param name="token">The token used to access the landing page</param>
		IEmailServiceSettings GetEmailServiceSettings(IEmailToken token);

		/// <summary>
		/// Updates email settings for the specified user.
		/// </summary>
		/// <param name="token">The token used to access the landing page</param>
		/// <param name="settings">The settings to be saved to the database.</param>
		void UpdateEmailServiceSettings(IEmailToken token, IEmailServiceSettings settings);

		/// <summary>
		/// This method will send any due email notifications to their intended recipients, taking into account the user settings for email notifications.
		/// This method is meant to be called from an automated task that executes every hour.
		/// </summary>
		Task SendNotifications();

		/// <summary>
		/// Retrieves the preview of the specified order, so long the order is part of the given token
		/// </summary>
		/// <param name="token">The token used to access the landing page</param>
		/// <param name="orderid">The id of the order</param>
		IAttachmentData GetOrderPreview(IEmailToken token, int orderid);

		/// <summary>
		/// Adds the specified order to the email token, or resets the state of the item if the order is already being referenced by the token.
		/// </summary>
		/// <param name="token">The token on which the error will be referenced</param>
		/// <param name="errorType">the errorType add to the toke</param>
		/// <param name="message"></param>
		void AddErrorIfNotExist(IEmailToken token, ErrorNotificationType errorType, string title, string message, string key, int? projectId, int? locationId, int? orderId);

		/// <summary>
		/// Marks the related items as seen (to prevent they from appearing again in the report)
		/// </summary>
		/// <param name="token">The token on which the order will be referenced</param>
		void MarkErrorAsSeen(IEmailToken token, int[] seenIDs);

		/// <summary>
		/// Returns a list of all the Error referenced by the token
		/// </summary>
		/// <param name="token">The token to be used in the query</param>
		/// <returns>Returns the list of Error referenced by the token.</returns>
		IEnumerable<ErrorEmailDetail> GetTokenErrors(IEmailToken token, bool includeNotified, bool includeSeen);

		/// <summary>
		/// Sends an email message using send grid service.
		/// </summary>
		/// <param name="email">The recipient for the email</param>
		/// <param name="subject">Subject of the email</param>
		/// <param name="body">Content of the email</param>
		/// <param name="files">Any attached files (can be null if no files need to be attached)</param>
		Task SendMessage(string email, string subject, string body, List<string> files);


        IEnumerable<string> GetSysAdminUsers();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="locationID"></param>
        /// <returns>return a list of customer service and production manager user ids for IDT</returns>
        IEnumerable<string> GetIDTStakeHoldersUsers(int projectID, int? locationID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Returns a list of user ids of customers service users asigned to the order</returns>
        IEnumerable<string> GetCustomersUsersByOrder(int orderID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Returns a list of user ids of customers service users asigned to the project</returns>
        IEnumerable<string> GetCustomersUsersByProject(int projectID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Returns a list of user ids of the Product Managers users asigned</returns>
        IEnumerable<string> GetProductionManagersUsersByLocation(int locationID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Return a list of user ids of the Provider asigned to the order </returns>
        IEnumerable<string> GetProvidersUsersByOrder(int orderID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns>Return list of user ids of the Client for requested order </returns>
        IEnumerable<string> GetClientUsersByProject(int projectID);
        List<OrderEmailDetail> GetPoolOrders(IEmailToken token, bool includeNotified, bool includeSeen);
    }

	public enum ErrorNotificationType
	{
		SystemError,
		CompanyNotFound,
		ArticleNotFound,
		MappingNotFound,
		SageError,
		LookUpKeyNotFound, 
        UserNotFound
	}



	public class OrderEmailDetail
	{
		public int OrderID;
		public string OrderNumber;
		public string CBP;
		public string Article;
		public int Quantity;
		public DateTime OrderDate;
		public DateTime? ValidationDate;
		public OrderStatus Status;
		public string ClientReference;
		public string LocationName;
		public string ERPReference;
    }

	public class ErrorEmailDetail
	{
		public int ItemID { get; set; }
		public string Message { get; set; }
		public string Location { get; set; }
		public string Company { get; set; }
		public ErrorNotificationType Type { get; set; }
		public string TypeDescription => Type.GetText();
	}

    public enum ArticleCompostionCalculationType
    {
        Default, 
        NotIncludeAdditionalWithCompo, 
        CanIncludeAdditionalWithCompo    
    }
}
