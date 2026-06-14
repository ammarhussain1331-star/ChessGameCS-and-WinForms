using System;
using System.Collections.Generic;

namespace ChessGame
{
    public struct ChessMove
    {
        public int StartRow { get; }
        public int StartCol { get; }
        public int EndRow { get; }
        public int EndCol { get; }
        public char Piece { get; }
        public char Captured { get; }
        public bool IsCastling { get; }
        public bool IsEnPassant { get; }
        public char Promotion { get; }
        public string Algebraic { get; set; }

        public ChessMove(int sr, int sc, int er, int ec, char piece, char captured, bool isCastling = false, bool isEnPassant = false, char promotion = '.')
        {
            StartRow = sr;
            StartCol = sc;
            EndRow = er;
            EndCol = ec;
            Piece = piece;
            Captured = captured;
            IsCastling = isCastling;
            IsEnPassant = isEnPassant;
            Promotion = promotion;
            Algebraic = "";
        }
    }

    public class BoardState
    {
        public char[,] Board { get; } = new char[8, 8];
        public bool WhiteTurn { get; set; }
        public bool WhiteCanCastleKingside { get; set; }
        public bool WhiteCanCastleQueenside { get; set; }
        public bool BlackCanCastleKingside { get; set; }
        public bool BlackCanCastleQueenside { get; set; }
        public (int r, int c)? EnPassantSquare { get; set; }
        public string LastMoveAlgebraic { get; set; }

        public BoardState Clone()
        {
            var clone = new BoardState
            {
                WhiteTurn = this.WhiteTurn,
                WhiteCanCastleKingside = this.WhiteCanCastleKingside,
                WhiteCanCastleQueenside = this.WhiteCanCastleQueenside,
                BlackCanCastleKingside = this.BlackCanCastleKingside,
                BlackCanCastleQueenside = this.BlackCanCastleQueenside,
                EnPassantSquare = this.EnPassantSquare,
                LastMoveAlgebraic = this.LastMoveAlgebraic
            };
            Array.Copy(this.Board, clone.Board, this.Board.Length);
            return clone;
        }
    }

    public class ChessEngine
    {
        public BoardState CurrentState { get; private set; }
        private readonly Stack<BoardState> _history = new Stack<BoardState>();

        public char[,] Board => CurrentState.Board;
        public bool WhiteTurn => CurrentState.WhiteTurn;
        public (int r, int c)? EnPassantSquare => CurrentState.EnPassantSquare;
        public List<ChessMove> MoveHistoryLog { get; } = new List<ChessMove>();

        public ChessEngine()
        {
            CurrentState = new BoardState();
            Reset();
        }

