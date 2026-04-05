using System.Text.Json;
using Chesster.Api.Data;
using Chesster.Api.Models;
using Chesster.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Chesster.Api.Services;

public interface IGameService
{
    Task<List<GameDto>> GetUserGamesAsync(Guid userId);
    Task<GameDto?> GetGameByIdAsync(Guid gameId, Guid userId);
    Task<GameDto> CreateGameAsync(Guid userId, CreateGameRequest request);
    Task<GameDto?> UpdateGameAsync(Guid gameId, Guid userId, UpdateGameRequest request);
    Task<bool> DeleteGameAsync(Guid gameId, Guid userId);
    Task<GameDto> ImportPgnAsync(Guid userId, string pgn);
}

public class GameService : IGameService
{
    private readonly ChessterDbContext _context;
    private readonly IPgnParserService _pgnParser;
    private readonly IAnalysisEngine _analysisEngine;

    public GameService(ChessterDbContext context, IPgnParserService pgnParser, IAnalysisEngine analysisEngine)
    {
        _context = context;
        _pgnParser = pgnParser;
        _analysisEngine = analysisEngine;
    }

    public async Task<List<GameDto>> GetUserGamesAsync(Guid userId)
    {
        return await _context.Games
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => MapToDto(g))
            .ToListAsync();
    }

    public async Task<GameDto?> GetGameByIdAsync(Guid gameId, Guid userId)
    {
        var game = await _context.Games
            .Include(g => g.Analyses)
            .FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);

        return game == null ? null : MapToDto(game);
    }

    public async Task<GameDto> CreateGameAsync(Guid userId, CreateGameRequest request)
    {
        var game = new Game
        {
            UserId = userId,
            Pgn = request.Pgn,
            White = request.White,
            Black = request.Black,
            Result = request.Result,
            Event = request.Event,
            Site = request.Site,
            Date = request.Date
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        return MapToDto(game);
    }

    public async Task<GameDto?> UpdateGameAsync(Guid gameId, Guid userId, UpdateGameRequest request)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
        if (game == null) return null;

        if (request.Pgn != null) game.Pgn = request.Pgn;
        if (request.White != null) game.White = request.White;
        if (request.Black != null) game.Black = request.Black;
        if (request.Result != null) game.Result = request.Result;
        if (request.Event != null) game.Event = request.Event;
        if (request.Site != null) game.Site = request.Site;
        if (request.Date.HasValue) game.Date = request.Date;

        await _context.SaveChangesAsync();
        return MapToDto(game);
    }

    public async Task<bool> DeleteGameAsync(Guid gameId, Guid userId)
    {
        var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.UserId == userId);
        if (game == null) return false;

        _context.Games.Remove(game);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<GameDto> ImportPgnAsync(Guid userId, string pgn)
    {
        var parsed = _pgnParser.Parse(pgn);

        var game = new Game
        {
            UserId = userId,
            Pgn = pgn,
            White = parsed.White,
            Black = parsed.Black,
            Result = parsed.Result,
            Event = parsed.Event,
            Site = parsed.Site,
            Date = parsed.Date
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        return MapToDto(game);
    }

    private static GameDto MapToDto(Game game)
    {
        var analyses = game.Analyses?.Select(a => new AnalysisDto(
            a.Id,
            a.MoveNumber,
            a.Side,
            a.Fen,
            a.Move,
            JsonSerializer.Deserialize<ChernevExplanation>(a.AnalysisJson) ?? new ChernevExplanation("unknown", "No analysis", new(), new(), "N/A", null, null)
        )).ToList();

        return new GameDto(
            game.Id,
            game.Pgn,
            game.White,
            game.Black,
            game.Result,
            game.Event,
            game.Site,
            game.Date,
            game.CreatedAt,
            analyses
        );
    }
}