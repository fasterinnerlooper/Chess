using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Chesster.Api.Controllers;
using FluentAssertions;
using Xunit;
using Chesster.Api.Models.DTOs;
using Moq;
using System;

namespace Chesster.Api.Tests;

public class AnalysisControllerTests : TestBase
{
    private readonly AnalysisController _controller;

    public AnalysisControllerTests()
    {
        _controller = new AnalysisController(_dbContext, _mockAnalysisEngine.Object, _mockPgnParser.Object);

        // Setup controller context for testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetGameAnalysis_ExistingGameWithAnalysis_ReturnsAnalysisList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Create user and game
        var user = new Models.User { Id = userId, Username = "test", Email = "test@test.com" };
        var game = new Models.Game
        {
            Id = gameId,
            UserId = userId,
            Pgn = "1. e4 e5 *",
            White = "White",
            Black = "Black",
            Result = "*"
        };

        _dbContext.Users.Add(user);
        _dbContext.Games.Add(game);

        // Create analysis
        var analysis = new Models.GameAnalysis
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            MoveNumber = 1,
            Side = "w",
            Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            Move = "e4",
            AnalysisJson = @"{""MoveQuality"":""Good"",""WhyItWasPlayed"":""Controls center"",""Alternatives"":[],""StrategicIdeas"":[""Center control""],""WhatToConsider"":""Development"",""Evaluation"":50,""BestMoveEvaluation"":100}"
        };

        _dbContext.GameAnalyses.Add(analysis);
        await _dbContext.SaveChangesAsync();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        // Act
        var result = await _controller.GetGameAnalysis(gameId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var analyses = okResult!.Value as List<AnalysisDto>;
        analyses.Should().NotBeNull();
        analyses!.Should().HaveCount(1);
        analyses[0].Move.Should().Be("e4");
    }

    [Fact]
    public async Task GetGameAnalysis_NonExistingGame_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        // Act
        var result = await _controller.GetGameAnalysis(gameId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Game not found" });
    }

    [Fact]
    public async Task AnalyzeGame_ValidGame_ReturnsAnalysisList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Create user and game
        var user = new Models.User { Id = userId, Username = "test", Email = "test@test.com" };
        var game = new Models.Game
        {
            Id = gameId,
            UserId = userId,
            Pgn = "[Event \"Test\"]\n[White \"White\"]\n[Black \"Black\"]\n1. e4 e5 *",
            White = "White",
            Black = "Black",
            Result = "*"
        };

        _dbContext.Users.Add(user);
        _dbContext.Games.Add(game);
        await _dbContext.SaveChangesAsync();

        // Mock PGN parser
        var parsedMoves = new List<Chesster.Api.Services.ParsedMove>
        {
            new Chesster.Api.Services.ParsedMove
            {
                MoveNumber = 1,
                Side = "w",
                Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                San = "e4",
                Uci = "e4"
            },
            new Chesster.Api.Services.ParsedMove
            {
                MoveNumber = 1,
                Side = "b",
                Fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
                San = "e5",
                Uci = "e5"
            }
        };

        _mockPgnParser
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new Chesster.Api.Services.ParsedGame { Moves = parsedMoves });

        // Mock analysis engine
        _mockAnalysisEngine
            .Setup(x => x.AnalyzePositionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ChernevExplanation("Good", "Test reason", new List<CandidateMove>(), new List<string> { "Test idea" }, "Test consider", 50, 100));

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        // Act
        var result = await _controller.AnalyzeGame(gameId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var analyses = okResult!.Value as List<AnalysisDto>;
        analyses.Should().NotBeNull();
        analyses!.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnalyzeGame_NonExistingGame_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        // Setup HttpContext with claims
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.HttpContext.User = principal;

        // Act
        var result = await _controller.AnalyzeGame(gameId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Game not found" });
    }
}