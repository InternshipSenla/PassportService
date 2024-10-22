using PassportService.Core;

namespace PassportService.Services
{
    public interface ICsvPassportLoaderService
    {
        string UnpackingCSVFile();
        Task LoadPassportsFromCsvAsync();
        Task AddPassports(IEnumerable<Passport> newPassports);
        Task AddNewPassportsInDb(List<Passport> newPassports);
        Task AddPassportsThatAreInDb(List<Passport> oldPassports);
    }
}
