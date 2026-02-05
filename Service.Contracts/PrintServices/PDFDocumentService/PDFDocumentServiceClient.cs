using System.Threading.Tasks;

namespace Service.Contracts.PDFDocumentService
{
	public class PDFDocumentServiceClient: BaseServiceClient, IPDFDocumentService
	{
		public async Task<PDFDocumentResult> CreateOrderPreviewAsync(PDFOrderPreview documentData)
		{
			return await base.InvokeAsync<PDFOrderPreview, PDFDocumentResult>("api/CreateOrderPreview", documentData);
		}

		public async Task<PDFDocumentResult> CreateProductionSheetAsync(PDFProdSheet documentData)
		{
			return await base.InvokeAsync<PDFProdSheet, PDFDocumentResult>("api/CreateProductionSheet", documentData);
		}

        public async Task<PDFDocumentResult> CreateOrderDetailAsync(PDFOrderDetail documentData)
        {
            return await base.InvokeAsync<PDFOrderDetail, PDFDocumentResult>("api/CreateOrderDetail", documentData);
        }
    }
}
