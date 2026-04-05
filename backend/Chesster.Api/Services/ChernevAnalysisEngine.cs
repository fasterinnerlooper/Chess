using System.Diagnostics;
using Chesster.Api.Models.DTOs;

namespace Chesster.Api.Services;

public interface IAnalysisEngine
{
    Task<ChernevExplanation> AnalyzePositionAsync(string fen, string move, string side);
    Task<List<CandidateMove>> GetCandidateMovesAsync(string fen, int depth = 3);
}

public class ChernevAnalysisEngine : IAnalysisEngine
{
    private readonly ILogger<ChernevAnalysisEngine> _logger;
    private readonly IChessService _chessService;
    private readonly string _stockfishPath;

    public ChernevAnalysisEngine(ILogger<ChernevAnalysisEngine> logger, IChessService chessService, IConfiguration configuration)
    {
        _logger = logger;
        _chessService = chessService;
        _stockfishPath = configuration["Stockfish:Path"] ?? "stockfish";
    }

    public async Task<ChernevExplanation> AnalyzePositionAsync(string fen, string move, string side)
    {
        var evaluation = await EvaluatePositionAsync(fen);
        var bestMoveEval = await GetBestMoveEvaluationAsync(fen);

        var moveQuality = DetermineMoveQuality(evaluation);
        var whyItWasPlayed = GenerateWhyExplanation(move, side, evaluation);
        var alternatives = await GetCandidateMovesAsync(fen);
        var strategicIdeas = IdentifyStrategicIdeas(fen, side);
        var whatToConsider = GenerateWhatToConsider(move, side, strategicIdeas);

        return new ChernevExplanation(
            moveQuality,
            whyItWasPlayed,
            alternatives,
            strategicIdeas,
            whatToConsider,
            evaluation,
            bestMoveEval
        );
    }

    public async Task<List<CandidateMove>> GetCandidateMovesAsync(string fen, int depth = 3)
    {
        return new List<CandidateMove>();
    }

    private async Task<int?> EvaluatePositionAsync(string fen)
    {
        try
        {
            var result = await RunStockfishAsync(fen);
            return ParseEvaluation(result);
        }
        catch
        {
            return GenerateHeuristicEvaluation(fen);
        }
    }

    private async Task<int?> GetBestMoveEvaluationAsync(string fen)
    {
        return await EvaluatePositionAsync(fen);
    }

    private async Task<string> RunStockfishAsync(string fen)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _stockfishPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return string.Empty;

