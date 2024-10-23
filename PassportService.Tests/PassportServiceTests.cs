using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PassportService.Core;
using PassportService.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using PassportService.Infrastructure;

namespace PassportService.Tests
{
    [TestFixture]
    public class PassportServiceTests
    {
        private Mock<IConfiguration> _configurationMock;
        private Mock<IPassportRepository> _passportRepositoryMock;
        private Mock<ILogger<PassportRepository>> _loggerMock;
        private PassportDbContext _context; // Добавляем поле для контекста

        [TearDown]
        public void TearDown()
        {
            // Освобождаем контекст
            _context?.Dispose();
        }
        [SetUp]
        public void Setup()
        {
           
            _configurationMock = new Mock<IConfiguration>();
            _passportRepositoryMock = new Mock<IPassportRepository>();
            _loggerMock = new Mock<ILogger<PassportRepository>>();

        
            var options = new DbContextOptionsBuilder<PassportDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

        
            _context = new PassportDbContext(options, _configurationMock.Object);
        }

        [TestCase("1234", 2)]
        [TestCase("5678", 1)]
        public async Task GetPassportsBySeries_ShouldReturnListOfPassports_WhenSeriesExists(string series, int expectedCount)
        {      
            var passports = new List<Passport>
            {
                new Passport { Series = "1234", Number = "567890" },
                new Passport { Series = "1234", Number = "098765" },
                new Passport { Series = "5678", Number = "098765" }
            };
            _context.Passports.AddRange(passports);
            await _context.SaveChangesAsync();

            //var repository = new PassportRepository(_configurationMock.Object, _context, _loggerMock.Object); // Создаем репозиторий с использованием контекста

            //// Act: вызываем метод
            //var result = await repository.GetPassportsBySeries(series);


            Console.WriteLine("series " + series );
            _passportRepositoryMock.Setup(repo => repo.GetPassportsBySeries(series))
                                   .ReturnsAsync(passports);

            var result = await _passportRepositoryMock.Object.GetPassportsBySeries(series);
            Console.WriteLine("series " + series + " count " + result.Count);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCount, result.Count);
            if(expectedCount > 0)
            {
                Assert.IsTrue(result.TrueForAll(p => p.Series == series));
            }
        }


        [TestCase("567890", 1)]
        [TestCase("098765", 2)]
        public async Task GetPassportsByNumber_ShouldReturnListOfPassports_WhenNumberExists(string number, int expectedCount)
        {
            var passports = new List<Passport>
            {
                new Passport { Series = "1234", Number = "567890" },
                new Passport { Series = "1234", Number = "098765" },
                new Passport { Series = "5678", Number = "098765" }
            };
            Console.WriteLine("number " + number);
            _passportRepositoryMock.Setup(repo => repo.GetPassportsByNumber(number))
                                   .ReturnsAsync(passports);

            var result = await _passportRepositoryMock.Object.GetPassportsByNumber(number);
            Console.WriteLine("number " + number + " count " + result.Count);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedCount, result.Count);
            if(expectedCount > 0)
            {
                Assert.IsTrue(result.TrueForAll(p => p.Number == number));
            }
        }
    }
}
