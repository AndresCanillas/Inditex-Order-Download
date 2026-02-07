using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts
{
	public interface IOrderDocumentService
	{
		/// <summary>
		/// Checks to see if the preview document for the specified order already exists, if it exists then it also returns the path to the document.
		/// </summary>
		bool PreviewDocumentExists(int orderid, out Guid documentFileGUID);

		/// <summary>
		/// Gets the path to the preview document (if available) if the preview is being generated in another process then it waits for up to 
		/// 30 seconds for it to complete before throwing a timeout error.
		/// </summary>
		/// <param name="orderid">The Order ID</param>
		/// <returns>The file guid of the document</returns>
		IFSFile GetPreviewDocument(int orderid, bool forceCreate=false);

		/// <summary>
		/// Creates the order preview document and attaches it to the order in the Documents attachment category.
		/// </summary>
		/// <param name="orderid">ID of the order</param>
		/// <returns>Returns the file GUID of the created document</returns>
		Task<Guid> CreatePreviewDocument(int orderid);

		/// <summary>
		/// Checks to see if the production sheet document for the specified order already exists, if it exists then it also returns the file GUID of the document.
		/// </summary>
		bool ProdSheetDocumentExists(int orderid, out Guid documentFileGUID);

		/// <summary>
		/// Gets the path to the production sheet document (if available) if the preview is being generated in another process then it waits for up to 
		/// 30 seconds for it to complete before throwing a timeout error.
		/// </summary>
		/// <param name="orderid">The Order ID</param>
		/// <returns>The the file GUID of the document</returns>
		IFSFile GetProdSheetDocument(int orderid);

		/// <summary>
		/// Creates the order production sheet document and attaches it to the order in the Documents attachment category.
		/// </summary>
		/// <param name="orderid">ID of the order</param>
		/// <returns>Returns the file GUID of the created document</returns>
		Task<Guid> CreateProdSheetDocument(int orderid);


        /// <summary>
        /// Creates the order detail document and attaches it to the order in the Documents attachment category.
        /// </summary>
        /// <param name="orderid">ID of the order</param>
		/// <returns>Returns the file GUID of the created document</returns>
        Task<Guid> CreateOrderDetailDocument(int orderid);

		/// <summary>
		/// Invalidates any cached images from the specified order
		/// </summary>
		/// <param name="orderid">ID of the order</param>
		Task InvalidateCache(int orderid);

        /// <summary>
        /// Creates the order detail document and attaches it to the order in the Documents attachment category.
        /// </summary>
        /// <param name="orderid">The Order ID</param>
        /// <returns>Returns the file GUID of the created document</returns>
        IFSFile GetOrderDetailDocument(int orderid);
	}
}
