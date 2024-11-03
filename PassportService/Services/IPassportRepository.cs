using Microsoft.AspNetCore.Mvc;
using PassportService.Core;

namespace PassportService.Services
{
    public interface IPassportRepository
    {
        Task<bool> AddPassportsAsync(List<Passport> passports);   
        Task UpdatePassports(List<Passport> passport);
        Task<List<Passport>> GetPassportsBySeriesAndNumber(string series, string number);
        Task<List<Passport>> GetInactivePassportsBySeriesAndNumber(string series, string number);
        Task<List<Passport>> GetPassportsByDate(DateTime date);
        Task UpdateDeletedPassportTasks();
        Task<List<Passport>?> GetPassportsThatAreInDbAndInCollection(IEnumerable<Passport> passports);
    }
}
