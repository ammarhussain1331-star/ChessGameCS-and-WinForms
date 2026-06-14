using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ChessGame
{
    public class MenuForm : Form
    {
        // ═══════════════════════════════════════════════════════
        //  COLOR PALETTE — Deep Space / Neon Accent
        // ═══════════════════════════════════════════════════════
        private static readonly Color Bg1 = Color.FromArgb(10, 10, 20);
        private static readonly Color Bg2 = Color.FromArgb(18, 14, 36);
        private static readonly Color CardBg = Color.FromArgb(18, 18, 32);
        private static readonly Color CardBorder = Color.FromArgb(45, 42, 70);

        private static readonly Color Accent = Color.FromArgb(90, 140, 255);       // Cool blue
        private static readonly Color AccentGlow = Color.FromArgb(120, 170, 255);   // Lighter glow
        private static readonly Color AccentAlt = Color.FromArgb(160, 100, 255);    // Violet
        private static readonly Color AccentCyan = Color.FromArgb(80, 220, 240);    // Cyan pop

        private static readonly Color TextPrimary = Color.FromArgb(235, 235, 250);
        private static readonly Color TextSecondary = Color.FromArgb(140, 140, 170);
        private static readonly Color TextMuted = Color.FromArgb(90, 90, 115);

        private static readonly Color BtnPlay1 = Color.FromArgb(60, 110, 240);
        private static readonly Color BtnPlay2 = Color.FromArgb(100, 60, 220);
        private static readonly Color BtnPvP1 = Color.FromArgb(30, 180, 160);
        private static readonly Color BtnPvP2 = Color.FromArgb(20, 130, 140);
        private static readonly Color BtnQuit1 = Color.FromArgb(180, 50, 70);
        private static readonly Color BtnQuit2 = Color.FromArgb(130, 30, 50);

        private static readonly Color SelectorActive = Accent;
        private static readonly Color SelectorInactive = Color.FromArgb(30, 30, 50);
        private static readonly Color SelectorBorder = Color.FromArgb(55, 55, 80);

        // ═══════════════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════════════
        private int _timeMinutes = 10;
        private int _incrementSeconds = 0;
        private int _aiDifficulty = 3;
        private int _themeIndex = 0;
        private bool _fullscreen = false;

        // Controls
        private Button _btnVsAI;
        private Button _btnVsPlayer;
        private Button _btnQuit;
        private Button _btnFullscreen;
        private Button[] _timeBtns;
        private Button[] _aiBtns;
        private Button[] _themeBtns;

        // Particle system
        private List<Particle> _particles = new List<Particle>();
        private System.Windows.Forms.Timer _animTimer;
        private Random _rng = new Random();

        // Double-buffered panel to prevent child flicker
        private class DBPanel : Panel
        {
            public DBPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            }
        }

        private class Particle
        {
            public float X, Y, VX, VY, Size, Alpha;
        }

        public MenuForm()
        {
            this.Text = "Chess Launcher";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Bg1;
            this.DoubleBuffered = true;

            InitParticles(60);
            InitUI();
            StartAnimation();
        }

        // ═══════════════════════════════════════════════════════
        //  PARTICLES (floating ambient dots)
        // ═══════════════════════════════════════════════════════
        private void InitParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new Particle
                {
                    X = _rng.Next(0, 1920),
                    Y = _rng.Next(0, 1080),
                    VX = (float)(_rng.NextDouble() * 0.6 - 0.3),
                    VY = (float)(_rng.NextDouble() * 0.4 - 0.2),
                    Size = (float)(_rng.NextDouble() * 2.5 + 0.8),
                    Alpha = (float)(_rng.NextDouble() * 0.35 + 0.05)
                });
            }
        }

        private void StartAnimation()
        {
            _animTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _animTimer.Tick += (s, e) =>
            {
                foreach (var p in _particles)
                {
                    p.X += p.VX;
                    p.Y += p.VY;
                    if (p.X < -10) p.X = this.Width + 10;
                    if (p.X > this.Width + 10) p.X = -10;
                    if (p.Y < -10) p.Y = this.Height + 10;
                    if (p.Y > this.Height + 10) p.Y = -10;
                }
                this.Invalidate(false);
            };
            _animTimer.Start();
        }

        // ═══════════════════════════════════════════════════════
        //  MAIN UI
        // ═══════════════════════════════════════════════════════
        private void InitUI()
        {
            // ── Background paint ──
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Deep radial-ish gradient background
                using (var br = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(8, 6, 18),
                    Color.FromArgb(22, 16, 40),
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(br, this.ClientRectangle);
                }

                // Soft radial glow at center-top
                int glowR = Math.Max(this.Width, this.Height) / 2;
                var glowCenter = new Point(this.Width / 2, this.Height / 3);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(glowCenter.X - glowR, glowCenter.Y - glowR, glowR * 2, glowR * 2);
                    using (var br = new PathGradientBrush(path))
                    {
                        br.CenterColor = Color.FromArgb(18, 90, 140, 255);
                        br.SurroundColors = new Color[] { Color.FromArgb(0, 90, 140, 255) };
                        g.FillPath(br, path);
                    }
                }

                // Floating particles
                foreach (var p in _particles)
                {
                    int alpha = (int)(p.Alpha * 255);
                    using (var br = new SolidBrush(Color.FromArgb(alpha, 120, 160, 255)))
                    {
                        g.FillEllipse(br, p.X, p.Y, p.Size, p.Size);
                    }
                }

                // Subtle chess grid watermark (lower-right)
                int box = 40;
                int startX = this.Width - box * 9;
                int startY = this.Height - box * 6;
                using (var br = new SolidBrush(Color.FromArgb(5, 120, 140, 255)))
                {
                    for (int r = 0; r < 5; r++)
                        for (int c = 0; c < 8; c++)
                            if ((r + c) % 2 == 0)
                                g.FillRectangle(br, startX + c * box, startY + r * box, box, box);
                }

                // Top accent line
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, 0, this.Width, 3),
                    AccentAlt, Accent, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br, 0, 0, this.Width, 3);
                }
            };

            // ── Main centered container ──
            var pnlContainer = new DBPanel
            {
                Size = new Size(1100, 640),
                BackColor = Color.Transparent
            };

            pnlContainer.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var r = pnlContainer.ClientRectangle;
                r.Inflate(-1, -1);
                using (var path = RoundedRect(r, 24))
                {
                    // Card fill
                    using (var br = new SolidBrush(Color.FromArgb(200, 14, 14, 28)))
                    {
                        g.FillPath(br, path);
                    }
                    // Frosted glass overlay
                    using (var br = new SolidBrush(Color.FromArgb(8, 255, 255, 255)))
                    {
                        g.FillPath(br, path);
                    }
                    // Glowing border
                    using (var pen = new Pen(Color.FromArgb(50, 90, 140, 255), 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            };

            // ═══════════════════════════════════════════════════
            //  LEFT COLUMN — BRANDING & PLAY BUTTONS
            // ═══════════════════════════════════════════════════

            // King icon
            var lblCrown = new Label
            {
                Text = "♔",
                Font = new Font("Segoe UI Symbol", 64, FontStyle.Bold),
                ForeColor = Accent,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 35, 500, 90)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "C H E S S",
                Font = new Font("Georgia", 42, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 130, 500, 65)
            };

            // Subtitle
            var lblSubtitle = new Label
            {
                Text = "SELECT  YOUR  GAME  MODE",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 200, 500, 25)
            };

            // ── Play Buttons ──
            _btnVsAI = MakeStyledButton("⚔   Player  vs  AI", BtnPlay1, BtnPlay2);
            _btnVsAI.Bounds = new Rectangle(55, 260, 390, 72);
            _btnVsAI.Click += (s, e) => StartGame(false);

            _btnVsPlayer = MakeStyledButton("👥   Player  vs  Player", BtnPvP1, BtnPvP2);
            _btnVsPlayer.Bounds = new Rectangle(55, 352, 390, 72);
            _btnVsPlayer.Click += (s, e) => StartGame(true);

            // Quit
            _btnQuit = MakeStyledButton("✕   Quit", BtnQuit1, BtnQuit2);
            _btnQuit.Bounds = new Rectangle(55, 530, 390, 55);
            _btnQuit.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            _btnQuit.Click += (s, e) => Application.Exit();

            // ═══════════════════════════════════════════════════
            //  CENTER DIVIDER
            // ═══════════════════════════════════════════════════
            var divider = new Panel
            {
                Bounds = new Rectangle(539, 40, 2, 560),
                BackColor = Color.Transparent
            };
            divider.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var br = new LinearGradientBrush(
                    divider.ClientRectangle,
                    Color.FromArgb(0, 90, 140, 255),
                    Color.FromArgb(50, 90, 140, 255),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(br, divider.ClientRectangle);
                }
            };

            // ═══════════════════════════════════════════════════
            //  RIGHT COLUMN — SETTINGS
            // ═══════════════════════════════════════════════════

            var lblSettings = new Label
            {
                Text = "⚙  G A M E   S E T T I N G S",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = AccentGlow,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(580, 48, 460, 35)
            };

            // ── Time Control ──
            int rowY = 120;
            var lblTime = MakeSettingLabel("⏱  TIME CONTROL");
            lblTime.Bounds = new Rectangle(580, rowY, 200, 28);

            _timeBtns = new Button[4];
            string[] timeLabels = { "5 min", "10 min", "30 min", "No Limit" };
            int[] timeVals = { 5, 10, 30, 0 };
            for (int i = 0; i < 4; i++)
            {
                int val = timeVals[i];
                int idx = i;
                _timeBtns[i] = MakeSelectorButton(timeLabels[i], i == 1);
                _timeBtns[i].Bounds = new Rectangle(580 + i * 115, rowY + 34, 108, 40);
                _timeBtns[i].Click += (s, e) =>
                {
                    _timeMinutes = val;
                    SelectSettingButton(_timeBtns, idx);
                };
            }

            // ── AI Difficulty ──
            rowY = 225;
            var lblAi = MakeSettingLabel("🤖  AI  DIFFICULTY");
            lblAi.Bounds = new Rectangle(580, rowY, 200, 28);

            _aiBtns = new Button[3];
            string[] aiLabels = { "Easy", "Medium", "Hard" };
            int[] aiVals = { 2, 3, 4 };
            for (int i = 0; i < 3; i++)
            {
                int val = aiVals[i];
                int idx = i;
                _aiBtns[i] = MakeSelectorButton(aiLabels[i], i == 1);
                _aiBtns[i].Bounds = new Rectangle(580 + i * 115, rowY + 34, 108, 40);
                _aiBtns[i].Click += (s, e) =>
                {
                    _aiDifficulty = val;
                    SelectSettingButton(_aiBtns, idx);
                };
            }

            // ── Board Theme ──
            rowY = 330;
            var lblTheme = MakeSettingLabel("🎨  BOARD  THEME");
            lblTheme.Bounds = new Rectangle(580, rowY, 200, 28);

            _themeBtns = new Button[4];
            string[] themeNames = { "Forest", "Ocean", "Walnut", "Carbon" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                _themeBtns[i] = MakeSelectorButton(themeNames[i], i == 0);
                _themeBtns[i].Bounds = new Rectangle(580 + i * 115, rowY + 34, 108, 40);
                _themeBtns[i].Click += (s, e) =>
                {
                    _themeIndex = idx;
                    SelectSettingButton(_themeBtns, idx);
                };
            }

            // ── Fullscreen Toggle ──
            rowY = 435;
            var lblFs = MakeSettingLabel("🖥  DISPLAY  MODE");
            lblFs.Bounds = new Rectangle(580, rowY, 200, 28);

            _btnFullscreen = MakeSelectorButton("Fullscreen : OFF", false);
            _btnFullscreen.Bounds = new Rectangle(580, rowY + 34, 338, 40);
            _btnFullscreen.Click += (s, e) =>
            {
                _fullscreen = !_fullscreen;
                _btnFullscreen.Text = _fullscreen ? "Fullscreen : ON" : "Fullscreen : OFF";
                ToggleSelectorState(_btnFullscreen, _fullscreen);
            };

            // ── Quote Block ──
            var pnlQuote = new DBPanel
            {
                Bounds = new Rectangle(580, 535, 460, 70),
                BackColor = Color.Transparent
            };
            pnlQuote.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var rect = pnlQuote.ClientRectangle;
                rect.Inflate(-1, -1);
                using (var path = RoundedRect(rect, 14))
                {
                    using (var br = new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
                        g.FillPath(br, path);
                    using (var pen = new Pen(Color.FromArgb(25, 120, 160, 255), 1f))
                        g.DrawPath(pen, path);
                }

                string quote = "\"Every chess master was once a beginner.\"  —  Irving Chernev";
                using (var font = new Font("Georgia", 9.5f, FontStyle.Italic))
                using (var brush = new SolidBrush(TextSecondary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(quote, font, brush, rect, sf);
                }
            };

            // ═══════════════════════════════════════════════════
            //  ASSEMBLE
            // ═══════════════════════════════════════════════════
            pnlContainer.Controls.Add(lblCrown);
            pnlContainer.Controls.Add(lblTitle);
            pnlContainer.Controls.Add(lblSubtitle);
            pnlContainer.Controls.Add(_btnVsAI);
            pnlContainer.Controls.Add(_btnVsPlayer);
            pnlContainer.Controls.Add(_btnQuit);

            pnlContainer.Controls.Add(divider);

            pnlContainer.Controls.Add(lblSettings);
            pnlContainer.Controls.Add(lblTime);
            for (int i = 0; i < 4; i++) pnlContainer.Controls.Add(_timeBtns[i]);
            pnlContainer.Controls.Add(lblAi);
            for (int i = 0; i < 3; i++) pnlContainer.Controls.Add(_aiBtns[i]);
            pnlContainer.Controls.Add(lblTheme);
            for (int i = 0; i < 4; i++) pnlContainer.Controls.Add(_themeBtns[i]);
            pnlContainer.Controls.Add(lblFs);
            pnlContainer.Controls.Add(_btnFullscreen);
            pnlContainer.Controls.Add(pnlQuote);

            this.Controls.Add(pnlContainer);

            // ── Centering logic ──
            Action centerPanel = () =>
            {
                pnlContainer.Location = new Point(
                    (this.ClientSize.Width - pnlContainer.Width) / 2,
                    (this.ClientSize.Height - pnlContainer.Height) / 2
                );
            };

            this.Resize += (s, e) => centerPanel();
            centerPanel();
        }

        // ═══════════════════════════════════════════════════════
        //  START GAME
        // ═══════════════════════════════════════════════════════
        private void StartGame(bool twoPlayer)
        {
            _animTimer?.Stop();
            var game = new ChessForm(twoPlayer, _timeMinutes, _incrementSeconds, _aiDifficulty, _themeIndex, _fullscreen);
            this.Hide();
            game.ShowDialog();
            this.Show();
            _animTimer?.Start();
        }

        // ═══════════════════════════════════════════════════════
        //  STYLED PLAY BUTTON (Gradient + Glow + Rounded)
        // ═══════════════════════════════════════════════════════
        private Button MakeStyledButton(string text, Color color1, Color color2)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                Tag = new object[] { text, color1, color2, false }
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            // Clip to rounded shape — eliminates corner artifacts
            btn.Resize += (s, e) =>
            {
                var b = (Button)s;
                using (var path = RoundedRect(b.ClientRectangle, 16))
                {
                    b.Region = new Region(path);
                }
            };

            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                var tag = (object[])b.Tag;
                string label = (string)tag[0];
                Color c1 = (Color)tag[1];
                Color c2 = (Color)tag[2];
                bool isHovered = (bool)tag[3];

                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var rect = b.ClientRectangle;

                // Clear background fully
                g.Clear(Color.FromArgb(14, 14, 28));

                using (var path = RoundedRect(rect, 16))
                {
                    Color fillA = isHovered ? ControlPaint.Light(c1, 0.15f) : c1;
                    Color fillB = isHovered ? ControlPaint.Light(c2, 0.15f) : c2;

                    using (var br = new LinearGradientBrush(rect, fillA, fillB, LinearGradientMode.Vertical))
                    {
                        g.FillPath(br, path);
                    }

                    // Subtle inner border
                    using (var pen = new Pen(Color.FromArgb(isHovered ? 100 : 60, 255, 255, 255), 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // Top gloss
                var glossRect = new RectangleF(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height / 2.5f);
                using (var glossPath = RoundedRect(Rectangle.Round(glossRect), 14))
                using (var br = new LinearGradientBrush(
                    glossRect,
                    Color.FromArgb(50, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.Vertical))
                {
                    g.FillPath(br, glossPath);
                }

                // Text
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(Color.White))
                {
                    g.DrawString(label, b.Font, brush, rect, sf);
                }
            };

            btn.MouseEnter += (s, e) =>
            {
                var b = (Button)s;
                var tag = (object[])b.Tag;
                b.Tag = new object[] { tag[0], tag[1], tag[2], true };
                b.Invalidate(false);
            };

            btn.MouseLeave += (s, e) =>
            {
                var b = (Button)s;
                var tag = (object[])b.Tag;
                b.Tag = new object[] { tag[0], tag[1], tag[2], false };
                b.Invalidate(false);
            };

            return btn;
        }

        // ═══════════════════════════════════════════════════════
        //  SELECTOR BUTTON (Settings chips)
        // ═══════════════════════════════════════════════════════
        private Button MakeSelectorButton(string text, bool active)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                ForeColor = active ? Color.White : TextSecondary,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.Tag = active;

            // Clip to rounded shape
            btn.Resize += (s, e) =>
            {
                var b = (Button)s;
                using (var path = RoundedRect(b.ClientRectangle, 12))
                {
                    b.Region = new Region(path);
                }
            };

            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                bool isActive = (bool)b.Tag;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                var rect = b.ClientRectangle;

                // Clear background fully
                g.Clear(Color.FromArgb(14, 14, 28));

                using (var path = RoundedRect(rect, 12))
                {
                    if (isActive)
                    {
                        using (var br = new LinearGradientBrush(rect, Accent, AccentAlt, LinearGradientMode.Horizontal))
                        {
                            g.FillPath(br, path);
                        }
                    }
                    else
                    {
                        using (var br = new SolidBrush(SelectorInactive))
                        {
                            g.FillPath(br, path);
                        }
                        using (var pen = new Pen(SelectorBorder, 1f))
                        {
                            g.DrawPath(pen, path);
                        }
                    }
                }

                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(isActive ? Color.White : TextSecondary))
                {
                    g.DrawString(b.Text, b.Font, brush, rect, sf);
                }
            };

            return btn;
        }

        // ═══════════════════════════════════════════════════════
        //  SETTING LABEL (category header)
        // ═══════════════════════════════════════════════════════
        private Label MakeSettingLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════
        private void ToggleSelectorState(Button btn, bool active)
        {
            btn.Tag = active;
            btn.ForeColor = active ? Color.White : TextSecondary;
            btn.Invalidate();
        }

        private void SelectSettingButton(Button[] group, int activeIndex)
        {
            for (int i = 0; i < group.Length; i++)
            {
                ToggleSelectorState(group[i], i == activeIndex);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _animTimer?.Stop();
            _animTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