        public void Reset()
        {
            CurrentState.WhiteTurn = true;
            CurrentState.WhiteCanCastleKingside = true;
            CurrentState.WhiteCanCastleQueenside = true;
            CurrentState.BlackCanCastleKingside = true;
            CurrentState.BlackCanCastleQueenside = true;
            CurrentState.EnPassantSquare = null;
            CurrentState.LastMoveAlgebraic = "";
            _history.Clear();
            MoveHistoryLog.Clear();

            // Set up initial board
            string[] setup = {
                "rnbqkbnr",
                "pppppppp",
                "........",
                "........",
                "........",
                "........",
                "PPPPPPPP",
                "RNBQKBNR"
            };

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    CurrentState.Board[r, c] = setup[r][c];
                }
            }
        }

        public bool IsWhite(char p) => char.IsUpper(p) && p != '.';
        public bool IsBlack(char p) => char.IsLower(p);

        public bool MakeMove(ChessMove move)
        {
            // Push current state to history
            _history.Push(CurrentState.Clone());

            int sr = move.StartRow;
            int sc = move.StartCol;
            int er = move.EndRow;
            int ec = move.EndCol;
            char p = Board[sr, sc];

            // Compute algebraic notation before board updates
            string algebraic = GetMoveAlgebraic(move);
            move.Algebraic = algebraic;

            // Execute move
            if (move.IsCastling)
            {
                // King moves
                Board[er, ec] = p;
                Board[sr, sc] = '.';

                // Rook moves
                if (ec == 6) // Kingside
                {
                    Board[er, 5] = Board[er, 7];
                    Board[er, 7] = '.';
                }
                else if (ec == 2) // Queenside
                {
                    Board[er, 3] = Board[er, 0];
                    Board[er, 0] = '.';
                }
            }
            else if (move.IsEnPassant)
            {
                Board[er, ec] = p;
                Board[sr, sc] = '.';
                // Captured pawn row
                int captureRow = sr; 
                Board[captureRow, ec] = '.';
            }
            else
            {
                char pieceToPlace = move.Promotion != '.' ? move.Promotion : p;
                Board[er, ec] = pieceToPlace;
                Board[sr, sc] = '.';
            }

            // Update castling rights
            if (p == 'K')
            {
                CurrentState.WhiteCanCastleKingside = false;
                CurrentState.WhiteCanCastleQueenside = false;
            }
            else if (p == 'k')
            {
                CurrentState.BlackCanCastleKingside = false;
                CurrentState.BlackCanCastleQueenside = false;
            }
            else if (p == 'R')
            {
                if (sr == 7 && sc == 7) CurrentState.WhiteCanCastleKingside = false;
                if (sr == 7 && sc == 0) CurrentState.WhiteCanCastleQueenside = false;
            }
            else if (p == 'r')
            {
                if (sr == 0 && sc == 7) CurrentState.BlackCanCastleKingside = false;
                if (sr == 0 && sc == 0) CurrentState.BlackCanCastleQueenside = false;
            }

            // Rook captured updates castling rights
            if (er == 7 && ec == 7) CurrentState.WhiteCanCastleKingside = false;
            if (er == 7 && ec == 0) CurrentState.WhiteCanCastleQueenside = false;
            if (er == 0 && ec == 7) CurrentState.BlackCanCastleKingside = false;
            if (er == 0 && ec == 0) CurrentState.BlackCanCastleQueenside = false;

            // Update En Passant square
            if (char.ToLower(p) == 'p' && Math.Abs(er - sr) == 2)
            {
                CurrentState.EnPassantSquare = ((sr + er) / 2, sc);
            }
            else
            {
                CurrentState.EnPassantSquare = null;
            }

            // Toggle Turn
            CurrentState.WhiteTurn = !CurrentState.WhiteTurn;
            CurrentState.LastMoveAlgebraic = algebraic;

            MoveHistoryLog.Add(move);
            return true;
        }

        public bool UndoMove()
        {
            if (_history.Count == 0) return false;

            CurrentState = _history.Pop();
            if (MoveHistoryLog.Count > 0)
            {
                MoveHistoryLog.RemoveAt(MoveHistoryLog.Count - 1);
            }
            return true;
        }

        public List<ChessMove> GetLegalMoves(bool white)
        {
            var pseudo = GetPseudoLegalMoves(white);
            var legal = new List<ChessMove>();

            foreach (var m in pseudo)
            {
                // Simulate move
                var tempState = CurrentState.Clone();
                ExecuteMoveOnBoard(m, tempState.Board);

                // If our king is not under attack after the move, it's legal
                if (!IsKingAttacked(white, tempState.Board))
                {
                    legal.Add(m);
                }
            }

            return legal;
        }

        private void ExecuteMoveOnBoard(ChessMove m, char[,] board)
        {
            char p = board[m.StartRow, m.StartCol];
            if (m.IsCastling)
            {
                board[m.EndRow, m.EndCol] = p;
                board[m.StartRow, m.StartCol] = '.';
                if (m.EndCol == 6)
                {
                    board[m.EndRow, 5] = board[m.EndRow, 7];
                    board[m.EndRow, 7] = '.';
                }
                else
                {
                    board[m.EndRow, 3] = board[m.EndRow, 0];
                    board[m.EndRow, 0] = '.';
                }
            }
            else if (m.IsEnPassant)
            {
                board[m.EndRow, m.EndCol] = p;
                board[m.StartRow, m.StartCol] = '.';
                board[m.StartRow, m.EndCol] = '.';
            }
            else
            {
                board[m.EndRow, m.EndCol] = m.Promotion != '.' ? m.Promotion : p;
                board[m.StartRow, m.StartCol] = '.';
            }
        }

        public bool IsInCheck(bool white)
        {
            return IsKingAttacked(white, Board);
        }

        public bool IsCheckmate(bool white)
        {
            if (!IsInCheck(white)) return false;
            return GetLegalMoves(white).Count == 0;
        }

        public bool IsStalemate(bool white)
        {
            if (IsInCheck(white)) return false;
            return GetLegalMoves(white).Count == 0;
        }

        private bool IsKingAttacked(bool white, char[,] board)
        {
            // Find king
            char king = white ? 'K' : 'k';
            int kr = -1, kc = -1;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (board[r, c] == king)
                    {
                        kr = r;
                        kc = c;
                        break;
                    }
                }
                if (kr != -1) break;
            }

            if (kr == -1) return true; // King captured (should not happen in real chess but safety fallback)

            return IsSquareAttacked(kr, kc, !white, board);
        }

        public bool IsSquareAttacked(int r, int c, bool byWhite, char[,] board)
        {
            // Check attacks from pawns
            int pDir = byWhite ? 1 : -1; // Opponent pawn direction to reach (r, c)
            char opponentPawn = byWhite ? 'P' : 'p';
            int pr1 = r + pDir;
            if (pr1 >= 0 && pr1 < 8)
            {
                if (c - 1 >= 0 && board[pr1, c - 1] == opponentPawn) return true;
                if (c + 1 < 8 && board[pr1, c + 1] == opponentPawn) return true;
            }

            // Check attacks from knights
            char opponentKnight = byWhite ? 'N' : 'n';
            int[] knr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] knc = { -1, 1, -2, 2, -2, 2, -1, 1 };
            for (int i = 0; i < 8; i++)
            {
                int nr = r + knr[i];
                int nc = c + knc[i];
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    if (board[nr, nc] == opponentKnight) return true;
                }
            }

            // Check sliding attacks (Rooks, Queens)
            char opponentRook = byWhite ? 'R' : 'r';
            char opponentQueen = byWhite ? 'Q' : 'q';
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char piece = board[nr, nc];
                    if (piece != '.')
                    {
                        if (piece == opponentRook || piece == opponentQueen) return true;
                        break; // Blocked
                    }
                    nr += dr[i];
                    nc += dc[i];
                }
            }

            // Check sliding attacks (Bishops, Queens)
            char opponentBishop = byWhite ? 'B' : 'b';
            int[] dbr = { -1, -1, 1, 1 };
            int[] dbc = { -1, 1, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nr = r + dbr[i];
                int nc = c + dbc[i];
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char piece = board[nr, nc];
                    if (piece != '.')
                    {
                        if (piece == opponentBishop || piece == opponentQueen) return true;
                        break; // Blocked
                    }
                    nr += dbr[i];
                    nc += dbc[i];
                }
            }

            // Check attacks from king (for proximity)
            char opponentKing = byWhite ? 'K' : 'k';
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int nr = r + i;
                    int nc = c + j;
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        if (board[nr, nc] == opponentKing) return true;
                    }
                }
            }

            return false;
        }

        private List<ChessMove> GetPseudoLegalMoves(bool white)
        {
            var moves = new List<ChessMove>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char p = Board[r, c];
                    if (p == '.' || IsWhite(p) != white) continue;

                    switch (char.ToLower(p))
                    {
                        case 'p': AddPawnMoves(r, c, moves); break;
                        case 'r': AddRookMoves(r, c, moves); break;
                        case 'n': AddKnightMoves(r, c, moves); break;
                        case 'b': AddBishopMoves(r, c, moves); break;
                        case 'q': AddQueenMoves(r, c, moves); break;
                        case 'k': AddKingMoves(r, c, moves); break;
                    }
                }
            }

            return moves;
        }

        private void AddPawnMoves(int r, int c, List<ChessMove> moves)
        {
            char p = Board[r, c];
            bool white = IsWhite(p);
            int dir = white ? -1 : 1;
            int startRow = white ? 6 : 1;
            int promoRow = white ? 0 : 7;

            // Single forward step
            int nextRow = r + dir;
            if (nextRow >= 0 && nextRow < 8 && Board[nextRow, c] == '.')
            {
                if (nextRow == promoRow)
                {
                    // Add all promotion options
                    char[] promos = white ? new[] { 'Q', 'R', 'B', 'N' } : new[] { 'q', 'r', 'b', 'n' };
                    foreach (char pr in promos)
                        moves.Add(new ChessMove(r, c, nextRow, c, p, '.', promotion: pr));
                }
                else
                {
                    moves.Add(new ChessMove(r, c, nextRow, c, p, '.'));
                }

                // Double forward step
                int doubleRow = r + 2 * dir;
                if (r == startRow && Board[doubleRow, c] == '.')
                {
                    moves.Add(new ChessMove(r, c, doubleRow, c, p, '.'));
                }
            }

            // Diagonal captures (including en passant)
            int[] cols = { c - 1, c + 1 };
            foreach (int tc in cols)
            {
                if (tc < 0 || tc >= 8) continue;

                char target = Board[nextRow, tc];
                if (target != '.' && IsWhite(target) != white)
                {
                    if (nextRow == promoRow)
                    {
                        char[] promos = white ? new[] { 'Q', 'R', 'B', 'N' } : new[] { 'q', 'r', 'b', 'n' };
                        foreach (char pr in promos)
                            moves.Add(new ChessMove(r, c, nextRow, tc, p, target, promotion: pr));
                    }
                    else
                    {
                        moves.Add(new ChessMove(r, c, nextRow, tc, p, target));
                    }
                }

                // En Passant capture
                if (EnPassantSquare.HasValue && EnPassantSquare.Value.r == nextRow && EnPassantSquare.Value.c == tc)
                {
                    char epTarget = Board[r, tc]; // The pawn being captured
                    moves.Add(new ChessMove(r, c, nextRow, tc, p, epTarget, isEnPassant: true));
                }
            }
        }

        private void AddRookMoves(int r, int c, List<ChessMove> moves)
        {
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            AddSlidingMoves(r, c, dr, dc, moves);
        }

        private void AddBishopMoves(int r, int c, List<ChessMove> moves)
        {
            int[] dr = { -1, -1, 1, 1 };
            int[] dc = { -1, 1, -1, 1 };
            AddSlidingMoves(r, c, dr, dc, moves);
        }

        private void AddQueenMoves(int r, int c, List<ChessMove> moves)
        {
            int[] dr = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dc = { 0, 0, -1, 1, -1, 1, -1, 1 };
            AddSlidingMoves(r, c, dr, dc, moves);
        }

        private void AddSlidingMoves(int r, int c, int[] dr, int[] dc, List<ChessMove> moves)
        {
            char p = Board[r, c];
            bool white = IsWhite(p);

            for (int i = 0; i < dr.Length; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];

                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char target = Board[nr, nc];
                    if (target == '.')
                    {
                        moves.Add(new ChessMove(r, c, nr, nc, p, '.'));
                    }
                    else
                    {
                        if (IsWhite(target) != white)
                        {
                            moves.Add(new ChessMove(r, c, nr, nc, p, target));
                        }
                        break; // Blocked by piece
                    }
                    nr += dr[i];
                    nc += dc[i];
                }
            }
        }

        private void AddKnightMoves(int r, int c, List<ChessMove> moves)
        {
            char p = Board[r, c];
            bool white = IsWhite(p);

            int[] knr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] knc = { -1, 1, -2, 2, -2, 2, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nr = r + knr[i];
                int nc = c + knc[i];

                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char target = Board[nr, nc];
                    if (target == '.' || IsWhite(target) != white)
                    {
                        moves.Add(new ChessMove(r, c, nr, nc, p, target));
                    }
                }
            }
        }

        private void AddKingMoves(int r, int c, List<ChessMove> moves)
        {
            char p = Board[r, c];
            bool white = IsWhite(p);

            // Normal moves
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int nr = r + i;
                    int nc = c + j;

                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        char target = Board[nr, nc];
                        if (target == '.' || IsWhite(target) != white)
                        {
                            moves.Add(new ChessMove(r, c, nr, nc, p, target));
                        }
                    }
                }
            }

            // Castling
            if (white)
            {
                if (CurrentState.WhiteCanCastleKingside)
                {
                    if (Board[7, 5] == '.' && Board[7, 6] == '.' && Board[7, 7] == 'R')
                    {
                        if (!IsSquareAttacked(7, 4, false, Board) &&
                            !IsSquareAttacked(7, 5, false, Board) &&
                            !IsSquareAttacked(7, 6, false, Board))
                        {
                            moves.Add(new ChessMove(7, 4, 7, 6, p, '.', isCastling: true));
                        }
                    }
                }
                if (CurrentState.WhiteCanCastleQueenside)
                {
                    if (Board[7, 1] == '.' && Board[7, 2] == '.' && Board[7, 3] == '.' && Board[7, 0] == 'R')
                    {
                        if (!IsSquareAttacked(7, 4, false, Board) &&
                            !IsSquareAttacked(7, 3, false, Board) &&
                            !IsSquareAttacked(7, 2, false, Board))
                        {
                            moves.Add(new ChessMove(7, 4, 7, 2, p, '.', isCastling: true));
                        }
                    }
                }
            }
            else
            {
                if (CurrentState.BlackCanCastleKingside)
                {
                    if (Board[0, 5] == '.' && Board[0, 6] == '.' && Board[0, 7] == 'r')
                    {
                        if (!IsSquareAttacked(0, 4, true, Board) &&
                            !IsSquareAttacked(0, 5, true, Board) &&
                            !IsSquareAttacked(0, 6, true, Board))
                        {
                            moves.Add(new ChessMove(0, 4, 0, 6, p, '.', isCastling: true));
                        }
                    }
                }
                if (CurrentState.BlackCanCastleQueenside)
                {
                    if (Board[0, 1] == '.' && Board[0, 2] == '.' && Board[0, 3] == '.' && Board[0, 0] == 'r')
                    {
                        if (!IsSquareAttacked(0, 4, true, Board) &&
                            !IsSquareAttacked(0, 3, true, Board) &&
                            !IsSquareAttacked(0, 2, true, Board))
                        {
                            moves.Add(new ChessMove(0, 4, 0, 2, p, '.', isCastling: true));
                        }
                    }
                }
            }
        }

        private string GetMoveAlgebraic(ChessMove move)
        {
            if (move.IsCastling)
            {
                return move.EndCol == 6 ? "O-O" : "O-O-O";
            }

            string pLetter = "";
            char pl = char.ToLower(move.Piece);
            if (pl != 'p')
            {
                pLetter = char.ToUpper(pl).ToString();
            }

            string startSquare = $"{(char)('a' + move.StartCol)}{8 - move.StartRow}";
            string endSquare = $"{(char)('a' + move.EndCol)}{8 - move.EndRow}";
            string captureIndicator = move.Captured != '.' || move.IsEnPassant ? "x" : "";

            // For pawns, if capture, include start file
            if (pl == 'p' && (move.Captured != '.' || move.IsEnPassant))
            {
                pLetter = ((char)('a' + move.StartCol)).ToString();
            }

            string promoIndicator = move.Promotion != '.' ? "=" + char.ToUpper(move.Promotion) : "";

            // Simulate the move to check if it results in check or checkmate
            var tempState = CurrentState.Clone();
            ExecuteMoveOnBoard(move, tempState.Board);
            bool check = IsKingAttacked(!CurrentState.WhiteTurn, tempState.Board);
            string checkIndicator = "";
            if (check)
            {
                // Check if checkmate
                bool checkmate = true;
                // Generate opponent legal moves
                var pseudoOpponent = new List<ChessMove>();
                bool oppColor = !CurrentState.WhiteTurn;
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        char piece = tempState.Board[r, c];
                        if (piece == '.' || IsWhite(piece) != oppColor) continue;

                        switch (char.ToLower(piece))
                        {
                            case 'p': AddPawnMovesTemp(r, c, pseudoOpponent, tempState.Board, tempState.EnPassantSquare); break;
                            case 'r': AddRookMovesTemp(r, c, pseudoOpponent, tempState.Board); break;
                            case 'n': AddKnightMovesTemp(r, c, pseudoOpponent, tempState.Board); break;
                            case 'b': AddBishopMovesTemp(r, c, pseudoOpponent, tempState.Board); break;
                            case 'q': AddQueenMovesTemp(r, c, pseudoOpponent, tempState.Board); break;
                            case 'k': AddKingMovesTemp(r, c, pseudoOpponent, tempState.Board, tempState); break;
                        }
                    }
                }

                foreach (var om in pseudoOpponent)
                {
                    var doubleTemp = tempState.Clone();
                    ExecuteMoveOnBoard(om, doubleTemp.Board);
                    if (!IsKingAttacked(oppColor, doubleTemp.Board))
                    {
                        checkmate = false;
                        break;
                    }
                }

                checkIndicator = checkmate ? "#" : "+";
            }

            return $"{pLetter}{captureIndicator}{endSquare}{promoIndicator}{checkIndicator}";
        }

        // --- Helper Temp Methods for quick simulations (avoiding infinite loops of CurrentState calls) ---
        private void AddPawnMovesTemp(int r, int c, List<ChessMove> moves, char[,] board, (int r, int c)? epSquare)
        {
            char p = board[r, c];
            bool white = IsWhite(p);
            int dir = white ? -1 : 1;
            int startRow = white ? 6 : 1;
            int promoRow = white ? 0 : 7;

            int nextRow = r + dir;
            if (nextRow >= 0 && nextRow < 8 && board[nextRow, c] == '.')
            {
                if (nextRow == promoRow)
                {
                    char[] promos = white ? new[] { 'Q', 'R', 'B', 'N' } : new[] { 'q', 'r', 'b', 'n' };
                    foreach (char pr in promos) moves.Add(new ChessMove(r, c, nextRow, c, p, '.', promotion: pr));
                }
                else moves.Add(new ChessMove(r, c, nextRow, c, p, '.'));

                int doubleRow = r + 2 * dir;
                if (r == startRow && board[doubleRow, c] == '.')
                {
                    moves.Add(new ChessMove(r, c, doubleRow, c, p, '.'));
                }
            }

            int[] cols = { c - 1, c + 1 };
            foreach (int tc in cols)
            {
                if (tc < 0 || tc >= 8) continue;
                char target = board[nextRow, tc];
                if (target != '.' && IsWhite(target) != white)
                {
                    if (nextRow == promoRow)
                    {
                        char[] promos = white ? new[] { 'Q', 'R', 'B', 'N' } : new[] { 'q', 'r', 'b', 'n' };
                        foreach (char pr in promos) moves.Add(new ChessMove(r, c, nextRow, tc, p, target, promotion: pr));
                    }
                    else moves.Add(new ChessMove(r, c, nextRow, tc, p, target));
                }
                if (epSquare.HasValue && epSquare.Value.r == nextRow && epSquare.Value.c == tc)
                {
                    moves.Add(new ChessMove(r, c, nextRow, tc, p, board[r, tc], isEnPassant: true));
                }
            }
        }

        private void AddRookMovesTemp(int r, int c, List<ChessMove> moves, char[,] board) => AddSlidingMovesTemp(r, c, new[] { -1, 1, 0, 0 }, new[] { 0, 0, -1, 1 }, moves, board);
        private void AddBishopMovesTemp(int r, int c, List<ChessMove> moves, char[,] board) => AddSlidingMovesTemp(r, c, new[] { -1, -1, 1, 1 }, new[] { -1, 1, -1, 1 }, moves, board);
        private void AddQueenMovesTemp(int r, int c, List<ChessMove> moves, char[,] board) => AddSlidingMovesTemp(r, c, new[] { -1, 1, 0, 0, -1, -1, 1, 1 }, new[] { 0, 0, -1, 1, -1, 1, -1, 1 }, moves, board);

        private void AddSlidingMovesTemp(int r, int c, int[] dr, int[] dc, List<ChessMove> moves, char[,] board)
        {
            char p = board[r, c];
            bool white = IsWhite(p);
            for (int i = 0; i < dr.Length; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char target = board[nr, nc];
                    if (target == '.') moves.Add(new ChessMove(r, c, nr, nc, p, '.'));
                    else
                    {
                        if (IsWhite(target) != white) moves.Add(new ChessMove(r, c, nr, nc, p, target));
                        break;
                    }
                    nr += dr[i]; nc += dc[i];
                }
            }
        }

        private void AddKnightMovesTemp(int r, int c, List<ChessMove> moves, char[,] board)
        {
            char p = board[r, c];
            bool white = IsWhite(p);
            int[] knr = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] knc = { -1, 1, -2, 2, -2, 2, -1, 1 };
            for (int i = 0; i < 8; i++)
            {
                int nr = r + knr[i];
                int nc = c + knc[i];
                if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                {
                    char target = board[nr, nc];
                    if (target == '.' || IsWhite(target) != white) moves.Add(new ChessMove(r, c, nr, nc, p, target));
                }
            }
        }

        private void AddKingMovesTemp(int r, int c, List<ChessMove> moves, char[,] board, BoardState state)
        {
            char p = board[r, c];
            bool white = IsWhite(p);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int nr = r + i;
                    int nc = c + j;
                    if (nr >= 0 && nr < 8 && nc >= 0 && nc < 8)
                    {
                        char target = board[nr, nc];
                        if (target == '.' || IsWhite(target) != white) moves.Add(new ChessMove(r, c, nr, nc, p, target));
                    }
                }
            }
            // Ignore castling inside temporary move generator for checkmate checks to prevent recursion
        }
    }
}
