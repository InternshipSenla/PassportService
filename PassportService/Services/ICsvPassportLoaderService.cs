using PassportService.Core;

namespace PassportService.Services
{
    public interface ICsvPassportLoaderService
    {
        public string UnpackingCSVFile();
        public Task LoadPassportsFromCsvAsync();
        public Task AddPassportsIfNotExistsAsync(IEnumerable<Passport> newPassports);
    }
}
