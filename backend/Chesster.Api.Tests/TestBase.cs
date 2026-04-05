using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Chesster.Api.Data;
using Chesster.Api.Services;
using Moq;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Chesster.Api.Tests;

public class TestBase
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ChessterDbContext _dbContext;
    protected readonly Mock<IAnalysisEngine> _mockAnalysisEngine;
    protected readonly Mock<IAuthService> _mockAuthService;
    protected readonly Mock<IJwtService> _mockJwtService;
    protected readonly Mock<IGameService> _mockGameService;
    protected readonly Mock<IPgnParserService> _mockPgnParser;

    public TestBase()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<ChessterDbContext>(options =>
        {
            options.UseInMemoryDatabase("TestDb");
        });

        // Mock services
        _mockAnalysisEngine = new Mock<IAnalysisEngine>();
        _mockAnalysisEngine
            .Setup(x => x.AnalyzePositionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Models.DTOs.ChernevExplanation(
                "Good",
                "Develops pieces",
                new List<Models.DTOs.CandidateMove>(),
                new List<string> { "Control center" },
                "Consider piece activity",
                50,
                100
            ));

        _mockAuthService = new Mock<IAuthService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockGameService = new Mock<IGameService>();
        _mockPgnParser = new Mock<IPgnParserService>();

        services.AddSingleton(_mockAnalysisEngine.Object);
        services.AddSingleton(_mockAuthService.Object);
        services.AddSingleton(_mockJwtService.Object);
        services.AddSingleton(_mockGameService.Object);
        services.AddSingleton(_mockPgnParser.Object);

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ChessterDbContext>();

        // Ensure clean database for each test
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();
    }

    protected Guid CreateTestUser(string username = "testuser", string email = "test@example.com")
    {
        var userId = Guid.NewGuid();
        var user = new Models.User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = "hashedpassword"
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        return userId;
    }

    protected Models.Game CreateTestGame(Guid userId, string pgn = "1. e4 e5 *")
    {
        var game = new Models.Game
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Pgn = pgn,
            White = "White",
            Black = "Black",
            Result = "*"
        };

        _dbContext.Games.Add(game);
        _dbContext.SaveChanges();
        return game;
    }
}