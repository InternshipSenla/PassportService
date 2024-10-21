using PassportService.Core;

namespace PassportService.Service
{
    public interface IPassportRepository
    {
        Task<Passport> GetPassportAsync(Passport passport);
        Task AddPasssporsAsync(List<Passport> passports);
        Task SaveChangeDbAsync();
        Task<List<Passport>> SerchDeletePassports();
        public void UpdatePassport(Passport passport);
        Task<List<Passport>> GetAllPassports();
        Task<List<Passport>> GetPassportsBySeries(string Series);
        Task<List<Passport>> GetPassportsByNumber(string Number);
        Task<List<Passport>> GetPassportsBySeriesAndNumber(string SeriesAndNumber);
        Task<List<Passport>> GetPassportsByDate(DateTime date);
    }
}
