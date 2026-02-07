using Service.Contracts.Database;

namespace WebLink.Contracts.Models
{

    public enum ConflictMethod
    {
        Default = 1,
        Article = 2,
        SharedData = 3
    }

    public enum ComparerType
    {
        Row = 1,
        Column = 2
    }

    public interface IComparerConfiguration : IEntity
    {
        int? CompanyID { get; set; }
        int? BrandID { get; set; }
        int? ProjectID { get; set; }
        ConflictMethod Method { get; set; }
        ComparerType Type { get; set; }
        string ColumnName { get; set; }
        bool CategorizeArticle { get; set; }
    }
}