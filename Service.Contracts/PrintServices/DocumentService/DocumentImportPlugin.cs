using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Service.Contracts.Documents;

namespace Service.Contracts
{
    public interface IDocumentImportPlugin : IDisposable
    {
        void PrepareFile(DocumentImportConfiguration configuration, ImportedData data);
        void Execute(DocumentImportConfiguration configuration, ImportedData data);
    }

    public class BaseDocumentImportPlugin : IDocumentImportPlugin
    {
        protected virtual void OnPrepareFile(DocumentImportConfiguration configuration, ImportedData data) { }
        protected virtual void OnExecute(DocumentImportConfiguration configuration, ImportedData data) { }

        public void PrepareFile(DocumentImportConfiguration configuration, ImportedData data)
        {
            OnPrepareFile(configuration, data);
        }

        public void Execute(DocumentImportConfiguration configuration, ImportedData data)
        {
            OnExecute(configuration, data);
        }

        public virtual void Dispose()
        {
        }
    }
}
