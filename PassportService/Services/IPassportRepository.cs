using PassportService.Core;

namespace PassportService.Services
{
    public interface IPassportRepository
    {
        Task<bool> AddPassportsAsync(List<Passport> passports);   
        Task UpdatePassports(List<Passport> passport);
        Task<List<Passport>> GetAllPassports();
        Task<List<Passport>> GetPassportsBySeries(string Series);
        Task<List<Passport>> GetPassportsByNumber(string Number);
        Task<List<Passport>> GetInactivePassportsBySeries(string Series);
        Task<List<Passport>> GetInactivePassportsByNumber(string Number);
        Task<List<Passport>> GetPassportsByDate(DateTime date);
        Task UpdateDeletedPassportTasks();
        Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports);
    }
}
