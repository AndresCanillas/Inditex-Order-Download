namespace WebLink.Contracts.Models
{
    public interface ICountryRepository : IGenericRepository<ICountry>
    {
        ICountry GetByAlpha2(string alpha2);
        ICountry GetByAlpha2(PrintDB ctx, string alpha2);
    }
}
