using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chesster.Api.Models;

public class Game
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public string Pgn { get; set; } = string.Empty;

    [MaxLength(100)]
    public string White { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Black { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Result { get; set; } = "*";

    [MaxLength(255)]
    public string? Event { get; set; }

    [MaxLength(255)]
    public string? Site { get; set; }

    public DateTime? Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GameAnalysis> Analyses { get; set; } = new List<GameAnalysis>();
}

public class GameAnalysis
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid GameId { get; set; }

    [ForeignKey(nameof(GameId))]
    public Game Game { get; set; } = null!;

    [Required]
    public int MoveNumber { get; set; }

    [Required]
    [MaxLength(1)]
    public string Side { get; set; } = string.Empty;

    [Required]
    public string Fen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Move { get; set; } = string.Empty;

    [Required]
    public string AnalysisJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}