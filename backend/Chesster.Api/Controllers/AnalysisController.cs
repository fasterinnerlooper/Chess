using System.Security.Claims;
using System.Text.Json;
using Chesster.Api.Data;
using Chesster.Api.Models;
using Chesster.Api.Models.DTOs;
using Chesster.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chesster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly ChessterDbContext _context;
    private readonly IAnalysisEngine _analysisEngine;
    private readonly IPgnParserService _pgnParser;

    public AnalysisController(ChessterDbContext context, IAnalysisEngine analysisEngine, IPgnParserService pgnParser)
    {
        _context = context;
        _analysisEngine = analysisEngine;
        _pgnParser = pgnParser;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userId ?? Guid.Empty.ToString());
    }

    [HttpGet("{gameId}")]
    public async Task<ActionResult<List<AnalysisDto>>> GetGameAnalysis(Guid gameId)
    {
        var userId = GetUserId();
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
        
        if (game == null)
            return NotFound(new { message = "Game not found" });

        var analyses = await _context.GameAnalyses
            .Where(a => a.GameId == gameId)
            .OrderBy(a => a.MoveNumber)
            .ThenBy(a => a.Side)
            .ToListAsync();

        var result = analyses.Select(a => new AnalysisDto(
            a.Id,
            a.MoveNumber,
            a.Side,
            a.Fen,
            a.Move,
            JsonSerializer.Deserialize<ChernevExplanation>(a.AnalysisJson) ?? new ChernevExplanation("unknown", "No analysis", new(), new(), "N/A", null, null)
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{gameId}/{moveNumber}")]
    public async Task<ActionResult<AnalysisDto>> GetMoveAnalysis(Guid gameId, int moveNumber)
    {
        var userId = GetUserId();
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
        
        if (game == null)
            return NotFound(new { message = "Game not found" });

        var analysis = await _context.GameAnalyses
            .FirstOrDefaultAsync(a => a.GameId == gameId && a.MoveNumber == moveNumber);

        if (analysis == null)
            return NotFound(new { message = "Analysis not found" });

        return Ok(new AnalysisDto(
            analysis.Id,
            analysis.MoveNumber,
            analysis.Side,
            analysis.Fen,
            analysis.Move,
            JsonSerializer.Deserialize<ChernevExplanation>(analysis.AnalysisJson) ?? new ChernevExplanation("unknown", "No analysis", new(), new(), "N/A", null, null)
        ));
    }

    [HttpPost("{gameId}/analyze")]
    public async Task<ActionResult<List<AnalysisDto>>> AnalyzeGame(Guid gameId)
    {
        try
        {
            var userId = GetUserId();
            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
            
            if (game == null)
                return NotFound(new { message = "Game not found" });

            _context.GameAnalyses.RemoveRange(_context.GameAnalyses.Where(a => a.GameId == gameId));
            await _context.SaveChangesAsync();

            var parsed = _pgnParser.Parse(game.Pgn);
            if (parsed.Moves.Count == 0)
                return Ok(new List<AnalysisDto>()); // Return empty list if no moves

            var newAnalyses = new List<AnalysisDto>();

            foreach (var move in parsed.Moves)
            {
                try
                {
                    var explanation = await _analysisEngine.AnalyzePositionAsync(move.Fen, move.San, move.Side);

                    var analysis = new GameAnalysis
                    {
                        GameId = gameId,
                        MoveNumber = move.MoveNumber,
                        Side = move.Side,
                        Fen = move.Fen,
                        Move = move.San,
                        AnalysisJson = JsonSerializer.Serialize(explanation)
                    };

                    _context.GameAnalyses.Add(analysis);
                    newAnalyses.Add(new AnalysisDto(
                        analysis.Id,
                        analysis.MoveNumber,
                        analysis.Side,
                        analysis.Fen,
                        analysis.Move,
                        explanation
                    ));
                }
                catch (Exception ex)
                {
                    // Log error but continue with other moves
                    System.Diagnostics.Debug.WriteLine($"Error analyzing move {move.San}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(newAnalyses);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Analysis endpoint error: {ex.Message}");
            return StatusCode(500, new { message = $"Analysis failed: {ex.Message}" });
        }
    }
}