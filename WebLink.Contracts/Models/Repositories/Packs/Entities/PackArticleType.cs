using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{ 
    public enum PackArticleType
    {
        NotDefined,
        ByArticle,
        ByOrderData,
        ByPlugin
    }

    public enum PackArticleCondition
    {
        Equal,
        Contain,
        StartWith,
        EndWith,
        Regex
    }
}
