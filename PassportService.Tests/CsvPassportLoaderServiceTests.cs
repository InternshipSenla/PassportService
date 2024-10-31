using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PassportService.Configuration;
using PassportService.Core;
using PassportService.Services;

namespace PassportService.Tests
{
    [TestFixture]
    public class CsvPassportLoaderServiceTests
    {
        private CsvPassportLoaderService _csvPassportLoaderService;
        private Mock<IConfiguration> _configurationMock;
        private Mock<IPassportRepository> _passportRepositoryMock;
        private Mock<ILogger<PassportRepository>> _loggerMock;
        private Mock<IOptions<CsvFileSettings>> _csvFileSettingsMock;

        [SetUp]
        public void Setup()
        {
            _passportRepositoryMock = new Mock<IPassportRepository>();
            _loggerMock = new Mock<ILogger<PassportRepository>>();
            _csvFileSettingsMock = new Mock<IOptions<CsvFileSettings>>();
            _csvPassportLoaderService
                = new CsvPassportLoaderService(_csvFileSettingsMock.Object, _passportRepositoryMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task AddPassports_ShouldAddNewPassports_WhenAllAreNew()
        {
            // Arrange
            var newPassports = new List<Passport>
            {
                new Passport { Series = "1234", Number = "567890" },
                new Passport { Series = "1234", Number = "098765" },
                new Passport { Series = "5678", Number = "123456" },
                new Passport { Series = "5678", Number = "654321" }
            };

            // Настройка мока
            _passportRepositoryMock
               .Setup(repo => repo.AddPassportsAsync(It.IsAny<List<Passport>>()))
               .ReturnsAsync(true);

            // Act
            await _csvPassportLoaderService.AddPassports(newPassports);

            // Assert
            _passportRepositoryMock.Verify(repo => repo.AddPassportsAsync(It.IsAny<List<Passport>>()), Times.Exactly(2));
        }

        [Test]
        public async Task AddPassports_ShouldAddOldPassports_WhenAllAreOld()
        {
            // Arrange
            var today = DateTime.UtcNow;

            var oldPassports = new List<Passport>
            {
                new Passport
                {
                    Series = "1234",
                    Number = "567890",
                    CreatedAt = new List<DateTime> { today.AddYears(-1) }, // Создан год назад
                    RemovedAt = new List<DateTime?> { today.AddMonths(-6) }, // Удален полгода назад
                    DateLastRequest = today.AddYears(-1)
                }
            };

            var newPassports = new List<Passport>
            {
                new Passport { Series = "1234", Number = "567890" } // Уже существующий паспорт               
            };

            _passportRepositoryMock
               .Setup(repo => repo.GetPassportsThatAreInDbAndInCollection(newPassports))
               .ReturnsAsync(oldPassports);

            await _csvPassportLoaderService.AddPassports(newPassports);

            _passportRepositoryMock.Verify(service =>
                service.GetPassportsThatAreInDbAndInCollection(newPassports), Times.Once);

            _passportRepositoryMock.Verify(repo => repo.UpdatePassports(It.Is<List<Passport>>(x =>
                x.Count == 1
                && x.Any(p => p.Series == "1234" && p.Number == "567890"
                && p.CreatedAt.Any(date => date.Date == today.Date)
                && p.DateLastRequest.Date == today.Date)
                 )), Times.Once);
        }


        [Test]
        public async Task AddPassports_ShouldAddNewAndOldPassports_WhenSomeAreOldAndSomeAreNew()
        {
            // Arrange
            var today = DateTime.UtcNow;

            var oldPassports = new List<Passport>
            {
                new Passport
                {
                    Series = "1234",
                    Number = "567890",
                    CreatedAt = new List<DateTime> { today.AddYears(-1) },
                    RemovedAt = new List<DateTime?> { today.AddMonths(-6) },
                    DateLastRequest = today.AddYears(-1)
                }
            };

            var newPassports = new List<Passport>
            {
                new Passport { Series = "1234", Number = "098765" },
                new Passport { Series = "1234", Number = "567890" }
            };

            _passportRepositoryMock
                .Setup(repo => repo.AddPassportsAsync(It.IsAny<List<Passport>>()))
                .ReturnsAsync(false);

            _passportRepositoryMock
                .Setup(repo => repo.GetPassportsThatAreInDbAndInCollection(It.IsAny<IEnumerable<Passport>>()))
                .ReturnsAsync(oldPassports);

            // Act
            await _csvPassportLoaderService.AddPassports(newPassports);

            // Assert: проверяем, что метод GetPassports был вызван дважды
            _passportRepositoryMock.Verify(service =>
                service.GetPassportsThatAreInDbAndInCollection(It.IsAny<IEnumerable<Passport>>()), Times.Exactly(2));

            // Assert: проверяем обновление паспорта "567890"
            _passportRepositoryMock.Verify(repo => repo.UpdatePassports(It.Is<List<Passport>>(x =>
                x.Count == 1 &&
                x.Any(p => p.Series == "1234" && p.Number == "567890" &&
                    p.CreatedAt != null && p.CreatedAt.Any(date => date.Date == today.Date) &&
                    p.DateLastRequest.Date == today.Date))), Times.Exactly(2));

            // Assert: проверяем, что новый паспорт был добавлен
            _passportRepositoryMock.Verify(repo => repo.AddPassportsAsync(It.IsAny<List<Passport>>()), Times.Exactly(3));
        }
    }
}
