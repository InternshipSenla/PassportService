using PassportService.Core;

namespace PassportService.Service
{
    public interface ICsvPassportLoaderRepository
    {
        public string PathToUnpackCSVFile();
        public Task LoadPassportsFromCsvAsync();
        public Task AddPassportsIfNotExistsAsync(IEnumerable<Passport> newPassports);
        public Task UpdateDeletedPassportAsync();
    }
}
