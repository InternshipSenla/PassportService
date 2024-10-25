using PassportService.Core;

namespace PassportService.Services
{
    public interface IPassportRepository
    {
        Task AddPassportsAsync(List<Passport> passports);   
        Task UpdatePassports(List<Passport> passport);
        Task<List<Passport>> GetAllPassports();
        Task<List<Passport>> GetPassportsBySeries(int Series);
        Task<List<Passport>> GetPassportsByNumber(int Number);
        Task<List<Passport>> GetInactivePassportsBySeries(int Series);
        Task<List<Passport>> GetInactivePassportsByNumber(int Number);
        Task<List<Passport>> GetPassportsByDate(DateTime date);
        Task UpdateDeletedPassportAsync();
        Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports);
    }
}
