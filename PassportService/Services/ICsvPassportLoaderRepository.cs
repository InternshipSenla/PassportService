using PassportService.Core;

namespace PassportService.Service
{
    public interface ICsvPassportLoaderRepository
    {
        public void UnpackingCSVFile();
        public string GetPathToUnpackCSVFile();
        public Task LoadPassportsFromCsvAsync();
        public Task AddPassportsIfNotExistsAsync(IEnumerable<Passport> newPassports);    
    }
}
