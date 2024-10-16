using PassportService.Core;

namespace PassportService.Service
{
    public interface IPassportRepository
    {
        IEnumerable<Passport> GetAllPassports();
        void AddPassports(IEnumerable<Passport> passports);
        void RemovePassports(IEnumerable<Passport> passports);
        void SaveChanges();
    }
}
