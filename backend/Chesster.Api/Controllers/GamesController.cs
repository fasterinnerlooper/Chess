using System.Security.Claims;
using Chesster.Api.Models.DTOs;
using Chesster.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chesster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userId ?? Guid.Empty.ToString());
    }

    [HttpGet]
    public async Task<ActionResult<List<GameDto>>> GetGames()
    {
        var userId = GetUserId();
        var games = await _gameService.GetUserGamesAsync(userId);
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameDto>> GetGame(Guid id)
    {
        var userId = GetUserId();
        var game = await _gameService.GetGameByIdAsync(id, userId);
        
        if (game == null)
            return NotFound(new { message = "Game not found" });

        return Ok(game);
    }

    [HttpPost]
    public async Task<ActionResult<GameDto>> CreateGame([FromBody] CreateGameRequest request)
    {
        var userId = GetUserId();
        var game = await _gameService.CreateGameAsync(userId, request);
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GameDto>> UpdateGame(Guid id, [FromBody] UpdateGameRequest request)
    {
        var userId = GetUserId();
        var game = await _gameService.UpdateGameAsync(id, userId, request);
        
        if (game == null)
            return NotFound(new { message = "Game not found" });

        return Ok(game);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGame(Guid id)
    {
        var userId = GetUserId();
        var result = await _gameService.DeleteGameAsync(id, userId);
        
        if (!result)
            return NotFound(new { message = "Game not found" });

        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<GameDto>> ImportPgn([FromBody] ImportPgnRequest request)
    {
        var userId = GetUserId();
        var game = await _gameService.ImportPgnAsync(userId, request.Pgn);
        return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
    }
}

public record ImportPgnRequest(string Pgn);