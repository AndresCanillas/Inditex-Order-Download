//using PDFLib;
using Service.Contracts.LabelService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Service.Contracts.PrintServices.PDFService
{
    public interface IPDFServiceClient : IPDFService { }

    public class PDFServiceClient : IPDFServiceClient
    {
        private IFactory factory;
        private IAppConfig config;
        private ConcurrentQueue<IPEndPoint> endpoints;
        //private ConcurrentDictionary<string, PDFDocumentModel> currentJobs;

        public PDFServiceClient(IFactory factory, IAppConfig config)
        {
            this.factory = factory;
            this.config = config;
            this.endpoints = new ConcurrentQueue<IPEndPoint>();
            var configuredEndPoints = config.Bind<List<string>>("PDFService.EndPoints");
            foreach (var uri in configuredEndPoints)
            {
                var uriObject = new Uri(uri);
                this.endpoints.Enqueue(TcpHelper.GetEndPoint(uriObject.Host, uriObject.Port));
            }
            //this.msgPeer = factory.GetInstance<IMsgPeer>();
        }

        //public bool IsConnected { get; set; }

        public void Connect()
        {

        }

        public bool IsConnected()
        {
            throw new NotImplementedException();
        }

        //public async Task<PDFServiceResponse> CreatePDFDocument(string key, PDFDocumentModel document)
        //{
        //    IPEndPoint ep;
        //    while (!endpoints.TryDequeue(out ep))
        //    {
        //        await Task.Delay(1000);
        //    }
        //    try
        //    {
        //        using (var peer = factory.GetInstance<IMsgPeer>())
        //        {
        //            peer.Connect(ep);
        //            var srv = peer.GetServiceProxy<IPDFService>();

        //            return await srv.CreatePDFDocument(key, document);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Task.Delay(10000);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        endpoints.Enqueue(ep);
        //    }
        //}

        public PDFDocumentWrapper GetStatus(string Key)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return srv.GetStatus(Key);
                }
            }
            catch (Exception ex)
            {
                Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }

        }

        public MergePdfWrapper GetMergeStatus(int Key)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return srv.GetMergeStatus(Key);
                }
            }
            catch (Exception ex)
            {
                Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }

        }

        private async Task ExecuteAction(string action, params object[] p)
        {

            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    switch (action)
                    {
                        case "1":
                            break;
                    }


                    //return await srv.CreatePDFDocument(key, document);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        //public void JoinFilesCompleted(string name, int jobFileID, List<string> pathFiles)
        //{
        //    IPEndPoint ep;
        //    while (!endpoints.TryDequeue(out ep))
        //    {
        //        Task.Delay(1000);
        //    }
        //    try
        //    {
        //        using (var peer = factory.GetInstance<IMsgPeer>())
        //        {
        //            peer.Connect(ep);
        //            var srv = peer.GetServiceProxy<IPDFService>();

        //            srv.JoinFilesCompleted(name, jobFileID, pathFiles);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Task.Delay(10000);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        endpoints.Enqueue(ep);
        //    }
        //}

        public void ReRun(string Key)
        {
            throw new NotImplementedException();
        }

        public void Delete(List<string> Key)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    srv.Delete(Key);
                }
            }
            catch (Exception ex)
            {
                Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        public bool CanContinue(string Key)
        {
            throw new NotImplementedException();
        }

        //public async Task<PDFServiceResponse> CreatePDFDocumentSplited(ConcurrentDictionary<string, PDFDocumentModel> keyValues)
        //{
        //    IPEndPoint ep;
        //    while (!endpoints.TryDequeue(out ep))
        //    {
        //        await Task.Delay(1000);
        //    }
        //    try
        //    {
        //        using (var peer = factory.GetInstance<IMsgPeer>())
        //        {
        //            peer.Connect(ep);
        //            var srv = peer.GetServiceProxy<IPDFService>();

        //            return await srv.CreatePDFDocumentSplited(keyValues);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await Task.Delay(10000);
        //        throw ex;
        //    }
        //    finally
        //    {
        //        endpoints.Enqueue(ep);
        //    }
        //}

        public async Task<PDFServiceResponse> CreatePDFDocument(string key, PrintToFileRequest wrapper)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return await srv.CreatePDFDocument(key, wrapper);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        public async Task<PDFServiceResponse> CreatePDFDocument(string Key, PDFDocumentWrapper wrapper)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return await srv.CreatePDFDocument(Key, wrapper);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        public async Task<PDFServiceResponse> CreatePDFDocumentSplited(string JobFileId, string JobFileSplitId, PDFDocumentWrapper keyValues)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return await srv.CreatePDFDocumentSplited(JobFileId, JobFileSplitId, keyValues);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        public async Task<bool> MergeFilesForJobAsync(int jobFileID, string targetFile, List<int> filesToMerge, PDFDocumentWrapper wrapper)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return await srv.MergeFilesForJobAsync(jobFileID, targetFile, filesToMerge, wrapper);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }
        public async Task<bool> MergePdfForJobAsync(MergePdfWrapper wrapper)
        {
            IPEndPoint ep;
            while (!endpoints.TryDequeue(out ep))
            {
                await Task.Delay(1000);
            }
            try
            {
                using (var peer = factory.GetInstance<IMsgPeer>())
                {
                    peer.Connect(ep);
                    var srv = peer.GetServiceProxy<IPDFService>();

                    return await srv.MergePdfForJobAsync(wrapper);
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(10000);
                throw ex;
            }
            finally
            {
                endpoints.Enqueue(ep);
            }
        }

        public Task<bool> AddMergeReadyAsync(int key)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddMergePdfReadyAsync(int key)
        {
            throw new NotImplementedException();
        }
    }
}
