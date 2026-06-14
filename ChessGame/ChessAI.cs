using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessGame
{
    public class ChessAI
    {
        // Piece values
        private const int PAWN_VAL = 100;
        private const int KNIGHT_VAL = 320;
        private const int BISHOP_VAL = 330;
        private const int ROOK_VAL = 500;
        private const int QUEEN_VAL = 900;
        private const int KING_VAL = 20000;

        // Piece-Square Tables (PST)
        // Values from White's perspective (index 0 is row 0/black side, index 7 is row 7/white side)
        // Flip row indices for Black's perspective.
        
        private static readonly int[,] PawnPst = {
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            {  5,  5, 10, 25, 25, 10,  5,  5 },
            {  0,  0,  0, 20, 20,  0,  0,  0 },
            {  5, -5,-10,  0,  0,-10, -5,  5 },
            {  5, 10, 10,-20,-20, 10, 10,  5 },
            {  0,  0,  0,  0,  0,  0,  0,  0 }
        };

        private static readonly int[,] KnightPst = {
            { -50,-40,-30,-30,-30,-30,-40,-50 },
            { -40,-20,  0,  0,  0,  0,-20,-40 },
            { -30,  0, 10, 15, 15, 10,  0,-30 },
            { -30,  5, 15, 20, 20, 15,  5,-30 },
            { -30,  0, 15, 20, 20, 15,  0,-30 },
            { -30,  5, 10, 15, 15, 10,  5,-30 },
            { -40,-20,  0,  5,  5,  0,-20,-40 },
            { -50,-40,-30,-30,-30,-30,-40,-50 }
        };

        private static readonly int[,] BishopPst = {
            { -20,-10,-10,-10,-10,-10,-10,-20 },
            { -10,  0,  0,  0,  0,  0,  0,-10 },
            { -10,  0,  5, 10, 10,  5,  0,-10 },
            { -10,  5,  5, 10, 10,  5,  5,-10 },
            { -10,  0, 10, 10, 10, 10,  0,-10 },
            { -10, 10, 10, 10, 10, 10, 10,-10 },
            { -10,  5,  0,  0,  0,  0,  5,-10 },
            { -20,-10,-10,-10,-10,-10,-10,-20 }
        };

        private static readonly int[,] RookPst = {
            {  0,  0,  0,  0,  0,  0,  0,  0 },
            {  5, 10, 10, 10, 10, 10, 10,  5 },
            { -5,  0,  0,  0,  0,  0,  0, -5 },
            { -5,  0,  0,  0,  0,  0,  0, -5 },
            { -5,  0,  0,  0,  0,  0,  0, -5 },
            { -5,  0,  0,  0,  0,  0,  0, -5 },
            { -5,  0,  0,  0,  0,  0,  0, -5 },
            {  0,  0,  0,  5,  5,  0,  0,  0 }
        };

        private static readonly int[,] QueenPst = {
            { -20,-10,-10, -5, -5,-10,-10,-20 },
            { -10,  0,  0,  0,  0,  0,  0,-10 },
            { -10,  0,  5,  5,  5,  5,  0,-10 },
            {  -5,  0,  5,  5,  5,  5,  0, -5 },
            {   0,  0,  5,  5,  5,  5,  0,  0 },
            { -10,  5,  5,  5,  5,  5,  5,-10 },
            { -10,  0,  5,  0,  0,  5,  0,-10 },
            { -20,-10,-10, -5, -5,-10,-10,-20 }
        };

        private static readonly int[,] KingMiddlePst = {
            { -30,-40,-40,-50,-50,-40,-40,-30 },
            { -30,-40,-40,-50,-50,-40,-40,-30 },
            { -30,-40,-40,-50,-50,-40,-40,-30 },
            { -30,-40,-40,-50,-50,-40,-40,-30 },
            { -20,-30,-30,-40,-40,-30,-30,-20 },
            { -10,-20,-20,-20,-20,-20,-20,-10 },
            {  20, 20,  0,  0,  0,  0, 20, 20 },
            {  20, 30, 10,  0,  0, 10, 30, 20 }
        };

        public static Task<ChessMove?> GetBestMoveAsync(ChessEngine engine, int depth)
        {
            return Task.Run(() =>
            {
                var ai = new ChessAI();
                return ai.GetBestMove(engine, depth);
            });
        }

        private ChessMove? GetBestMove(ChessEngine engine, int depth)
        {
            bool isWhite = engine.WhiteTurn;
            var moves = engine.GetLegalMoves(isWhite);
            if (moves.Count == 0) return null;

            // Sort moves to speed up pruning
            OrderMoves(moves, engine.Board);

            ChessMove? bestMove = null;
            int bestScore = isWhite ? -9999999 : 9999999;

            foreach (var m in moves)
            {
                var tempEngine = new ChessEngine();
                // Load current state into temporary engine
                tempEngine.Reset();
                CopyState(engine.CurrentState, tempEngine.CurrentState);

                tempEngine.MakeMove(m);

                int score = Minimax(tempEngine, depth - 1, -9999999, 9999999, !isWhite);

                if (isWhite)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = m;
                    }
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = m;
                    }
                }
            }

            return bestMove;
        }

        private int Minimax(ChessEngine engine, int depth, int alpha, int beta, bool maximizing)
        {
            if (depth == 0)
            {
                return EvaluateBoard(engine.Board);
            }

            var moves = engine.GetLegalMoves(maximizing);

            if (moves.Count == 0)
            {
                if (engine.IsInCheck(maximizing))
                {
                    // Checkmate: return huge negative value if maximizing, huge positive if minimizing
                    return maximizing ? -1000000 - depth : 1000000 + depth;
                }
                else
                {
                    // Stalemate
                    return 0;
                }
            }

            OrderMoves(moves, engine.Board);

            if (maximizing)
            {
                int maxEval = -9999999;
                foreach (var m in moves)
                {
                    var tempEngine = new ChessEngine();
                    tempEngine.Reset();
                    CopyState(engine.CurrentState, tempEngine.CurrentState);

                    tempEngine.MakeMove(m);
                    int eval = Minimax(tempEngine, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha) break; // Beta cutoff
                }
                return maxEval;
            }
            else
            {
                int minEval = 9999999;
                foreach (var m in moves)
                {
                    var tempEngine = new ChessEngine();
                    tempEngine.Reset();
                    CopyState(engine.CurrentState, tempEngine.CurrentState);

                    tempEngine.MakeMove(m);
                    int eval = Minimax(tempEngine, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha) break; // Alpha cutoff
                }
                return minEval;
            }
        }

        private void OrderMoves(List<ChessMove> moves, char[,] board)
        {
            // Simple Move Ordering:
            // 1. Captures (higher value captured piece minus lower value capturing piece - MVV-LVA)
            // 2. Promotions
            // 3. Castling
            // 4. Quiet moves (rely on PST evaluation)
            moves.Sort((m1, m2) =>
            {
                int score1 = GetMoveOrderScore(m1, board);
                int score2 = GetMoveOrderScore(m2, board);
                return score2.CompareTo(score1); // Descending order
            });
        }

        private int GetMoveOrderScore(ChessMove m, char[,] board)
        {
            int score = 0;
            if (m.Captured != '.' || m.IsEnPassant)
            {
                char victim = m.Captured;
                char attacker = m.Piece;
                score = 10 * GetPieceValue(victim) - GetPieceValue(attacker) + 10000;
            }

            if (m.Promotion != '.')
            {
                score += 9000 + GetPieceValue(m.Promotion);
            }

            if (m.IsCastling)
            {
                score += 1000;
            }

            return score;
        }

        private int GetPieceValue(char p)
        {
            switch (char.ToLower(p))
            {
                case 'p': return PAWN_VAL;
                case 'n': return KNIGHT_VAL;
                case 'b': return BISHOP_VAL;
                case 'r': return ROOK_VAL;
                case 'q': return QUEEN_VAL;
                case 'k': return KING_VAL;
                default: return 0;
            }
        }

        private int EvaluateBoard(char[,] board)
        {
            int score = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char p = board[r, c];
                    if (p == '.') continue;

                    int value = GetPieceValue(p);
                    int pstBonus = GetPstBonus(p, r, c);

                    if (char.IsUpper(p)) // White
                    {
                        score += value + pstBonus;
                    }
                    else // Black
                    {
                        score -= (value + pstBonus);
                    }
                }
            }

            return score;
        }

        private int GetPstBonus(char p, int r, int c)
        {
            bool isWhite = char.IsUpper(p);
            char pl = char.ToLower(p);

            // Row index to look up in the table
            // Tables are from White's perspective (row 7 is White baseline)
            int rowIdx = isWhite ? r : (7 - r);
            int colIdx = isWhite ? c : (7 - c); // Mirror column for symmetry

            switch (pl)
            {
                case 'p': return PawnPst[rowIdx, colIdx];
                case 'n': return KnightPst[rowIdx, colIdx];
                case 'b': return BishopPst[rowIdx, colIdx];
                case 'r': return RookPst[rowIdx, colIdx];
                case 'q': return QueenPst[rowIdx, colIdx];
                case 'k': return KingMiddlePst[rowIdx, colIdx];
                default: return 0;
            }
        }

        private void CopyState(BoardState src, BoardState dest)
        {
            dest.WhiteTurn = src.WhiteTurn;
            dest.WhiteCanCastleKingside = src.WhiteCanCastleKingside;
            dest.WhiteCanCastleQueenside = src.WhiteCanCastleQueenside;
            dest.BlackCanCastleKingside = src.BlackCanCastleKingside;
            dest.BlackCanCastleQueenside = src.BlackCanCastleQueenside;
            dest.EnPassantSquare = src.EnPassantSquare;
            dest.LastMoveAlgebraic = src.LastMoveAlgebraic;
            Array.Copy(src.Board, dest.Board, src.Board.Length);
        }
    }
}
