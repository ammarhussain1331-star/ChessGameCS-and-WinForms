using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChessGame
{
    public class CapturedPiecesControl : UserControl
    {
        private List<char> _capturedPieces = new List<char>();
        private int _scoreAdvantage = 0;
        private bool _drawWhitePieces = false; // If true, draws White pieces captured by Black

        public CapturedPiecesControl()
        {
            this.DoubleBuffered = true;
            this.Height = 28;
            this.BackColor = Color.Transparent;
        }

        public void UpdateCaptured(List<char> captured, int scoreAdvantage, bool drawWhitePieces)
        {
            _capturedPieces = new List<char>(captured);
            _scoreAdvantage = scoreAdvantage;
            _drawWhitePieces = drawWhitePieces;

            // Sort by piece value descending (Q, R, B, N, P)
            _capturedPieces.Sort((a, b) =>
            {
                int valA = GetPieceSortVal(a);
                int valB = GetPieceSortVal(b);
                return valB.CompareTo(valA);
            });

            Invalidate();
        }

        private int GetPieceSortVal(char p)
        {
            switch (char.ToLower(p))
            {
                case 'q': return 5;
                case 'r': return 4;
                case 'b': return 3;
                case 'n': return 2;
                case 'p': return 1;
                default: return 0;
            }
        }

        private string GetPieceGlyph(char p)
        {
            switch (char.ToLower(p))
            {
                case 'p': return "♟";
                case 'r': return "♜";
                case 'n': return "♞";
                case 'b': return "♝";
                case 'q': return "♛";
                default: return "";
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float xOffset = 2;
            float glyphHeight = this.Height * 0.75f;

            using (var font = new Font("Segoe UI Symbol", glyphHeight, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                foreach (char p in _capturedPieces)
                {
                    string symbol = GetPieceGlyph(p);
                    if (string.IsNullOrEmpty(symbol)) continue;

                    // Draw outline and fill for captured pieces
                    using (var path = new GraphicsPath())
                    {
                        var rect = new RectangleF(xOffset, 2, glyphHeight, glyphHeight);
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };

                        path.AddString(symbol, new FontFamily("Segoe UI Symbol"), (int)FontStyle.Bold, glyphHeight, rect, sf);

                        Brush fillBrush;
                        Pen strokePen;

                        if (_drawWhitePieces)
                        {
                            // Captured White pieces (drawn semi-translucent cream)
                            fillBrush = new SolidBrush(Color.FromArgb(180, 240, 235, 220));
                            strokePen = new Pen(Color.FromArgb(140, 80, 70, 50), 1f);
                        }
                        else
                        {
                            // Captured Black pieces (drawn semi-translucent dark carbon)
                            fillBrush = new SolidBrush(Color.FromArgb(180, 40, 40, 45));
                            strokePen = new Pen(Color.FromArgb(140, 240, 230, 210), 1f);
                        }

                        using (fillBrush)
                        using (strokePen)
                        {
                            g.FillPath(fillBrush, path);
                            g.DrawPath(strokePen, path);
                        }
                    }

                    // Shift right with overlay for compactness
                    xOffset += glyphHeight * 0.62f;
                }
            }

            // Draw Score Advantage (e.g. "+3")
            if (_scoreAdvantage > 0)
            {
                using (var scoreFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var scoreBrush = new SolidBrush(Color.FromArgb(212, 175, 55))) // Gold Advantage
                {
                    string scoreStr = $"+{_scoreAdvantage}";
                    var size = g.MeasureString(scoreStr, scoreFont);
                    g.DrawString(scoreStr, scoreFont, scoreBrush, xOffset + 4, (this.Height - size.Height) / 2f);
                }
            }
        }
    }
}
