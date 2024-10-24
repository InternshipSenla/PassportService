using PassportService.Core;

namespace PassportService.Services
{
    public interface ICsvPassportLoaderService
    {      
        Task LoadPassportsFromCsvAsync();
    }
}
