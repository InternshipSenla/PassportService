using PassportService.Core;

namespace PassportService.Service
{
    public interface IPassportRepository
    {
        Task<List<Passport>> GetAllPassports();
        Task<List<Passport>> GetPassportsBySeries(string Series);
        Task<List<Passport>> GetPassportsByNumber(string Number);
        Task<List<Passport>> GetPassportsBySeriesAndNumber(string SeriesAndNumber);
        Task LoadPassportsFromCsvAsync();
    }
}
