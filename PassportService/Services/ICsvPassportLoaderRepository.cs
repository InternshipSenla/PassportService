using PassportService.Core;

namespace PassportService.Service
{
    public interface ICsvPassportLoaderRepository
    {
        public string UnpackingCSVFile();
        public Task LoadPassportsFromCsvAsync();
        public Task AddPassportsIfNotExistsAsync(IEnumerable<Passport> newPassports);    
    }
}
