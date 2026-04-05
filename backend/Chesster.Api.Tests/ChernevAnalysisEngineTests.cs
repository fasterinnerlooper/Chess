using Chesster.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chesster.Api.Tests;

public class ChernevAnalysisEngineTests
{
    private readonly Mock<ILogger<ChernevAnalysisEngine>> _mockLogger;
    private readonly Mock<IChessService> _mockChessService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ChernevAnalysisEngine _analysisEngine;

    public ChernevAnalysisEngineTests()
    {
        _mockLogger = new Mock<ILogger<ChernevAnalysisEngine>>();
        _mockChessService = new Mock<IChessService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Stockfish:Path"]).Returns("stockfish");

        _analysisEngine = new ChernevAnalysisEngine(_mockLogger.Object, _mockChessService.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task AnalyzePositionAsync_ValidPosition_ReturnsExplanation()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var move = "e4";
        var side = "w";

        // Act
        var result = await _analysisEngine.AnalyzePositionAsync(fen, move, side);

        // Assert
        result.Should().NotBeNull();
        result.MoveQuality.Should().NotBeNullOrEmpty();
        result.WhyItWasPlayed.Should().NotBeNullOrEmpty();
        result.StrategicIdeas.Should().NotBeNullOrEmpty();
        result.WhatToConsider.Should().NotBeNullOrEmpty();
        // Note: Evaluation and BestMoveEvaluation may be null if Stockfish is not available
    }

    [Fact]
    public async Task GetCandidateMovesAsync_ValidFen_ReturnsEmptyList()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Act
        var result = await _analysisEngine.GetCandidateMovesAsync(fen);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Current implementation returns empty list
    }

    // Additional tests could be added for the private methods if made internal or using reflection,
    // but for now, we'll focus on the public interface
}