using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PassportService.Core;
using PassportService.Services;
using Microsoft.Extensions.Logging;
using PassportService.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace PassportService.Tests
{
    [TestFixture]
    public class CsvPassportLoaderServiceTests
    {
        private CsvPassportLoaderService _csvPassportLoaderService;    
        private Mock<IConfiguration> _configurationMock;
        private Mock<IPassportRepository> _passportRepositoryMock;
        private Mock<ILogger<PassportRepository>> _loggerMock;
         
        [SetUp]
        public void Setup()
        {
            _configurationMock = new Mock<IConfiguration>();
            _passportRepositoryMock = new Mock<IPassportRepository>();           
            _loggerMock = new Mock<ILogger<PassportRepository>>();
            _csvPassportLoaderService 
                = new CsvPassportLoaderService(_passportRepositoryMock.Object, _configurationMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task AddPassports_ShouldAddNewPassports_WhenAllAreNew()
        {     
            var newPassports = new List<Passport>
            {
                new Passport { Series = 1234, Number = 567890 },
                new Passport { Series = 1234, Number = 098765 }
            };

            await _csvPassportLoaderService.AddPassports(newPassports);

            _passportRepositoryMock.Verify(repo => repo.AddPassportsAsync(It.Is<List<Passport>>(x =>
                   x.Count == 2 &&
                   x.Any(p => p.Series == 1234 && p.Number == 567890) &&
                   x.Any(p => p.Series == 1234 && p.Number == 098765))), Times.Once);           
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
                    Series = 1234,
                    Number = 567890,
                    CreatedAt = new List<DateTime> { today.AddYears(-1) }, // Создан год назад
                    RemovedAt = new List<DateTime?> { today.AddMonths(-6) }, // Удален полгода назад
                    DateLastRequest = today.AddYears(-1)
                }
            };

            var newPassports = new List<Passport>
            {
                new Passport { Series = 1234, Number = 567890 } // Уже существующий паспорт               
            };

            _passportRepositoryMock
               .Setup(repo => repo.GetPassportsThatAreInDbAndInCollection(newPassports))
               .ReturnsAsync(oldPassports);

            await _csvPassportLoaderService.AddPassports(newPassports);

            _passportRepositoryMock.Verify(service =>
                service.GetPassportsThatAreInDbAndInCollection(newPassports), Times.Once);

            _passportRepositoryMock.Verify(repo => repo.UpdatePassports(It.Is<List<Passport>>(x =>
                x.Count == 1
                && x.Any(p => p.Series == 1234 && p.Number == 567890
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
                    Series = 1234,
                    Number = 567890,
                    CreatedAt = new List<DateTime> { today.AddYears(-1) }, // Создан год назад
                    RemovedAt = new List<DateTime?> { today.AddMonths(-6) }, // Удален полгода назад
                    DateLastRequest = today.AddYears(-1)
                }
            };

            var newPassports = new List<Passport>
            {
                new Passport { Series = 1234, Number = 098765 }, // Новый паспорт
                new Passport { Series = 1234, Number = 567890 }  // Уже существующий паспорт
            };

            _passportRepositoryMock
               .Setup(repo => repo.GetPassportsThatAreInDbAndInCollection(newPassports))
               .ReturnsAsync(oldPassports);

            // Действие: добавление паспортов
            await _csvPassportLoaderService.AddPassports(newPassports);

            // Assert: Проверяем что метод был вызван один раз
            _passportRepositoryMock.Verify(service =>
                service.GetPassportsThatAreInDbAndInCollection(newPassports), Times.Once);

            // Assert: Проверяем обновление существующего паспорта (добавление новой даты)
            _passportRepositoryMock.Verify(repo => repo.UpdatePassports(It.Is<List<Passport>>(x =>
                x.Count == 1 &&
                x.Any(p => p.Series == 1234 && p.Number == 567890 &&
                    p.CreatedAt.Any(date => date.Date == today.Date) &&
                    p.DateLastRequest.Date == today.Date))), Times.Once);

            // Assert: Новый паспорт должен быть добавлен в базу
            _passportRepositoryMock.Verify(repo => repo.AddPassportsAsync(It.Is<List<Passport>>(x =>
                x.Count == 1 &&
                x.Any(p => p.Series == 1234 && p.Number == 098765))), Times.Once);
        }
    }
}
