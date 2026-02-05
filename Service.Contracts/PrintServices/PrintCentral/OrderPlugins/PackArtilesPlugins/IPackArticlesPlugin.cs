using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintCentral
{
	public interface IPackArticlesPlugin : IDisposable
	{
        void GetPackArticles(ImportedData Data, Dictionary<string, int> articleCodes);

    }

    public abstract class AbstractPackArticlesPlugin : IPackArticlesPlugin
    {
        public virtual void GetPackArticles(ImportedData data, Dictionary<string, int> articleCodes)
        {            
        }

        public virtual void AddArticles(List<string> articles, Dictionary<string, int> articleCodes, int quantity)
        {
            for (var i = 0; i < articles.Count; i++)
            {
                var currentArticle = articles[i];

                if (string.IsNullOrEmpty(currentArticle)) continue;

                if (!articleCodes.ContainsKey(currentArticle))
                    articleCodes.Add(currentArticle, quantity);
                else
                    articleCodes[currentArticle] += quantity;
            }
        }

        public abstract void Dispose();
	}
}
