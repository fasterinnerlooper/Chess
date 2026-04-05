namespace Chesster.Api.Services;

public class ChessPosition
{
    public string[][] Board { get; set; } = new string[8][];
    public bool WhiteToMove { get; set; } = true;
    public string CastlingRights { get; set; } = "KQkq";
    public string? EnPassantSquare { get; set; }
    public int HalfMoveClock { get; set; }
    public int FullMoveNumber { get; set; } = 1;

    public static ChessPosition FromFen(string fen)
    {
        var parts = fen.Split(' ');
        var position = new ChessPosition();
        
        var rows = parts[0].Split('/');
        for (int i = 0; i < 8; i++)
        {
            var row = rows[i];
            var col = 0;
            var boardRow = new string[8];
            for (int j = 0; j < row.Length && col < 8; j++)
            {
                if (char.IsDigit(row[j]))
                {
                    var empty = row[j] - '0';
                    for (int k = 0; k < empty; k++)
                        boardRow[col++] = null;
                }
                else
                {
                    boardRow[col++] = row[j].ToString();
                }
            }
            position.Board[i] = boardRow;
        }

        position.WhiteToMove = parts.Length > 1 && parts[1] == "w";
        position.CastlingRights = parts.Length > 2 ? parts[2] : "KQkq";
        position.EnPassantSquare = parts.Length > 3 && parts[3] != "-" ? parts[3] : null;
        var halfMove = 0;
        if (parts.Length > 4) int.TryParse(parts[4], out halfMove);
        position.HalfMoveClock = halfMove;
        
        var fullMove = 1;
        if (parts.Length > 5) int.TryParse(parts[5], out fullMove);
        position.FullMoveNumber = fullMove;

        return position;
    }

    public string ToFen()
    {
        var parts = new List<string>();
        
        for (int i = 0; i < 8; i++)
        {
            var row = "";
            var empty = 0;
            for (int j = 0; j < 8; j++)
            {
                if (Board[i][j] == null)
                    empty++;
                else
                {
                    if (empty > 0) { row += empty.ToString(); empty = 0; }
                    row += Board[i][j];
                }
            }
            if (empty > 0) row += empty.ToString();
            parts.Add(row);
        }

        var fen = string.Join("/", parts);
        fen += " " + (WhiteToMove ? "w" : "b");
        fen += " " + CastlingRights;
        fen += " " + (EnPassantSquare ?? "-");
        fen += " " + HalfMoveClock;
        fen += " " + FullMoveNumber;

        return fen;
    }
}

public class Move
{
    public int FromRow { get; set; }
    public int FromCol { get; set; }
    public int ToRow { get; set; }
    public int ToCol { get; set; }
    public string? Promotion { get; set; }
}

public interface IChessService
{
    bool IsValidMove(string fen, string san);
    string SanToUci(string fen, string san);
    string GetFenAfterMove(string fen, string san);
    string GetTurn(string fen);
    int GetMoveNumber(string fen);
}

public class ChessService : IChessService
{
    private static readonly Dictionary<string, (int row, int col)> SquareMap = new();
    static ChessService()
    {
        for (int r = 8; r >= 1; r--)
            for (int c = 0; c < 8; c++)
                SquareMap[((char)('a' + c)).ToString() + r] = (8 - r, c);
    }

