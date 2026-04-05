using System.Text.RegularExpressions;
using Chesster.Api.Models.DTOs;

namespace Chesster.Api.Services;

public class ParsedMove
{
    public int MoveNumber { get; set; }
    public string Side { get; set; } = string.Empty;
    public string San { get; set; } = string.Empty;
    public string Uci { get; set; } = string.Empty;
    public string Fen { get; set; } = string.Empty;
    public string? Comment { get; set; }
}

public class ParsedGame
{
    public string White { get; set; } = string.Empty;
    public string Black { get; set; } = string.Empty;
    public string Result { get; set; } = "*";
    public string? Event { get; set; }
    public string? Site { get; set; }
    public DateTime? Date { get; set; }
    public List<ParsedMove> Moves { get; set; } = new();
}

public interface IPgnParserService
{
    ParsedGame Parse(string pgn);
    string GeneratePgn(ParsedGame game);
}

public class PgnParserService : IPgnParserService
{
    private readonly IChessService _chessService;

    public PgnParserService(IChessService chessService)
    {
        _chessService = chessService;
    }

    public ParsedGame Parse(string pgn)
    {
        var result = new ParsedGame();
        var currentFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        var headerMatch = Regex.Match(pgn, @"\[(\w+)\s+""([^""]+)""\]");
        while (headerMatch.Success)
        {
            var tag = headerMatch.Groups[1].Value;
            var value = headerMatch.Groups[2].Value;

            switch (tag.ToLower())
            {
                case "white": result.White = value; break;
                case "black": result.Black = value; break;
                case "result": result.Result = value; break;
                case "event": result.Event = value; break;
                case "site": result.Site = value; break;
                case "date":
                    if (DateTime.TryParse(value.Replace("????", "2024"), out var date))
                        result.Date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    break;
            }

            headerMatch = headerMatch.NextMatch();
        }

        var movesSection = Regex.Replace(pgn, @"\[.*?\]", "").Trim();
        movesSection = Regex.Replace(movesSection, @"\([^)]*\)", "");
        
        var movePattern = @"(\d+\.)([^\d]+)";
        var moveMatches = Regex.Matches(movesSection, movePattern);
        var moveNumber = 1;
        var isWhiteTurn = true;

        foreach (Match match in moveMatches)
        {
            var moveText = match.Groups[2].Value.Trim();
            var parts = moveText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part) || part == "*" || 
                    part == "1-0" || part == "0-1" || part == "1/2-1/2")
                    continue;

                var san = Regex.Replace(part, @"\$.*\d", "");
                if (string.IsNullOrWhiteSpace(san)) continue;

                if (_chessService.IsValidMove(currentFen, san))
                {
                    var uci = _chessService.SanToUci(currentFen, san);
                    currentFen = _chessService.GetFenAfterMove(currentFen, san);
                    
                    result.Moves.Add(new ParsedMove
                    {
                        MoveNumber = moveNumber,
                        Side = isWhiteTurn ? "w" : "b",
                        San = san,
                        Uci = uci,
                        Fen = currentFen
                    });

                    if (!isWhiteTurn) moveNumber++;
                    isWhiteTurn = !isWhiteTurn;
                }
            }
        }

        return result;
    }

    public string GeneratePgn(ParsedGame game)
    {
        var lines = new List<string>
        {
            $"""[White "{game.White}"]""",
            $"""[Black "{game.Black}"]""",
            $"""[Result "{game.Result}"]"""
        };

        if (!string.IsNullOrEmpty(game.Event))
            lines.Add($"""[Event "{game.Event}"]""");
        if (!string.IsNullOrEmpty(game.Site))
            lines.Add($"""[Site "{game.Site}"]""");
        if (game.Date.HasValue)
            lines.Add($"""[Date "{game.Date:yyyy.MM.dd}"]""");

        lines.Add("");

        var currentFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var moveNum = 1;
        
        foreach (var move in game.Moves)
        {
            var isWhite = move.Side == "w";
            if (isWhite)
            {
                lines.Add($"{moveNum}. {move.San}");
            }
            else
            {
                lines[^1] += $" {move.San}";
                moveNum++;
            }

            currentFen = _chessService.GetFenAfterMove(currentFen, move.San);
        }

        lines.Add(game.Result);

        return string.Join("\n", lines);
    }
}