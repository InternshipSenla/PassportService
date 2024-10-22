using PassportService.Core;

namespace PassportService.Services
{
    public interface IPassportRepository
    {
        Task<Passport> GetPassportAsync(Passport passport);
        Task AddPassportsAsync(List<Passport> passports);          
        Task UpdatePassport(Passport passport);
        Task UpdatePassports(List<Passport> passport);
        Task<List<Passport>> GetAllPassports();
        Task<List<Passport>> GetPassportsBySeries(string Series);
        Task<List<Passport>> GetPassportsByNumber(string Number);
        Task<List<Passport>> GetPassportsBySeriesAndNumber(string SeriesAndNumber);
        Task<List<Passport>> GetInactivePassportsBySeries(string Series);
        Task<List<Passport>> GetInactivePassportsByNumber(string Number);
        Task<List<Passport>> GetInactivePassportsBySeriesAndNumber(string SeriesAndNumber);
        Task<List<Passport>> GetPassportsByDate(DateTime date);
        Task<List<Passport>> SerchDeletePassports();
        Task UpdateDeletedPassportAsync();
        Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports);
    }
}