            await process.StandardInput.WriteLineAsync($"position fen {fen}");
            await process.StandardInput.WriteLineAsync("eval");
            await process.StandardInput.WriteLineAsync("quit");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stockfish not available, using heuristic evaluation");
            return string.Empty;
        }
    }

    private int? ParseEvaluation(string output)
    {
        if (string.IsNullOrEmpty(output)) return null;
        
        var match = System.Text.RegularExpressions.Regex.Match(output, @"Centipawn:\s*(-?\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var eval))
            return eval;

        return null;
    }

    private int GenerateHeuristicEvaluation(string fen)
    {
        var pos = ChessPosition.FromFen(fen);
        var score = 0;

        var pieceValues = new Dictionary<char, int>
        {
            { 'P', 100 }, { 'N', 320 }, { 'B', 330 }, { 'R', 500 }, { 'Q', 900 }, { 'K', 20000 },
            { 'p', -100 }, { 'n', -320 }, { 'b', -330 }, { 'r', -500 }, { 'q', -900 }, { 'k', -20000 }
        };

        var positionalBonus = new Dictionary<char, int[,]>
        {
            { 'P', new int[,] { {0,0,0,0,0,0,0,0}, {50,50,50,50,50,50,50,50}, {10,10,20,30,30,20,10,10}, {5,5,10,25,25,10,5,5}, {0,0,0,20,20,0,0,0}, {5,-5,-10,0,0,-10,-5,5}, {5,10,10,-20,-20,10,10,5}, {0,0,0,0,0,0,0,0} } },
            { 'N', new int[,] { {-50,-40,-30,-30,-30,-30,-40,-50}, {-40,-20,0,0,0,0,-20,-40}, {-30,0,10,15,15,10,0,-30}, {-30,5,15,20,20,15,5,-30}, {-30,0,15,20,20,15,0,-30}, {-30,5,10,15,15,10,5,-30}, {-40,-20,0,5,5,0,-20,-40}, {-50,-40,-30,-30,-30,-30,-40,-50} } },
            { 'B', new int[,] { {-20,-10,-10,-10,-10,-10,-10,-20}, {-10,0,0,0,0,0,0,-10}, {-10,0,5,10,10,5,0,-10}, {-10,5,5,10,10,5,5,-10}, {-10,0,10,10,10,10,0,-10}, {-10,10,10,10,10,10,10,-10}, {-10,5,0,0,0,0,5,-10}, {-20,-10,-10,-10,-10,-10,-10,-20} } },
        };

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var piece = pos.Board[r][c];
                if (piece == null) continue;
                
                var p = piece[0];
                if (pieceValues.TryGetValue(p, out var value))
                {
                    score += value;
                    
                    var isWhite = char.IsUpper(p);
                    var normalizedR = isWhite ? r : 7 - r;
                    
                    if (positionalBonus.ContainsKey(char.ToUpper(p)))
                    {
                        var bonusMap = positionalBonus[char.ToUpper(p)];
                        score += bonusMap[normalizedR, c];
                    }
                }
            }
        }

        return score / 10;
    }

    private string DetermineMoveQuality(int? evaluation)
    {
        if (!evaluation.HasValue) return "unknown";
        
        var eval = evaluation.Value;
        return eval switch
        {
            >= 100 => "excellent",
            >= 30 => "good",
            >= -30 => "inaccuracy",
            >= -100 => "mistake",
            _ => "blunder"
        };
    }

    private string GenerateWhyExplanation(string move, string side, int? evaluation)
    {
        var isWhite = side == "w";
        var player = isWhite ? "White" : "Black";
        
        if (move.Contains("x"))
            return $"{player} captures with {move}, gaining material or forcing a trade.";
        
        if (move.Contains("+"))
            return $"{player} delivers check with {move}, forcing the opponent to respond.";
        
        if (move.Contains("="))
            return $"{player} promotes a pawn with {move}.";
        
        if (System.Text.RegularExpressions.Regex.IsMatch(move, @"^[KQRBN][a-h]?"))
            return $"{player} develops a piece with {move}, improving piece activity.";
        
        if (System.Text.RegularExpressions.Regex.IsMatch(move, @"^\d+\.\.\."))
            return $"{player} continues with {move}, following the game plan.";

        var pawnMoves = new[] { "e4", "d4", "c4", "b4", "a4", "e5", "d5", "c5", "b5", "a5" };
        if (pawnMoves.Contains(move))
            return $"{player} advances a pawn with {move}, controlling central squares.";

        return $"{player} plays {move}, following standard chess principles.";
    }

    private string GetMoveReason(string move)
    {
        if (move.Contains("x")) return "Captures material";
        if (move.Contains("+")) return "Checks the king";
        if (move.Contains("#")) return "Delivers checkmate";
        if (System.Text.RegularExpressions.Regex.IsMatch(move, @"^[KQRBN]")) return "Develops piece";
        return "Positional move";
    }

    private List<string> IdentifyStrategicIdeas(string fen, string side)
    {
        var ideas = new List<string>();
        var pos = ChessPosition.FromFen(fen);
        var isWhite = side == "w";

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var piece = pos.Board[r][c];
                if (piece == null) continue;
                
                var isPieceWhite = char.IsUpper(piece[0]);
                if (isPieceWhite != isWhite) continue;

                var p = char.ToLower(piece[0]);
                if (p == 'r' || p == 'q')
                {
                    if (r >= 6 || r <= 1)
                    {
                        ideas.Add("Piece development");
                        break;
                    }
                }
            }
            if (ideas.Count > 0) break;
        }

        var pawnCount = 0;
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var piece = pos.Board[r][c];
                if (piece != null && piece.ToLower()[0] == 'p')
                {
                    var isPieceWhite = char.IsUpper(piece[0]);
                    if (isPieceWhite == isWhite)
                        pawnCount++;
                }
            }
        }

        if (pawnCount > 4) ideas.Add("Central control");
        if (ideas.Count == 0) ideas.Add("Standard development");

        return ideas;
    }

    private string GenerateWhatToConsider(string move, string side, List<string> strategicIdeas)
    {
        return $"Consider whether {move} achieves the strategic goals: {string.Join(", ", strategicIdeas)}. Did you consider alternative moves that might achieve similar goals?";
    }
}