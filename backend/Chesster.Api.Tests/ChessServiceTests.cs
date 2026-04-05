using Chesster.Api.Services;
using FluentAssertions;
using Xunit;

namespace Chesster.Api.Tests;

public class ChessServiceTests
{
    private readonly ChessService _chessService;

    public ChessServiceTests()
    {
        _chessService = new ChessService();
    }

    [Fact]
    public void IsValidMove_StartingPosition_ValidPawnMove_ReturnsTrue()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var san = "e4";

        // Act
        var result = _chessService.IsValidMove(fen, san);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidMove_StartingPosition_InvalidMove_ReturnsFalse()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var san = "e5"; // Pawn can't move two squares from starting position if not first move

        // Act
        var result = _chessService.IsValidMove(fen, san);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SanToUci_StartingPosition_e4_ReturnsCorrectUci()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var san = "e4";

        // Act
        var result = _chessService.SanToUci(fen, san);

        // Assert
        result.Should().Be("e2e4");
    }

    [Fact]
    public void GetFenAfterMove_StartingPosition_e4_ReturnsCorrectFen()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var san = "e4";

        // Act
        var result = _chessService.GetFenAfterMove(fen, san);

        // Assert
        result.Should().Be("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1");
    }

    [Fact]
    public void GetTurn_WhiteToMove_ReturnsWhite()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Act
        var result = _chessService.GetTurn(fen);

        // Assert
        result.Should().Be("w");
    }

    [Fact]
    public void GetTurn_BlackToMove_ReturnsBlack()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

        // Act
        var result = _chessService.GetTurn(fen);

        // Assert
        result.Should().Be("b");
    }

    [Fact]
    public void GetMoveNumber_StartingPosition_Returns1()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Act
        var result = _chessService.GetMoveNumber(fen);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetMoveNumber_AfterMoves_ReturnsCorrectNumber()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

        // Act
        var result = _chessService.GetMoveNumber(fen);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ChessPosition_FromFen_StartingPosition_ParsesCorrectly()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Act
        var position = ChessPosition.FromFen(fen);

        // Assert
        position.WhiteToMove.Should().BeTrue();
        position.CastlingRights.Should().Be("KQkq");
        position.EnPassantSquare.Should().BeNull();
        position.HalfMoveClock.Should().Be(0);
        position.FullMoveNumber.Should().Be(1);

        // Check some pieces
        position.Board[0][0].Should().Be("r"); // a8 rook
        position.Board[0][4].Should().Be("k"); // e8 king
        position.Board[7][0].Should().Be("R"); // a1 rook
        position.Board[7][4].Should().Be("K"); // e1 king
    }

    [Fact]
    public void ChessPosition_ToFen_StartingPosition_ReturnsOriginalFen()
    {
        // Arrange
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var position = ChessPosition.FromFen(fen);

        // Act
        var result = position.ToFen();

        // Assert
        result.Should().Be(fen);
    }
}