using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ChessGame
{
    public class ChessClock : UserControl
    {
        private double _whiteTime = 600; // in seconds
        private double _blackTime = 600;
        private double _increment = 0;   // in seconds
        private bool _whiteActive = true;
        private bool _running = false;

        private readonly Timer _timer;

        public event EventHandler OnTimeExpired;

        public double WhiteTime => _whiteTime;
        public double BlackTime => _blackTime;

        public ChessClock()
        {
            this.DoubleBuffered = true;
            this.Size = new Size(320, 70);
            this.BackColor = Color.FromArgb(20, 18, 16);

            _timer = new Timer { Interval = 100 }; // 100ms tick for precision
            _timer.Tick += Timer_Tick;
        }

        public void SetTime(double initialMinutes, double incrementSeconds)
        {
            _whiteTime = initialMinutes * 60;
            _blackTime = initialMinutes * 60;
            _increment = incrementSeconds;
            _running = false;
            _timer.Stop();
            Invalidate();
        }

        public void Start()
        {
            _running = true;
            _timer.Start();
            Invalidate();
        }

        public void Stop()
        {
            _running = false;
            _timer.Stop();
            Invalidate();
        }

        public void Reset(double initialMinutes, double incrementSeconds)
        {
            SetTime(initialMinutes, incrementSeconds);
        }

        public void SwitchTurn(bool isWhiteTurn)
        {
            // Apply increment to the player who just finished their turn
            if (_running)
            {
                if (_whiteActive && !isWhiteTurn) // White turn finished
                {
                    _whiteTime += _increment;
                }
                else if (!_whiteActive && isWhiteTurn) // Black turn finished
                {
                    _blackTime += _increment;
                }
            }

            _whiteActive = isWhiteTurn;
            Invalidate();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_running) return;

            if (_whiteActive)
            {
                _whiteTime -= 0.1;
                if (_whiteTime <= 0)
                {
                    _whiteTime = 0;
                    Stop();
                    OnTimeExpired?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _blackTime -= 0.1;
                if (_blackTime <= 0)
                {
                    _blackTime = 0;
                    Stop();
                    OnTimeExpired?.Invoke(this, EventArgs.Empty);
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int halfWidth = this.Width / 2;

            // Draw two distinct clocks
            DrawSingleClock(g, new Rectangle(2, 2, halfWidth - 4, this.Height - 4), _whiteTime, true);
            DrawSingleClock(g, new Rectangle(halfWidth + 2, 2, halfWidth - 4, this.Height - 4), _blackTime, false);
        }

        private void DrawSingleClock(Graphics g, Rectangle rect, double timeInSeconds, bool isWhiteClock)
        {
            bool isActive = _running && (isWhiteClock == _whiteActive);

            // Background panel with rounded rect
            using (var path = RoundedRect(rect, 6))
            {
                Color panelBg = isActive ? Color.FromArgb(32, 30, 24) : Color.FromArgb(18, 16, 14);
                using (var bgBrush = new SolidBrush(panelBg))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border glow
                Color borderColor = isActive 
                    ? Color.FromArgb(212, 175, 55)          // Active: Gold
                    : Color.FromArgb(40, 40, 42);           // Inactive: Muted Dark Gray
                
                using (var borderPen = new Pen(borderColor, isActive ? 2f : 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Time text formatting
            int minutes = (int)(timeInSeconds / 60);
            double seconds = timeInSeconds % 60;
            string timeStr;
            
            // Under 20 seconds, show tenths of a second
            if (timeInSeconds < 20 && timeInSeconds > 0)
            {
                timeStr = $"{minutes}:{seconds:00.0}";
            }
            else
            {
                timeStr = $"{minutes}:{((int)seconds):00}";
            }

            // Colors
            Color textColor;
            if (timeInSeconds < 10)
            {
                textColor = Color.FromArgb(230, 70, 70); // Time pressure warning: Red
            }
            else if (isActive)
            {
                textColor = Color.FromArgb(255, 240, 215, 160); // Gold-tinted light text
            }
            else
            {
                textColor = Color.FromArgb(120, 120, 120); // Muted gray
            }

            // Draw Label ("WHITE" / "BLACK")
            using (var labelFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (var labelBrush = new SolidBrush(isWhiteClock ? Color.FromArgb(180, 180, 180) : Color.FromArgb(120, 120, 120)))
            {
                string labelText = isWhiteClock ? "WHITE CLOCK" : "BLACK CLOCK";
                var labelSize = g.MeasureString(labelText, labelFont);
                g.DrawString(labelText, labelFont, labelBrush, 
                    rect.X + (rect.Width - labelSize.Width) / 2f, 
                    rect.Y + 8);
            }

            // Draw Clock Digital Digits
            using (var digitFont = new Font("Consolas", 18f, FontStyle.Bold))
            using (var digitBrush = new SolidBrush(textColor))
            {
                var digitSize = g.MeasureString(timeStr, digitFont);
                g.DrawString(timeStr, digitFont, digitBrush, 
                    rect.X + (rect.Width - digitSize.Width) / 2f, 
                    rect.Y + rect.Height / 2f - digitSize.Height / 2f + 4);
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
