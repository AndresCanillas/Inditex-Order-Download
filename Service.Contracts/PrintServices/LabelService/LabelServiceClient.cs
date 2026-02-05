using System.Threading.Tasks;

namespace Service.Contracts.LabelService
{
    class LabelServiceClient : BaseServiceClient, ILabelServiceClient
    {
        public async Task<PrinterDriversResponse> GetPrinterDriversAsync()
        {
            return await InvokeAsync<PrinterDriversResponse>("api/drivers");
        }

        public async Task<LabelInfoResponse> GetLabelInfoAsync(LabelInfoRequest config)
        {
            return await InvokeAsync<LabelInfoRequest, LabelInfoResponse>("api/labelinfo", config);
        }

        public async Task<LabelServiceResponse> GetArticlePreviewAsync(ArticlePreviewRequest config)
        {
            return await InvokeAsync<ArticlePreviewRequest, LabelServiceResponse>("api/articlepreview", config);
        }

        public async Task<LabelServiceResponse> GetArticlePreview2Async(ArticlePreviewRequest2 config)
        {
            return await InvokeAsync<ArticlePreviewRequest2, LabelServiceResponse>("api/ArticlePreviewWithVariables", config);
        }

        public async Task<LabelServiceResponse> PrintArticleAsync(PrintToFileRequest config)
        {
            return await InvokeAsync<PrintToFileRequest, LabelServiceResponse>("api/printtofile", config);
        }

        public async Task<LabelServiceResponse> PrintArticleByQuantityAsync(PrintToFileRequest config)
        {
            return await InvokeAsync<PrintToFileRequest, LabelServiceResponse>("api/printtofilebyquantity", config);
        }

        public async Task<LabelServiceContentResponse> PrintArticleLocallyAsync(PrintToFileRequest config)
        {
            return await InvokeAsync<PrintToFileRequest, LabelServiceContentResponse>("api/printtofilelocally", config);
        }
        public async Task<LabelServiceContentResponse> PrintArticleLocallyByQuantityAsync(PrintToFileRequest config)
        {
            return await InvokeAsync<PrintToFileRequest, LabelServiceContentResponse>("api/printtofilelocallybyquantity", config);
        }

        public async Task<LabelServiceContentResponse> PrintHeaderAsync(PrintHeaderRequest config)
        {
            return await InvokeAsync<PrintHeaderRequest, LabelServiceContentResponse>("api/header/print", config);
        }

        public async Task<ComparePreviewResponse> ComparePreviewsAsync(ComparePreviewsRequest rq)
        {
            return await InvokeAsync<ComparePreviewsRequest, ComparePreviewResponse>("api/comparepreviews", rq);
        }

        public async Task<LabelServiceResponse> InvalidateCache(LabelCacheInvalidationRequest rq)
        {
            return await InvokeAsync<LabelCacheInvalidationRequest, LabelServiceResponse>("api/invalidatecache", rq);
        }

        public async Task<LabelServiceResponse> PrintPDFLibAsync(PrintPDFRequest rq)
        {
            return await InvokeAsync<PrintPDFRequest, LabelServiceResponse>("/labels/pdflabelfilecontent", rq);
        }

        public async Task<LabelServiceContentResponse> PrintPDFLibLocallyAsync(PrintPDFRequest rq)
        {
            return await InvokeAsync<PrintPDFRequest, LabelServiceContentResponse>("/labels/pdflabelfile", rq);
        }
    }
}
