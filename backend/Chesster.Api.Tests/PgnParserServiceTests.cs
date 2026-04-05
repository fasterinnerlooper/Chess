using Chesster.Api.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Chesster.Api.Tests;

public class PgnParserServiceTests
{
    private readonly PgnParserService _pgnParser;

    public PgnParserServiceTests()
    {
        var chessService = new ChessService();
        _pgnParser = new PgnParserService(chessService);
    }

    [Fact]
    public void Parse_SimplePgn_ReturnsParsedGame()
    {
        // Arrange
        var pgn = @"[Event ""Test Game""]
[Site ""Test Site""]
[Date ""2023.01.01""]
[White ""White Player""]
[Black ""Black Player""]
[Result ""1-0""]

1. e4 e5";

        // Act
        var result = _pgnParser.Parse(pgn);

        // Assert
        result.Should().NotBeNull();
        result.Event.Should().Be("Test Game");
        result.Site.Should().Be("Test Site");
        result.Date.Should().Be(new DateTime(2023, 1, 1));
        result.White.Should().Be("White Player");
        result.Black.Should().Be("Black Player");
        result.Result.Should().Be("1-0");
        result.Moves.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_PgnWithoutHeaders_ReturnsGameWithDefaults()
    {
        // Arrange
        var pgn = "1. e4 e5 2. Nf3";

        // Act
        var result = _pgnParser.Parse(pgn);

        // Assert
        result.Should().NotBeNull();
        result.White.Should().Be(string.Empty);
        result.Black.Should().Be(string.Empty);
        result.Result.Should().Be("*");
        result.Moves.Should().HaveCount(3);
    }

    [Fact]
    public void GeneratePgn_ValidGame_ReturnsPgnString()
    {
        // Arrange
        var game = new ParsedGame
        {
            Event = "Test Event",
            Site = "Test Site",
            Date = new DateTime(2023, 1, 1),
            White = "White Player",
            Black = "Black Player",
            Result = "1-0",
            Moves = new List<ParsedMove>
            {
                new ParsedMove { MoveNumber = 1, Side = "w", San = "e4", Uci = "e2e4", Fen = "fen1" },
                new ParsedMove { MoveNumber = 1, Side = "b", San = "e5", Uci = "e7e5", Fen = "fen2" }
            }
        };

        // Act
        var result = _pgnParser.GeneratePgn(game);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("[Event \"Test Event\"]");
        result.Should().Contain("[White \"White Player\"]");
        result.Should().Contain("1. e4 e5");
        result.Should().Contain("1-0");
    }

    [Fact]
    public void Parse_PgnWithComments_IncludesComments()
    {
        // Arrange
        var pgn = "1. e4 e5 2. Nf3";

        // Act
        var result = _pgnParser.Parse(pgn);

        // Assert
        result.Moves.Should().HaveCount(3);
        // Comments not implemented yet
    }
}