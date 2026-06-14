using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChessGame
{
    public class ChessBoardControl : UserControl
    {
        private ChessEngine _engine;
        private List<ChessMove> _legalMoves = new List<ChessMove>();
        private (int r, int c)? _selectedSquare = null;
        private (int r, int c)? _hoveredSquare = null;
        
        // Highlights
        private ChessMove? _lastMove = null;

        // Animations state
        private bool _isAnimating = false;
        private int _animStartRow, _animStartCol;
        private int _animEndRow, _animEndCol;
        private char _animPiece;
        private float _animProgress = 0f;
        private readonly Timer _animTimer;
        private const float ANIM_SPEED = 0.15f; // Completed in ~7 frames (~100-150ms)

        // Rendering colors and themes
        public class Theme
        {
            public string Name { get; set; }
            public Color LightSquare { get; set; }
            public Color DarkSquare { get; set; }
            public Color LastMoveHighlight { get; set; }
            public Color SelectedHighlight { get; set; }
        }

        public static readonly List<Theme> Themes = new List<Theme>
        {
            new Theme
            {
                Name = "Classic Forest",
                LightSquare = Color.FromArgb(240, 217, 181),
                DarkSquare = Color.FromArgb(118, 150, 86),
                LastMoveHighlight = Color.FromArgb(120, 247, 247, 105),
                SelectedHighlight = Color.FromArgb(140, 247, 230, 90)
            },
            new Theme
            {
                Name = "Midnight Ocean",
                LightSquare = Color.FromArgb(232, 237, 240),
                DarkSquare = Color.FromArgb(75, 115, 153),
                LastMoveHighlight = Color.FromArgb(120, 185, 211, 238),
                SelectedHighlight = Color.FromArgb(140, 130, 175, 225)
            },
            new Theme
            {
                Name = "Warm Walnut",
                LightSquare = Color.FromArgb(240, 220, 190),
                DarkSquare = Color.FromArgb(181, 136, 99),
                LastMoveHighlight = Color.FromArgb(100, 212, 175, 55),
                SelectedHighlight = Color.FromArgb(130, 255, 215, 0)
            },
            new Theme
            {
                Name = "Carbon Slate",
                LightSquare = Color.FromArgb(220, 220, 220),
                DarkSquare = Color.FromArgb(80, 80, 80),
                LastMoveHighlight = Color.FromArgb(100, 200, 200, 200),
                SelectedHighlight = Color.FromArgb(140, 212, 175, 55)
            }
        };

        private Theme _currentTheme = Themes[0];
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                Invalidate();
            }
        }

        private bool _flipped = false;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Flipped
        {
            get => _flipped;
            set
            {
                _flipped = value;
                Invalidate();
            }
        }

        // Layout parameters
        private RectangleF _boardRect;
        private float _squareSize;

        // Delegates / Events
        public delegate void MoveMadeHandler(ChessMove move);
        public event MoveMadeHandler OnMoveMade;

        public delegate void PromotionSelectionHandler(int r, int c, Action<char> promoteCallback);
        public event PromotionSelectionHandler OnPromotionRequired;

        public ChessBoardControl()
        {
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.FromArgb(30, 28, 24);

            _animTimer = new Timer { Interval = 15 };
            _animTimer.Tick += AnimTimer_Tick;

            this.MouseMove += ChessBoardControl_MouseMove;
            this.MouseLeave += ChessBoardControl_MouseLeave;
            this.MouseClick += ChessBoardControl_MouseClick;
            this.Resize += ChessBoardControl_Resize;
        }

        public void Initialize(ChessEngine engine)
        {
            _engine = engine;
            _selectedSquare = null;
            _hoveredSquare = null;
            _lastMove = null;
            _isAnimating = false;
            _animTimer.Stop();
            UpdateLegalMoves();
            RecalculateLayout();
            Invalidate();
        }

        public void SetLastMove(ChessMove? move)
        {
            _lastMove = move;
            Invalidate();
        }

        public void UpdateLegalMoves()
        {
            if (_engine == null) return;
            _legalMoves = _engine.GetLegalMoves(_engine.WhiteTurn);
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            _animProgress += ANIM_SPEED;
            if (_animProgress >= 1f)
            {
                _animProgress = 1f;
                _isAnimating = false;
                _animTimer.Stop();
            }
            Invalidate();
        }

        public void TriggerMoveAnimation(ChessMove m)
        {
            _animStartRow = m.StartRow;
            _animStartCol = m.StartCol;
            _animEndRow = m.EndRow;
            _animEndCol = m.EndCol;
            _animPiece = _engine.Board[m.EndRow, m.EndCol]; // Already updated in engine
            _animProgress = 0f;
            _isAnimating = true;
            _lastMove = m;
            _selectedSquare = null;
            _animTimer.Start();
        }

        private void RecalculateLayout()
        {
            float side = Math.Min(this.Width, this.Height);
            // Leave a small margin (e.g., 20px) around the board
            float margin = 12f;
            float boardSide = side - margin * 2;
            if (boardSide < 100) boardSide = 100;

            float x = (this.Width - boardSide) / 2f;
            float y = (this.Height - boardSide) / 2f;

            _boardRect = new RectangleF(x, y, boardSide, boardSide);
            _squareSize = boardSide / 8f;
        }

        private void ChessBoardControl_Resize(object sender, EventArgs e)
        {
            RecalculateLayout();
            Invalidate();
        }

        private (int r, int c)? HitTest(Point location)
        {
            if (!_boardRect.Contains(location)) return null;

            int col = (int)((location.X - _boardRect.X) / _squareSize);
            int row = (int)((location.Y - _boardRect.Y) / _squareSize);

            // Clamp
            row = Math.Max(0, Math.Min(7, row));
            col = Math.Max(0, Math.Min(7, col));

            if (_flipped)
            {
                row = 7 - row;
                col = 7 - col;
            }

            return (row, col);
        }

        private void ChessBoardControl_MouseMove(object sender, MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            if (hit != _hoveredSquare)
            {
                _hoveredSquare = hit;
                Invalidate();
            }
        }

        private void ChessBoardControl_MouseLeave(object sender, EventArgs e)
        {
            if (_hoveredSquare != null)
            {
                _hoveredSquare = null;
                Invalidate();
            }
        }

        private void ChessBoardControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (_engine == null || _isAnimating) return;

            var hit = HitTest(e.Location);
            if (!hit.HasValue) return;

            int r = hit.Value.r;
            int c = hit.Value.c;

            if (_selectedSquare.HasValue)
            {
                int sr = _selectedSquare.Value.r;
                int sc = _selectedSquare.Value.c;

                // Find if clicked square is a valid legal move destination
                ChessMove matchedMove = default;
                bool isLegal = false;
                
                // Keep track of moves with promotions
                var promoMoves = new List<ChessMove>();

                foreach (var m in _legalMoves)
                {
                    if (m.StartRow == sr && m.StartCol == sc && m.EndRow == r && m.EndCol == c)
                    {
                        isLegal = true;
                        if (m.Promotion != '.')
                        {
                            promoMoves.Add(m);
                        }
                        else
                        {
                            matchedMove = m;
                        }
                    }
                }

                if (isLegal)
                {
                    if (promoMoves.Count > 0)
                    {
                        // Promotion required! Call callback
                        OnPromotionRequired?.Invoke(r, c, (promoChar) =>
                        {
                            // Find matching promo move
                            foreach (var pm in promoMoves)
                            {
                                // Promotion char is matching case of turn
                                char neededPromo = _engine.WhiteTurn ? char.ToUpper(promoChar) : char.ToLower(promoChar);
                                if (char.ToLower(pm.Promotion) == char.ToLower(neededPromo))
                                {
                                    OnMoveMade?.Invoke(pm);
                                    break;
                                }
                            }
                        });
                    }
                    else
                    {
                        OnMoveMade?.Invoke(matchedMove);
                    }
                }
                else
                {
                    // Select another piece
                    char clickedPiece = _engine.Board[r, c];
                    if (clickedPiece != '.' && _engine.IsWhite(clickedPiece) == _engine.WhiteTurn)
                    {
                        _selectedSquare = (r, c);
                    }
                    else
                    {
                        _selectedSquare = null;
                    }
                    Invalidate();
                }
            }
            else
            {
                char clickedPiece = _engine.Board[r, c];
                if (clickedPiece != '.' && _engine.IsWhite(clickedPiece) == _engine.WhiteTurn)
                {
                    _selectedSquare = (r, c);
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // Paint background of control
            using (var bgBrush = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            if (_engine == null) return;

            // Draw Board Outline/Border
            using (var borderPen = new Pen(Color.FromArgb(50, 212, 175, 55), 3f))
            {
                g.DrawRectangle(borderPen, _boardRect.X - 2, _boardRect.Y - 2, _boardRect.Width + 4, _boardRect.Height + 4);
            }

            // Draw Squares
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var rect = GetSquareRect(r, c);
                    Color squareColor = (r + c) % 2 == 0 ? _currentTheme.LightSquare : _currentTheme.DarkSquare;
                    using (var br = new SolidBrush(squareColor))
                    {
                        g.FillRectangle(br, rect);
                    }
                }
            }

            // Highlight Last Move
            if (_lastMove.HasValue)
            {
                var lm = _lastMove.Value;
                var rectStart = GetSquareRect(lm.StartRow, lm.StartCol);
                var rectEnd = GetSquareRect(lm.EndRow, lm.EndCol);
                using (var br = new SolidBrush(_currentTheme.LastMoveHighlight))
                {
                    g.FillRectangle(br, rectStart);
                    g.FillRectangle(br, rectEnd);
                }
            }

            // Highlight Selected Square
            if (_selectedSquare.HasValue)
            {
                var rect = GetSquareRect(_selectedSquare.Value.r, _selectedSquare.Value.c);
                using (var br = new SolidBrush(_currentTheme.SelectedHighlight))
                {
                    g.FillRectangle(br, rect);
                }

                // Show Legal Moves for Selected Piece
                int sr = _selectedSquare.Value.r;
                int sc = _selectedSquare.Value.c;
                foreach (var m in _legalMoves)
                {
                    if (m.StartRow == sr && m.StartCol == sc)
                    {
                        var targetRect = GetSquareRect(m.EndRow, m.EndCol);
                        if (m.Captured != '.' || m.IsEnPassant)
                        {
                            // Draw elegant corner markers for capture
                            using (var pen = new Pen(Color.FromArgb(160, 220, 80, 80), 3f))
                            {
                                float len = _squareSize * 0.25f;
                                // Top-Left
                                g.DrawLine(pen, targetRect.X, targetRect.Y, targetRect.X + len, targetRect.Y);
                                g.DrawLine(pen, targetRect.X, targetRect.Y, targetRect.X, targetRect.Y + len);
                                // Top-Right
                                g.DrawLine(pen, targetRect.Right, targetRect.Y, targetRect.Right - len, targetRect.Y);
                                g.DrawLine(pen, targetRect.Right, targetRect.Y, targetRect.Right, targetRect.Y + len);
                                // Bottom-Left
                                g.DrawLine(pen, targetRect.X, targetRect.Bottom, targetRect.X + len, targetRect.Bottom);
                                g.DrawLine(pen, targetRect.X, targetRect.Bottom, targetRect.X, targetRect.Bottom - len);
                                // Bottom-Right
                                g.DrawLine(pen, targetRect.Right, targetRect.Bottom, targetRect.Right - len, targetRect.Bottom);
                                g.DrawLine(pen, targetRect.Right, targetRect.Bottom, targetRect.Right, targetRect.Bottom - len);
                            }
                        }
                        else
                        {
                            // Draw soft circle for empty square move
                            float rSize = _squareSize * 0.26f;
                            float rx = targetRect.X + (targetRect.Width - rSize) / 2f;
                            float ry = targetRect.Y + (targetRect.Height - rSize) / 2f;
                            using (var br = new SolidBrush(Color.FromArgb(90, 75, 120, 180)))
                            {
                                g.FillEllipse(br, rx, ry, rSize, rSize);
                            }
                        }
                    }
                }
            }

            // Highlight Hovered Square (Subtle overlay)
            if (_hoveredSquare.HasValue && !_isAnimating)
            {
                var h = _hoveredSquare.Value;
                var rect = GetSquareRect(h.r, h.c);
                using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                {
                    g.FillRectangle(br, rect);
                }
            }

            // Draw Check Highlight
            if (_engine.IsInCheck(_engine.WhiteTurn))
            {
                // Find king
                char king = _engine.WhiteTurn ? 'K' : 'k';
                int kr = -1, kc = -1;
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (_engine.Board[r, c] == king) { kr = r; kc = c; break; }
                    }
                }
                if (kr != -1)
                {
                    var rect = GetSquareRect(kr, kc);
                    // Pulsing red glow (outer to inner soft gradients)
                    using (var br = new PathGradientBrush(new PointF[] {
                        new PointF(rect.X, rect.Y),
                        new PointF(rect.Right, rect.Y),
                        new PointF(rect.Right, rect.Bottom),
                        new PointF(rect.X, rect.Bottom)
                    }))
                    {
                        br.CenterPoint = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
                        br.CenterColor = Color.FromArgb(200, 230, 40, 40);
                        br.SurroundColors = new Color[] { Color.FromArgb(0, 230, 40, 40) };
                        g.FillRectangle(br, rect);
                    }
                }
            }

            // Draw Coordinate Labels (Files a-h, Ranks 1-8)
            // Draw along the margins inside the squares, chess.com style
            using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                for (int i = 0; i < 8; i++)
                {
                    int logicalRow = _flipped ? (7 - i) : i;
                    int logicalCol = _flipped ? (7 - i) : i;

                    // Draw Rank Numbers (1-8) on the left-most column (physical col 0)
                    var rectCol = GetSquareRect(logicalRow, _flipped ? 7 : 0);
                    bool isLight = (i + 0) % 2 == 0;
                    Color labelColor = isLight ? _currentTheme.DarkSquare : _currentTheme.LightSquare;
                    using (var brush = new SolidBrush(labelColor))
                    {
                        string rank = (8 - logicalRow).ToString();
                        g.DrawString(rank, font, brush, rectCol.X + 3, rectCol.Y + 3);
                    }

                    // Draw File Letters (a-h) on the bottom-most row (physical row 7)
                    var rectRow = GetSquareRect(_flipped ? 0 : 7, logicalCol);
                    isLight = (7 + i) % 2 == 0;
                    labelColor = isLight ? _currentTheme.DarkSquare : _currentTheme.LightSquare;
                    using (var brush = new SolidBrush(labelColor))
                    {
                        string file = ((char)('a' + logicalCol)).ToString();
                        var size = g.MeasureString(file, font);
                        g.DrawString(file, font, brush, rectRow.Right - size.Width - 3, rectRow.Bottom - size.Height - 3);
                    }
                }
            }

            // Draw Static Pieces
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    // Skip the piece currently animating
                    if (_isAnimating && r == _animEndRow && c == _animEndCol) continue;

                    char piece = _engine.Board[r, c];
                    if (piece != '.')
                    {
                        DrawPiece(g, piece, GetSquareRect(r, c));
                    }
                }
            }

            // Draw Animating Piece
            if (_isAnimating)
            {
                var startRect = GetSquareRect(_animStartRow, _animStartCol);
                var endRect = GetSquareRect(_animEndRow, _animEndCol);

                float startX = startRect.X + startRect.Width / 2f;
                float startY = startRect.Y + startRect.Height / 2f;
                float endX = endRect.X + endRect.Width / 2f;
                float endY = endRect.Y + endRect.Height / 2f;

                float currX = startX + (endX - startX) * _animProgress;
                float currY = startY + (endY - startY) * _animProgress;

                var animRect = new RectangleF(
                    currX - startRect.Width / 2f,
                    currY - startRect.Height / 2f,
                    startRect.Width,
                    startRect.Height
                );

                DrawPiece(g, _animPiece, animRect);
            }
        }

        private RectangleF GetSquareRect(int r, int c)
        {
            int drawRow = _flipped ? (7 - r) : r;
            int drawCol = _flipped ? (7 - c) : c;
            return new RectangleF(
                _boardRect.X + drawCol * _squareSize,
                _boardRect.Y + drawRow * _squareSize,
                _squareSize,
                _squareSize
            );
        }

        private string GetPieceGlyph(char piece)
        {
            switch (char.ToLower(piece))
            {
                case 'p': return "♟";
                case 'r': return "♜";
                case 'n': return "♞";
                case 'b': return "♝";
                case 'q': return "♛";
                case 'k': return "♚";
                default: return "";
            }
        }

        private void DrawPiece(Graphics g, char piece, RectangleF rect)
        {
            string symbol = GetPieceGlyph(piece);
            if (string.IsNullOrEmpty(symbol)) return;

            bool isWhite = _engine.IsWhite(piece);

            // Piece size (roughly 72% of square size)
            float fontSize = rect.Height * 0.72f;

            using (var path = new GraphicsPath())
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // Add string outline to the path
                path.AddString(
                    symbol, 
                    new FontFamily("Segoe UI Symbol"), 
                    (int)FontStyle.Bold, 
                    fontSize, 
                    rect, 
                    sf
                );

                // Draw Drop Shadow
                // Shift matrix by offset
                var shadowMatrix = new Matrix();
                shadowMatrix.Translate(2.5f, 3.5f);
                using (var shadowPath = (GraphicsPath)path.Clone())
                {
                    shadowPath.Transform(shadowMatrix);
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // Fill piece with a luxurious gradient
                Brush fillBrush;
                Pen strokePen;

                if (isWhite)
                {
                    // Soft gradient from gold-tinted white down to warm cream
                    fillBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(255, 255, 255, 250),
                        Color.FromArgb(255, 238, 228, 204),
                        LinearGradientMode.Vertical
                    );
                    // Dark gold-brown pen for a premium outline
                    strokePen = new Pen(Color.FromArgb(220, 80, 70, 50), 1.8f)
                    {
                        LineJoin = LineJoin.Round
                    };
                }
                else
                {
                    // Dark gradient from carbon gray down to midnight obsidian black
                    fillBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(255, 50, 50, 55),
                        Color.FromArgb(255, 14, 15, 18),
                        LinearGradientMode.Vertical
                    );
                    // Off-white/gold pen for a crisp contrast outline
                    strokePen = new Pen(Color.FromArgb(220, 240, 230, 210), 1.4f)
                    {
                        LineJoin = LineJoin.Round
                    };
                }

                // Render
                using (fillBrush)
                using (strokePen)
                {
                    g.FillPath(fillBrush, path);
                    g.DrawPath(strokePen, path);
                }

                // Draw high gloss highlight inside White pieces
                if (isWhite)
                {
                    var glossMatrix = new Matrix();
                    glossMatrix.Translate(-0.8f, -1.2f);
                    using (var glossPath = (GraphicsPath)path.Clone())
                    {
                        glossPath.Transform(glossMatrix);
                        using (var glossPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                        {
                            g.DrawPath(glossPen, glossPath);
                        }
                    }
                }
            }
        }
    }
}
