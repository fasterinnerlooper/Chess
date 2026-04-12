using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Chesster.Api.Models.DTOs;

public record RegisterRequest(
    [Required][MaxLength(50)] string Username,
    [Required][EmailAddress][MaxLength(255)] string Email,
    [Required][MinLength(6)] string Password
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    DateTime CreatedAt
);

public record GameDto(
    Guid Id,
    string Pgn,
    string White,
    string Black,
    string Result,
    string? Event,
    string? Site,
    DateTime? Date,
    DateTime CreatedAt,
    List<AnalysisDto>? Analyses
);

public record CreateGameRequest(
    [Required] string Pgn,
    [MaxLength(100)] string White,
    [MaxLength(100)] string Black,
    [MaxLength(10)] string Result,
    [MaxLength(255)] string? Event,
    [MaxLength(255)] string? Site,
    DateTime? Date
);

public record UpdateGameRequest(
    string? Pgn,
    string? White,
    string? Black,
    string? Result,
    string? Event,
    string? Site,
    DateTime? Date
);

public record AnalysisDto(
    Guid Id,
    int MoveNumber,
    string Side,
    string Fen,
    string Move,
    ChernevExplanation Explanation
);

public record ChernevExplanation(
    string MoveQuality,
    string WhyItWasPlayed,
    List<CandidateMove> Alternatives,
    List<string> StrategicIdeas,
    string WhatToConsider,
    int? Evaluation,
    int? BestMoveEvaluation
);

public record CandidateMove(
    string Move,
    string Reason,
    int Evaluation
);

public record OAuthCallbackRequest(
    [Required] string Code,
    string? State
);

public record StockfishBestMoveResponse(
    string? BestMove
);

public record StockfishApiResponse(
    StockfishBestMoveResponse? Result
);

public record StockfishEvalResponse(
    string? Evaluation
);

public record StockfishScoreDto(
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("value")] int Value
);

public record StockfishInfoDto(
    [property: JsonPropertyName("string")] string InfoString,
    [property: JsonPropertyName("depth")] int Depth,
    [property: JsonPropertyName("seldepth")] int Seldepth,
    [property: JsonPropertyName("time")] int Time,
    [property: JsonPropertyName("nodes")] int Nodes,
    [property: JsonPropertyName("nps")] int Nps,
    [property: JsonPropertyName("tbhits")] int Tbhits,
    [property: JsonPropertyName("score")] StockfishScoreDto Score,
    [property: JsonPropertyName("multipv")] int Multipv,
    [property: JsonPropertyName("pv")] string Pv
);

public record StockfishResultDto(
    [property: JsonPropertyName("bestmove")] string Bestmove,
    [property: JsonPropertyName("info")] List<StockfishInfoDto> Info,
    [property: JsonPropertyName("ponder")] string Ponder,
    [property: JsonPropertyName("fen")] string Fen
);

public record StockfishBestMoveApiResponse(
    [property: JsonPropertyName("result")] StockfishResultDto Result
);