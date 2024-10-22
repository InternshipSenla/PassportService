
using PassportService;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Moq;
using PassportService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PassportService.Core;

namespace PassportService.Tests
{
    public class PassportServiceTests
    {
        private readonly Mock<PassportDbContext> _dbContextMock;
        private readonly PassportService.Services.IPassportRepository _passportService;
    }
}