    public bool IsValidMove(string fen, string san)
    {
        try
        {
            var position = ChessPosition.FromFen(fen);
            var moves = GetLegalMoves(position);
            
            var cleanSan = san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "").Trim();
            
            if (cleanSan == "O-O" || cleanSan == "0-0")
            {
                var row = position.WhiteToMove ? 7 : 0;
                var kingMove = moves.FirstOrDefault(m => m.FromRow == row && m.FromCol == 4 && m.ToCol == 6);
                return kingMove != null;
            }
            if (cleanSan == "O-O-O" || cleanSan == "0-0-0")
            {
                var row = position.WhiteToMove ? 7 : 0;
                var queenMove = moves.FirstOrDefault(m => m.FromRow == row && m.FromCol == 4 && m.ToCol == 2);
                return queenMove != null;
            }

            // Find the target square from SAN
            var toPos = cleanSan.Length >= 2 ? cleanSan[^2..] : cleanSan;
            if (toPos.Length != 2 || !char.IsLetter(toPos[0]) || !char.IsDigit(toPos[1]))
                return false;
                
            if (!SquareMap.TryGetValue(toPos, out var to)) return false;
            
            // Determine piece type
            var piece = 'P';
            var checkFrom = 0;
            if (char.IsUpper(cleanSan[0]))
            {
                piece = cleanSan[0];
                checkFrom = 1;
            }
            
            // Handle disambiguation (e.g., Nbd7, Qxe5)
            var fromCol = -1;
            var fromRow = -1;
            if (cleanSan.Length > 2 && checkFrom < cleanSan.Length - 2)
            {
                var disambig = cleanSan.Substring(checkFrom, cleanSan.Length - 2 - checkFrom);
                if (disambig.Length == 1 && char.IsLetter(disambig[0]))
                    fromCol = disambig[0] - 'a';
                else if (disambig.Length == 1 && char.IsDigit(disambig[0]))
                    fromRow = 8 - (disambig[0] - '0');
            }
            
            // Find matching move
            var pieceChar = piece;
            var matching = moves.Where(m => 
                m.ToRow == to.row && m.ToCol == to.col &&
                (pieceChar == 'P' || position.Board[m.FromRow][m.FromCol]?.ToUpper()[0] == pieceChar) &&
                (fromCol < 0 || m.FromCol == fromCol) &&
                (fromRow < 0 || m.FromRow == fromRow));
            
            return matching.Any();
        }
        catch { return false; }
    }

    public string SanToUci(string fen, string san)
    {
        var position = ChessPosition.FromFen(fen);
        var cleanSan = san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "").Trim();
        
        if (cleanSan == "O-O") return position.WhiteToMove ? "e1g1" : "e8g8";
        if (cleanSan == "O-O-O") return position.WhiteToMove ? "e1c1" : "e8c8";

        var piece = 'P';
        var capture = cleanSan.Contains('x');
        var fromCol = -1;
        var fromRow = -1;
        var promotion = 'Q';

        if (char.IsUpper(cleanSan[0]))
        {
            piece = cleanSan[0];
            cleanSan = cleanSan[1..];
        }

        if (cleanSan.Contains('='))
        {
            promotion = cleanSan[^1];
            cleanSan = cleanSan[..^2];
        }

        var toPos = cleanSan.Length >= 2 ? cleanSan[^2..] : cleanSan;
        if (toPos.Length == 2 && char.IsLetter(toPos[0]) && char.IsDigit(toPos[1]))
        {
            if (!SquareMap.TryGetValue(toPos, out var to)) return "";
            var toRow = to.row;
            var toCol = to.col;
            
            if (cleanSan.Length == 3 && char.IsLetter(cleanSan[0]))
            {
                fromCol = cleanSan[0] - 'a';
            }
            else if (cleanSan.Length == 4 && char.IsDigit(cleanSan[0]))
            {
                fromRow = int.Parse(cleanSan[0].ToString());
            }

            var moves = GetLegalMoves(position);
            var matching = moves.Where(m => 
                m.ToRow == toRow && m.ToCol == toCol &&
                (piece == 'P' || position.Board[m.FromRow][m.FromCol]?.ToUpper()[0] == piece) &&
                (fromCol < 0 || m.FromCol == fromCol) &&
                (fromRow < 0 || m.FromRow == fromRow));

            var move = matching.FirstOrDefault();
            if (move != null)
            {
                var fromSquare = ((char)('a' + move.FromCol)).ToString() + (8 - move.FromRow);
                var toSquare = ((char)('a' + move.ToCol)).ToString() + (8 - move.ToRow);
                if (promotion != 'Q')
                    return fromSquare + toSquare + promotion.ToString().ToLower();
                return fromSquare + toSquare;
            }
        }

        return "";
    }

    public string GetFenAfterMove(string fen, string san)
    {
        var position = ChessPosition.FromFen(fen);
        var uci = SanToUci(fen, san);
        
        if (string.IsNullOrEmpty(uci)) return fen;
        if (uci.Length < 4) return fen;

        var from = uci[..2];
        var to = uci.Substring(2, 2);
        
        if (!SquareMap.TryGetValue(from, out var fromPos)) return fen;
        if (!SquareMap.TryGetValue(to, out var toPos)) return fen;

        var piece = position.Board[fromPos.row][fromPos.col];
        if (piece == null) return fen;

        position.Board[toPos.row][toPos.col] = piece;
        position.Board[fromPos.row][fromPos.col] = null;

        if (piece.ToLower()[0] == 'p' && (toPos.row == 0 || toPos.row == 7))
        {
            var promo = uci.Length > 4 ? uci[4] : 'q';
            position.Board[toPos.row][toPos.col] = promo.ToString().ToUpper();
        }

        if (piece.ToLower()[0] == 'k' && Math.Abs(toPos.col - fromPos.col) == 2)
        {
            if (toPos.col == 6)
            {
                var rookFrom = position.Board[fromPos.row][7];
                position.Board[fromPos.row][5] = rookFrom;
                position.Board[fromPos.row][7] = null;
            }
            else if (toPos.col == 2)
            {
                var rookFrom = position.Board[fromPos.row][0];
                position.Board[fromPos.row][3] = rookFrom;
                position.Board[fromPos.row][0] = null;
            }
        }

        if (piece.ToLower()[0] == 'p' && Math.Abs(toPos.col - fromPos.col) > 0 && 
            position.Board[toPos.row][toPos.col] == null)
        {
            var captureRow = fromPos.row;
            position.Board[captureRow][toPos.col] = null;
        }

        position.WhiteToMove = !position.WhiteToMove;
        if (!position.WhiteToMove)
            position.FullMoveNumber++;

        return position.ToFen();
    }

    public string GetTurn(string fen)
    {
        var parts = fen.Split(' ');
        return parts.Length > 1 && parts[1] == "w" ? "w" : "b";
    }

    public int GetMoveNumber(string fen)
    {
        var parts = fen.Split(' ');
        return parts.Length > 5 && int.TryParse(parts[5], out var num) ? num : 1;
    }

    private List<Move> GetLegalMoves(ChessPosition pos)
    {
        var moves = new List<Move>();
        var piece = pos.WhiteToMove ? 'w' : 'b';

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var square = pos.Board[r][c];
                if (square == null) continue;
                
                var isWhite = char.IsUpper(square[0]);
                if ((isWhite && piece == 'w') || (!isWhite && piece == 'b'))
                {
                    var p = char.ToLower(square[0]);
                    switch (p)
                    {
                        case 'p': GetPawnMoves(pos, r, c, moves); break;
                        case 'n': GetKnightMoves(pos, r, c, moves); break;
                        case 'b': GetBishopMoves(pos, r, c, moves); break;
                        case 'r': GetRookMoves(pos, r, c, moves); break;
                        case 'q': GetQueenMoves(pos, r, c, moves); break;
                        case 'k': GetKingMoves(pos, r, c, moves); break;
                    }
                }
            }
        }
        return moves;
    }

    private void GetPawnMoves(ChessPosition pos, int row, int col, List<Move> moves)
    {
        var direction = pos.WhiteToMove ? -1 : 1;
        var startRow = pos.WhiteToMove ? 6 : 1;
        var enemyColor = pos.WhiteToMove ? 'b' : 'w';

        if (IsInBounds(row + direction, col) && pos.Board[row + direction][col] == null)
        {
            moves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + direction, ToCol = col });
            if (row == startRow && pos.Board[row + 2 * direction][col] == null)
                moves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + 2 * direction, ToCol = col });
        }

        foreach (var dc in new[] { -1, 1 })
        {
            if (IsInBounds(row + direction, col + dc))
            {
                var target = pos.Board[row + direction][col + dc];
                if (target != null && char.ToLower(target[0]) == enemyColor)
                    moves.Add(new Move { FromRow = row, FromCol = col, ToRow = row + direction, ToCol = col + dc });
            }
        }
    }

    private void GetKnightMoves(ChessPosition pos, int row, int col, List<Move> moves)
    {
        foreach (var (dr, dc) in new[] { (-2,-1), (-2,1), (-1,-2), (-1,2), (1,-2), (1,2), (2,-1), (2,1) })
        {
            var nr = row + dr;
            var nc = col + dc;
            if (IsInBounds(nr, nc) && (pos.Board[nr][nc] == null || IsEnemy(pos, nr, nc)))
                moves.Add(new Move { FromRow = row, FromCol = col, ToRow = nr, ToCol = nc });
        }
    }

    private void GetBishopMoves(ChessPosition pos, int row, int col, List<Move> moves)
        => GetSlidingMoves(pos, row, col, moves, new[] { (-1,-1), (-1,1), (1,-1), (1,1) });

    private void GetRookMoves(ChessPosition pos, int row, int col, List<Move> moves)
        => GetSlidingMoves(pos, row, col, moves, new[] { (-1,0), (1,0), (0,-1), (0,1) });

    private void GetQueenMoves(ChessPosition pos, int row, int col, List<Move> moves)
        => GetSlidingMoves(pos, row, col, moves, new[] { (-1,-1), (-1,1), (1,-1), (1,1), (-1,0), (1,0), (0,-1), (0,1) });

    private void GetSlidingMoves(ChessPosition pos, int row, int col, List<Move> moves, (int dr, int dc)[] directions)
    {
        foreach (var (dr, dc) in directions)
        {
            for (int i = 1; i < 8; i++)
            {
                var nr = row + dr * i;
                var nc = col + dc * i;
                if (!IsInBounds(nr, nc)) break;
                if (pos.Board[nr][nc] != null)
                {
                    if (IsEnemy(pos, nr, nc))
                        moves.Add(new Move { FromRow = row, FromCol = col, ToRow = nr, ToCol = nc });
                    break;
                }
                moves.Add(new Move { FromRow = row, FromCol = col, ToRow = nr, ToCol = nc });
            }
        }
    }

    private void GetKingMoves(ChessPosition pos, int row, int col, List<Move> moves)
    {
        foreach (var (dr, dc) in new[] { (-1,-1), (-1,0), (-1,1), (0,-1), (0,1), (1,-1), (1,0), (1,1) })
        {
            var nr = row + dr;
            var nc = col + dc;
            if (IsInBounds(nr, nc) && (pos.Board[nr][nc] == null || IsEnemy(pos, nr, nc)))
                moves.Add(new Move { FromRow = row, FromCol = col, ToRow = nr, ToCol = nc });
        }
    }

    private bool IsInBounds(int row, int col) => row >= 0 && row < 8 && col >= 0 && col < 8;
    private bool IsEnemy(ChessPosition pos, int row, int col)
    {
        var piece = pos.Board[row][col];
        if (piece == null) return false;
        return pos.WhiteToMove ? char.IsLower(piece[0]) : char.IsUpper(piece[0]);
    }
}