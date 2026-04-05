using Chesster.Api.Models;
using Chesster.Api.Models.DTOs;
using Chesster.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Chesster.Api.Tests;

public class GameServiceTests : TestBase
{
    private readonly Mock<IPgnParserService> _mockPgnParser;
    private readonly Mock<IAnalysisEngine> _mockAnalysisEngine;
    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _mockPgnParser = new Mock<IPgnParserService>();
        _mockAnalysisEngine = new Mock<IAnalysisEngine>();
        _gameService = new GameService(_dbContext, _mockPgnParser.Object, _mockAnalysisEngine.Object);
    }

    [Fact]
    public async Task GetUserGamesAsync_UserWithGames_ReturnsGameDtos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var game1 = new Game { Id = Guid.NewGuid(), UserId = userId, Pgn = "1. e4 e5", White = "White", Black = "Black", Result = "1-0", CreatedAt = DateTime.UtcNow };
        var game2 = new Game { Id = Guid.NewGuid(), UserId = userId, Pgn = "1. d4 d5", White = "White2", Black = "Black2", Result = "0-1", CreatedAt = DateTime.UtcNow.AddSeconds(-1) };
        _dbContext.Games.AddRange(game1, game2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetUserGamesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result[0].Pgn.Should().Be("1. e4 e5");
        result[1].Pgn.Should().Be("1. d4 d5");
    }

    [Fact]
    public async Task GetUserGamesAsync_UserWithoutGames_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _gameService.GetUserGamesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGameByIdAsync_ExistingGame_ReturnsGameDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, UserId = userId, Pgn = "1. e4 e5", White = "White", Black = "Black", Result = "1-0" };
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetGameByIdAsync(gameId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(gameId);
        result.Pgn.Should().Be("1. e4 e5");
    }

    [Fact]
    public async Task GetGameByIdAsync_NonExistingGame_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Act
        var result = await _gameService.GetGameByIdAsync(gameId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGameByIdAsync_WrongUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, UserId = userId, Pgn = "1. e4 e5", White = "White", Black = "Black", Result = "1-0" };
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.GetGameByIdAsync(gameId, wrongUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateGameAsync_ValidRequest_CreatesAndReturnsGameDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateGameRequest(
            Pgn: "1. e4 e5",
            White: "White Player",
            Black: "Black Player",
            Result: "1-0",
            Event: "Test Event",
            Site: "Test Site",
            Date: new DateTime(2023, 1, 1)
        );

        // Act
        var result = await _gameService.CreateGameAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Pgn.Should().Be("1. e4 e5");
        result.White.Should().Be("White Player");
        result.Black.Should().Be("Black Player");
        result.Result.Should().Be("1-0");
        result.Event.Should().Be("Test Event");
        result.Site.Should().Be("Test Site");
        result.Date.Should().Be(new DateTime(2023, 1, 1));

        var gameInDb = await _dbContext.Games.FirstOrDefaultAsync(g => g.UserId == userId);
        gameInDb.Should().NotBeNull();
        gameInDb.Pgn.Should().Be("1. e4 e5");
    }

    [Fact]
    public async Task UpdateGameAsync_ExistingGame_UpdatesAndReturnsGameDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, UserId = userId, Pgn = "1. e4 e5", White = "White", Black = "Black", Result = "*" };
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateGameRequest(
            Pgn: "1. e4 e5 2. Nf3",
            White: "Updated White",
            Black: "Updated Black",
            Result: "1-0",
            Event: null,
            Site: null,
            Date: null
        );

        // Act
        var result = await _gameService.UpdateGameAsync(gameId, userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Pgn.Should().Be("1. e4 e5 2. Nf3");
        result.White.Should().Be("Updated White");
        result.Black.Should().Be("Updated Black");
        result.Result.Should().Be("1-0");

        var updatedGame = await _dbContext.Games.FindAsync(gameId);
        updatedGame.Pgn.Should().Be("1. e4 e5 2. Nf3");
    }

    [Fact]
    public async Task UpdateGameAsync_NonExistingGame_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var request = new UpdateGameRequest(Pgn: "1. e4 e5", null, null, null, null, null, null);

        // Act
        var result = await _gameService.UpdateGameAsync(gameId, userId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGameAsync_ExistingGame_DeletesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new Game { Id = gameId, UserId = userId, Pgn = "1. e4 e5", White = "White", Black = "Black", Result = "*" };
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _gameService.DeleteGameAsync(gameId, userId);

        // Assert
        result.Should().BeTrue();

        var deletedGame = await _dbContext.Games.FindAsync(gameId);
        deletedGame.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGameAsync_NonExistingGame_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Act
        var result = await _gameService.DeleteGameAsync(gameId, userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ImportPgnAsync_ValidPgn_CreatesGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pgn = @"[Event ""Test Game""]
[White ""White Player""]
[Black ""Black Player""]
[Result ""1-0""]

1. e4 e5";

        var parsedGame = new ParsedGame
        {
            Event = "Test Game",
            White = "White Player",
            Black = "Black Player",
            Result = "1-0",
            Moves = new List<ParsedMove>()
        };

        _mockPgnParser.Setup(x => x.Parse(pgn)).Returns(parsedGame);

        // Act
        var result = await _gameService.ImportPgnAsync(userId, pgn);

        // Assert
        result.Should().NotBeNull();
        result.Event.Should().Be("Test Game");
        result.White.Should().Be("White Player");
        result.Black.Should().Be("Black Player");
        result.Result.Should().Be("1-0");

        var gameInDb = await _dbContext.Games.FirstOrDefaultAsync(g => g.UserId == userId);
        gameInDb.Should().NotBeNull();
        gameInDb.Pgn.Should().Be(pgn);
    }
}