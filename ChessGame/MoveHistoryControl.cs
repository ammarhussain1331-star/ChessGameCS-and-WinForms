using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame
{
    public class MoveHistoryControl : UserControl
    {
        private readonly List<string> _moves = new List<string>(); // List of half-moves
        private readonly VScrollBar _scrollBar;
        
        private const int LINE_HEIGHT = 26;
        private const int HEADER_HEIGHT = 28;
        private const int NUM_COL_WIDTH = 45;
        private const int MOVE_COL_WIDTH = 90;

        public MoveHistoryControl()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(24, 22, 18);

            _scrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Visible = false
            };
            _scrollBar.Scroll += (s, e) => Invalidate();
            this.Controls.Add(_scrollBar);

            this.MouseWheel += MoveHistoryControl_MouseWheel;
        }

        public void AddHalfMove(string moveAlgebraic)
        {
            _moves.Add(moveAlgebraic);
            UpdateScroll();
            // Scroll to bottom
            if (_scrollBar.Visible)
            {
                _scrollBar.Value = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
            }
            Invalidate();
        }

        public void SetMoves(List<ChessMove> history)
        {
            _moves.Clear();
            foreach (var m in history)
            {
                _moves.Add(m.Algebraic);
            }
            UpdateScroll();
            Invalidate();
        }

        public void Clear()
        {
            _moves.Clear();
            UpdateScroll();
            Invalidate();
        }

        private void MoveHistoryControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!_scrollBar.Visible) return;

            int newValue = _scrollBar.Value - (e.Delta / 120) * _scrollBar.SmallChange;
            _scrollBar.Value = Math.Max(_scrollBar.Minimum, Math.Min(_scrollBar.Maximum - _scrollBar.LargeChange + 1, newValue));
            Invalidate();
        }

        private void UpdateScroll()
        {
            int totalLines = (int)Math.Ceiling(_moves.Count / 2.0f);
            int visibleLines = Math.Max(0, (this.Height - HEADER_HEIGHT) / LINE_HEIGHT);

            if (visibleLines > 0 && totalLines > visibleLines)
            {
                _scrollBar.Visible = true;
                _scrollBar.Minimum = 0;
                _scrollBar.Maximum = totalLines;
                _scrollBar.LargeChange = visibleLines;
                _scrollBar.SmallChange = 1;
            }
            else
            {
                _scrollBar.Visible = false;
                _scrollBar.Value = 0;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScroll();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw Header Background
            using (var headerBg = new SolidBrush(Color.FromArgb(32, 28, 24)))
            {
                g.FillRectangle(headerBg, 0, 0, this.Width, HEADER_HEIGHT);
            }

            // Draw Header Border Line
            using (var pen = new Pen(Color.FromArgb(60, 212, 175, 55)))
            {
                g.DrawLine(pen, 0, HEADER_HEIGHT, this.Width, HEADER_HEIGHT);
            }

            // Draw Header Text
            using (var headerFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(180, 160, 120)))
            {
                g.DrawString("N°", headerFont, brush, 10, 6);
                g.DrawString("WHITE", headerFont, brush, NUM_COL_WIDTH + 10, 6);
                g.DrawString("BLACK", headerFont, brush, NUM_COL_WIDTH + MOVE_COL_WIDTH + 10, 6);
            }

            // Draw moves
            int startLine = _scrollBar.Visible ? _scrollBar.Value : 0;
            int totalLines = (int)Math.Ceiling(_moves.Count / 2.0f);
            int visibleLines = (this.Height - HEADER_HEIGHT) / LINE_HEIGHT + 1;
            int endLine = Math.Min(totalLines, startLine + visibleLines);

            using (var textFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (var activeFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var numBrush = new SolidBrush(Color.FromArgb(120, 110, 95)))
            {
                for (int i = startLine; i < endLine; i++)
                {
                    int y = HEADER_HEIGHT + (i - startLine) * LINE_HEIGHT;

                    // Alternating background
                    if (i % 2 == 0)
                    {
                        using (var lineBg = new SolidBrush(Color.FromArgb(28, 26, 22)))
                        {
                            g.FillRectangle(lineBg, 0, y, this.Width, LINE_HEIGHT);
                        }
                    }

                    // Draw Line Number
                    string numStr = $"{i + 1}.";
                    g.DrawString(numStr, textFont, numBrush, 10, y + 5);

                    // Draw White Move
                    int whiteIdx = i * 2;
                    if (whiteIdx < _moves.Count)
                    {
                        bool isLatestMove = (whiteIdx == _moves.Count - 1);
                        Color moveColor = isLatestMove ? Color.FromArgb(212, 175, 55) : Color.FromArgb(220, 215, 200);
                        using (var moveBrush = new SolidBrush(moveColor))
                        {
                            g.DrawString(_moves[whiteIdx], isLatestMove ? activeFont : textFont, moveBrush, NUM_COL_WIDTH + 10, y + 5);
                        }
                    }

                    // Draw Black Move
                    int blackIdx = i * 2 + 1;
                    if (blackIdx < _moves.Count)
                    {
                        bool isLatestMove = (blackIdx == _moves.Count - 1);
                        Color moveColor = isLatestMove ? Color.FromArgb(212, 175, 55) : Color.FromArgb(220, 215, 200);
                        using (var moveBrush = new SolidBrush(moveColor))
                        {
                            g.DrawString(_moves[blackIdx], isLatestMove ? activeFont : textFont, moveBrush, NUM_COL_WIDTH + MOVE_COL_WIDTH + 10, y + 5);
                        }
                    }
                }
            }
        }
    }
}
