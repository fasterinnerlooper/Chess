using System.Net;
using System.Net.Http.Json;
using Chesster.Api.Controllers;
using Chesster.Api.Models.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Moq;

namespace Chesster.Api.Tests;

public class GamesControllerTests : TestBase
{
    private readonly GamesController _controller;

    public GamesControllerTests()
    {
        _controller = new GamesController(_mockGameService.Object);

        // Setup controller context for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetGames_ReturnsUserGames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var games = new List<GameDto>
        {
            new GameDto(Guid.NewGuid(), "1. e4 e5 *", "Player1", "Player2", "*", null, null, null, DateTime.UtcNow, null),
            new GameDto(Guid.NewGuid(), "1. d4 d5 *", "Player3", "Player4", "1-0", null, null, null, DateTime.UtcNow, null)
        };

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.GetUserGamesAsync(userId))
            .ReturnsAsync(games);

        // Act
        var result = await _controller.GetGames();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        result.Result.Should().BeAssignableTo<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var returnedGames = okResult!.Value as List<GameDto>;
        returnedGames.Should().NotBeNull();
        returnedGames!.Should().HaveCount(2);
        returnedGames[0].White.Should().Be("Player1");
        returnedGames[1].White.Should().Be("Player3");
    }

    [Fact]
    public async Task GetGame_ExistingGame_ReturnsGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = new GameDto(gameId, "1. e4 e5 *", "White", "Black", "*", null, null, null, DateTime.UtcNow, null);

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.GetGameByIdAsync(gameId, userId))
            .ReturnsAsync(game);

        // Act
        var result = await _controller.GetGame(gameId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedGame = okResult!.Value as GameDto;
        returnedGame.Should().NotBeNull();
        returnedGame!.Id.Should().Be(gameId);
        returnedGame.White.Should().Be("White");
        returnedGame.Black.Should().Be("Black");
    }

    [Fact]
    public async Task GetGame_NonExistingGame_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.GetGameByIdAsync(gameId, userId))
            .ReturnsAsync((GameDto)null!);

        // Act
        var result = await _controller.GetGame(gameId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Game not found" });
    }

    [Fact]
    public async Task CreateGame_ValidRequest_ReturnsCreatedGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateGameRequest(
            Pgn: "[Event \"New Game\"]\n1. e4 e5 *",
            White: "Player White",
            Black: "Player Black",
            Result: "*",
            Event: "Test Tournament",
            Site: "Test Site",
            Date: DateTime.UtcNow.Date
        );

        var createdGame = new GameDto(
            Guid.NewGuid(),
            request.Pgn,
            request.White,
            request.Black,
            request.Result,
            request.Event,
            request.Site,
            request.Date,
            DateTime.UtcNow,
            null
        );

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.CreateGameAsync(userId, request))
            .ReturnsAsync(createdGame);

        // Act
        var result = await _controller.CreateGame(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(GamesController.GetGame));
        createdResult.RouteValues!["id"].Should().Be(createdGame.Id);
        var returnedGame = createdResult.Value as GameDto;
        returnedGame.Should().NotBeNull();
        returnedGame!.White.Should().Be("Player White");
        returnedGame.Black.Should().Be("Player Black");
    }

    [Fact]
    public async Task UpdateGame_ExistingGame_ReturnsUpdatedGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var updateRequest = new UpdateGameRequest(
            Pgn: "1. e4 e5 2. Nf3 *",
            White: "Updated White",
            Black: "Updated Black",
            Result: "1-0",
            Event: null,
            Site: null,
            Date: null
        );

        var updatedGame = new GameDto(
            gameId,
            "1. e4 e5 2. Nf3 *",
            "Updated White",
            "Updated Black",
            "1-0",
            null,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.UpdateGameAsync(gameId, userId, updateRequest))
            .ReturnsAsync(updatedGame);

        // Act
        var result = await _controller.UpdateGame(gameId, updateRequest);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedGame = okResult!.Value as GameDto;
        returnedGame.Should().NotBeNull();
        returnedGame!.White.Should().Be("Updated White");
        returnedGame.Black.Should().Be("Updated Black");
        returnedGame.Result.Should().Be("1-0");
    }

    [Fact]
    public async Task UpdateGame_NonExistingGame_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var updateRequest = new UpdateGameRequest(Pgn: "1. e4 *", White: "Updated White", Black: null, Result: null, Event: null, Site: null, Date: null);

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.UpdateGameAsync(gameId, userId, updateRequest))
            .ReturnsAsync((GameDto)null!);

        // Act
        var result = await _controller.UpdateGame(gameId, updateRequest);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Game not found" });
    }

    [Fact]
    public async Task DeleteGame_ExistingGame_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.DeleteGameAsync(gameId, userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteGame(gameId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGame_NonExistingGame_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.DeleteGameAsync(gameId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteGame(gameId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Game not found" });
    }

    [Fact]
    public async Task ImportPgn_ValidPgn_ReturnsCreatedGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pgn = "[Event \"Test\"]\n[White \"White\"]\n[Black \"Black\"]\n1. e4 e5 *";
        var request = new ImportPgnRequest(pgn);

        var importedGame = new GameDto(
            Guid.NewGuid(),
            pgn,
            "White",
            "Black",
            "*",
            "Test",
            null,
            null,
            DateTime.UtcNow,
            null
        );

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        _mockGameService
            .Setup(x => x.ImportPgnAsync(userId, pgn))
            .ReturnsAsync(importedGame);

        // Act
        var result = await _controller.ImportPgn(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(GamesController.GetGame));
        var returnedGame = createdResult.Value as GameDto;
        returnedGame.Should().NotBeNull();
        returnedGame!.White.Should().Be("White");
        returnedGame.Black.Should().Be("Black");
    }
}