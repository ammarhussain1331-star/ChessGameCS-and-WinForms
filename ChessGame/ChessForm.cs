using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChessGame
{
    public class ChessForm : Form
    {
        private readonly bool _twoPlayerMode;
        private readonly int _timeMinutes;
        private readonly int _incrementSeconds;
        private readonly int _aiDifficulty;
        private readonly int _themeIndex;
        private readonly bool _startFullscreen;

        private ChessEngine _engine;
        private ChessBoardControl _boardControl;
        private ChessClock _clock;
        private MoveHistoryControl _historyControl;
        
        // Sidebar controls
        private CapturedPiecesControl _topCaptured;
        private CapturedPiecesControl _bottomCaptured;
        private Label _lblTopPlayer;
        private Label _lblBottomPlayer;
        private Panel _pnlTopTurn;
        private Panel _pnlBottomTurn;

        // Overlay Panels
        private Panel _pnlGameOverOverlay = null;

        // Action Buttons
        private Button _btnUndo;
        private Button _btnRestart;
        private Button _btnFlip;
        private Button _btnMenu;

        private bool _boardFlipped = false;
        private bool _aiThinking = false;

        public ChessForm(bool twoPlayer, int timeMinutes, int incrementSeconds, int aiDifficulty, int themeIndex, bool fullscreen)
        {
            _twoPlayerMode = twoPlayer;
            _timeMinutes = timeMinutes;
            _incrementSeconds = incrementSeconds;
            _aiDifficulty = aiDifficulty;
            _themeIndex = themeIndex;
            _startFullscreen = fullscreen;

            this.Text = _twoPlayerMode ? "Chess – Local Versus" : "Chess – Versus AI";
            this.ClientSize = new Size(1000, 720);
            this.MinimumSize = new Size(900, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 16, 14);
            this.DoubleBuffered = true;

            if (_startFullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }

            InitEngine();
            InitUI();
            StartGame();
        }

        private void InitEngine()
        {
            _engine = new ChessEngine();
        }

        private void InitUI()
        {
            // Main layout using TableLayoutPanel
            // Column 0: Chess Board (resizable, locks aspect ratio)
            // Column 1: Sidebar Dashboard (fixed width e.g., 340px)
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.FromArgb(18, 16, 14)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left container to center the ChessBoardControl
            var boardContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 12, 10)
            };

            _boardControl = new ChessBoardControl
            {
                Dock = DockStyle.Fill
            };
            _boardControl.CurrentTheme = ChessBoardControl.Themes[_themeIndex];
            _boardControl.OnMoveMade += BoardControl_OnMoveMade;
            _boardControl.OnPromotionRequired += BoardControl_OnPromotionRequired;

            boardContainer.Controls.Add(_boardControl);
            mainLayout.Controls.Add(boardContainer, 0, 0);

            // Right Sidebar panel
            var sidebar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(22, 20, 18)
            };
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // 0. Top Player Card
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));  // 1. Clocks
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 2. Move History
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // 3. Bottom Player Card
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // 4. Action Buttons Bar

            // 0. Top Player Card (Black or AI)
            var pnlTopPlayer = CreatePlayerCard(false, out _lblTopPlayer, out _topCaptured, out _pnlTopTurn);
            sidebar.Controls.Add(pnlTopPlayer, 0, 0);

            // 1. Clocks
            _clock = new ChessClock
            {
                Dock = DockStyle.Fill
            };
            _clock.OnTimeExpired += Clock_OnTimeExpired;
            sidebar.Controls.Add(_clock, 0, 1);

            // 2. Move History
            _historyControl = new MoveHistoryControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 10)
            };
            sidebar.Controls.Add(_historyControl, 0, 2);

            // 3. Bottom Player Card (White - You)
            var pnlBottomPlayer = CreatePlayerCard(true, out _lblBottomPlayer, out _bottomCaptured, out _pnlBottomTurn);
            sidebar.Controls.Add(pnlBottomPlayer, 0, 3);

            // 4. Action Buttons Bar
            var pnlActions = CreateActionsBar();
            sidebar.Controls.Add(pnlActions, 0, 4);

            mainLayout.Controls.Add(sidebar, 1, 0);
            this.Controls.Add(mainLayout);

            // Keyboard binds
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (this.FormBorderStyle == FormBorderStyle.None)
                    {
                        this.FormBorderStyle = FormBorderStyle.Sizable;
                        this.WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        this.Close();
                    }
                }
            };
        }

        private Panel CreatePlayerCard(bool isWhite, out Label lblName, out CapturedPiecesControl capturedCtrl, out Panel turnIndicator)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 26, 24),
                Padding = new Padding(8)
            };

            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(pnl.ClientRectangle, 6))
                {
                    using (var br = new SolidBrush(Color.FromArgb(28, 26, 24)))
                    {
                        g.FillPath(br, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(45, 45, 48), 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            };

            // Avatar circle indicator
            var pnlAvatar = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(10, 10)
            };
            pnlAvatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(isWhite ? Color.FromArgb(235, 230, 215) : Color.FromArgb(50, 50, 55)))
                {
                    g.FillEllipse(br, 1, 1, 34, 34);
                }
                using (var pen = new Pen(Color.FromArgb(212, 175, 55), 1.5f))
                {
                    g.DrawEllipse(pen, 1, 1, 34, 34);
                }

                // Draw Initial Letter inside Avatar
                using (var f = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (var br = new SolidBrush(isWhite ? Color.FromArgb(18, 18, 20) : Color.FromArgb(240, 240, 240)))
                {
                    string initial = isWhite ? "W" : (_twoPlayerMode ? "B" : "AI");
                    var sz = g.MeasureString(initial, f);
                    g.DrawString(initial, f, br, (pnlAvatar.Width - sz.Width) / 2f + 0.5f, (pnlAvatar.Height - sz.Height) / 2f + 0.5f);
                }
            };

            // Player Name Label
            lblName = new Label
            {
                Text = isWhite ? "Player White" : (_twoPlayerMode ? "Player Black" : "Chess AI Engine"),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 220, 200),
                Location = new Point(54, 8),
                Width = 200,
                Height = 18,
                BackColor = Color.Transparent
            };

            // Captured pieces control
            capturedCtrl = new CapturedPiecesControl
            {
                Location = new Point(54, 28),
                Width = 220,
                Height = 24
            };

            // Glow Turn Indicator Bar
            var indicator = new Panel
            {
                Size = new Size(6, 36),
                Location = new Point(pnl.Width - 16, 10),
                BackColor = Color.FromArgb(212, 175, 55),
                Visible = isWhite // White starts
            };
            turnIndicator = indicator;

            pnl.Controls.Add(pnlAvatar);
            pnl.Controls.Add(lblName);
            pnl.Controls.Add(capturedCtrl);
            pnl.Controls.Add(indicator);

            // Adjust turn indicator position on sizing
            pnl.Resize += (s, e) =>
            {
                indicator.Left = pnl.Width - 14;
            };

            return pnl;
        }

        private Panel CreateActionsBar()
        {
            var pnl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            _btnUndo = MakeActionBtn("↩   Undo");
            _btnUndo.Click += (s, e) => UndoLastMove();

            _btnRestart = MakeActionBtn("🔄  Reset");
            _btnRestart.Click += (s, e) => ResetGame();

            _btnFlip = MakeActionBtn("⇄   Flip");
            _btnFlip.Click += (s, e) => FlipBoard();

            _btnMenu = MakeActionBtn("🏠  Menu");
            _btnMenu.Click += (s, e) => this.Close();

            pnl.Controls.Add(_btnUndo, 0, 0);
            pnl.Controls.Add(_btnRestart, 1, 0);
            pnl.Controls.Add(_btnFlip, 2, 0);
            pnl.Controls.Add(_btnMenu, 3, 0);

            return pnl;
        }

        private Button MakeActionBtn(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.FromArgb(200, 190, 170),
                BackColor = Color.FromArgb(32, 30, 28),
                Margin = new Padding(2, 0, 2, 0),
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;

            btn.Paint += (s, e) =>
            {
                var b = (Button)s;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = b.ClientRectangle;
                using (var path = RoundedRect(rect, 4))
                {
                    using (var br = new SolidBrush(b.BackColor))
                    {
                        g.FillPath(br, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(48, 46, 44), 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                // Draw Text
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(b.ForeColor))
                {
                    g.DrawString(b.Text, b.Font, brush, rect, sf);
                }
            };

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = Color.FromArgb(44, 40, 36);
                btn.ForeColor = Color.FromArgb(255, 230, 160);
                btn.Invalidate();
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.FromArgb(32, 30, 28);
                btn.ForeColor = Color.FromArgb(200, 190, 170);
                btn.Invalidate();
            };

            return btn;
        }

        private void StartGame()
        {
            _boardControl.Initialize(_engine);
            _historyControl.Clear();

            if (_timeMinutes > 0)
            {
                _clock.SetTime(_timeMinutes, _incrementSeconds);
                _clock.Start();
                _clock.Visible = true;
            }
            else
            {
                _clock.Visible = false;
            }

            UpdateCapturedPieces();
            UpdateTurnIndicators();
        }

        private void ResetGame()
        {
            RemoveGameOverOverlay();
            _engine.Reset();
            _boardControl.Initialize(_engine);
            _historyControl.Clear();

            if (_timeMinutes > 0)
            {
                _clock.Reset(_timeMinutes, _incrementSeconds);
                _clock.Start();
            }

            _aiThinking = false;
            UpdateCapturedPieces();
            UpdateTurnIndicators();
        }

        private void UndoLastMove()
        {
            if (_aiThinking) return;

            RemoveGameOverOverlay();

            if (!_twoPlayerMode)
            {
                // In single player vs AI, we need to undo BOTH AI's move and player's move
                if (_engine.MoveHistoryLog.Count >= 2)
                {
                    _engine.UndoMove();
                    _engine.UndoMove();
                }
                else if (_engine.MoveHistoryLog.Count == 1)
                {
                    // Only White has moved, undo it
                    _engine.UndoMove();
                }
            }
            else
            {
                // Two player mode: undo single move
                _engine.UndoMove();
            }

            _boardControl.Initialize(_engine);
            _boardControl.SetLastMove(_engine.MoveHistoryLog.Count > 0 ? _engine.MoveHistoryLog[_engine.MoveHistoryLog.Count - 1] : (ChessMove?)null);
            _historyControl.SetMoves(_engine.MoveHistoryLog);

            if (_timeMinutes > 0)
            {
                _clock.SwitchTurn(_engine.WhiteTurn);
            }

            UpdateCapturedPieces();
            UpdateTurnIndicators();
        }

        private void FlipBoard()
        {
            _boardFlipped = !_boardFlipped;
            _boardControl.Flipped = _boardFlipped;
        }

        private void BoardControl_OnMoveMade(ChessMove move)
        {
            if (_aiThinking) return;

            // Make move in engine
            _engine.MakeMove(move);
            _boardControl.UpdateLegalMoves();

            // Play animation
            _boardControl.TriggerMoveAnimation(move);

            // Log history
            var loggedMove = _engine.MoveHistoryLog[_engine.MoveHistoryLog.Count - 1];
            _historyControl.AddHalfMove(loggedMove.Algebraic);

            // Clock update
            if (_timeMinutes > 0)
            {
                _clock.SwitchTurn(_engine.WhiteTurn);
            }

            // Update stats
            UpdateCapturedPieces();
            UpdateTurnIndicators();

            // Check game over
            if (CheckGameOver()) return;

            // Trigger AI if applicable
            if (!_twoPlayerMode && !_engine.WhiteTurn)
            {
                TriggerAiTurn();
            }
        }

        private async void TriggerAiTurn()
        {
            _aiThinking = true;
            _btnUndo.Enabled = false;
            _btnRestart.Enabled = false;
            UpdateTurnIndicators();

            var startTime = DateTime.Now;

            // AI Async Move
            // Use selected AI difficulty (Easy depth 2, Medium depth 3, Hard depth 4)
            var bestMove = await ChessAI.GetBestMoveAsync(_engine, _aiDifficulty);

            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            int minDelay = 1000; // 1 second minimum thinking delay
            if (elapsed < minDelay)
            {
                await Task.Delay(minDelay - (int)elapsed);
            }

            _aiThinking = false;
            _btnUndo.Enabled = true;
            _btnRestart.Enabled = true;

            if (bestMove.HasValue)
            {
                // Execute AI Move
                _engine.MakeMove(bestMove.Value);
                _boardControl.UpdateLegalMoves();
                _boardControl.TriggerMoveAnimation(bestMove.Value);
                var loggedMove = _engine.MoveHistoryLog[_engine.MoveHistoryLog.Count - 1];
                _historyControl.AddHalfMove(loggedMove.Algebraic);

                if (_timeMinutes > 0)
                {
                    _clock.SwitchTurn(_engine.WhiteTurn);
                }

                UpdateCapturedPieces();
                UpdateTurnIndicators();

                CheckGameOver();
            }
        }

        private bool CheckGameOver()
        {
            bool whiteChecked = _engine.IsInCheck(true);
            bool blackChecked = _engine.IsInCheck(false);

            if (_engine.IsCheckmate(true))
            {
                ShowGameOverOverlay("BLACK WINS", "Won by checkmate", Color.FromArgb(220, 80, 80));
                _clock.Stop();
                return true;
            }
            if (_engine.IsCheckmate(false))
            {
                ShowGameOverOverlay("WHITE WINS", "Won by checkmate", Color.FromArgb(70, 200, 110));
                _clock.Stop();
                return true;
            }
            if (_engine.IsStalemate(true) || _engine.IsStalemate(false))
            {
                ShowGameOverOverlay("DRAW", "Draw by stalemate", Color.FromArgb(170, 150, 120));
                _clock.Stop();
                return true;
            }

            return false;
        }

        private void Clock_OnTimeExpired(object sender, EventArgs e)
        {
            if (_clock.WhiteTime <= 0)
            {
                ShowGameOverOverlay("BLACK WINS", "White ran out of time", Color.FromArgb(220, 80, 80));
            }
            else if (_clock.BlackTime <= 0)
            {
                ShowGameOverOverlay("WHITE WINS", "Black ran out of time", Color.FromArgb(70, 200, 110));
            }
        }

        private void UpdateCapturedPieces()
        {
            // Scan board to see what's captured
            // Standard set
            var initialCount = new Dictionary<char, int>
            {
                {'P', 8}, {'R', 2}, {'N', 2}, {'B', 2}, {'Q', 1},
                {'p', 8}, {'r', 2}, {'n', 2}, {'b', 2}, {'q', 1}
            };

            var currentCount = new Dictionary<char, int>
            {
                {'P', 0}, {'R', 0}, {'N', 0}, {'B', 0}, {'Q', 0},
                {'p', 0}, {'r', 0}, {'n', 0}, {'b', 0}, {'q', 0}
            };

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    char p = _engine.Board[r, c];
                    if (p != '.' && char.ToLower(p) != 'k')
                    {
                        currentCount[p]++;
                    }
                }
            }

            // Captured lists
            var whiteCaptured = new List<char>(); // White pieces captured by Black
            var blackCaptured = new List<char>(); // Black pieces captured by White

            int whiteValueCaptured = 0;
            int blackValueCaptured = 0;

            foreach (var kvp in initialCount)
            {
                int diff = kvp.Value - currentCount[kvp.Key];
                for (int i = 0; i < diff; i++)
                {
                    if (char.IsUpper(kvp.Key)) // White piece captured
                    {
                        whiteCaptured.Add(kvp.Key);
                        whiteValueCaptured += GetPieceValue(kvp.Key);
                    }
                    else // Black piece captured
                    {
                        blackCaptured.Add(kvp.Key);
                        blackValueCaptured += GetPieceValue(kvp.Key);
                    }
                }
            }

            // Material Advantage scores
            int whiteAdv = Math.Max(0, blackValueCaptured - whiteValueCaptured);
            int blackAdv = Math.Max(0, whiteValueCaptured - blackValueCaptured);

            // Update captured displays
            // Top player captured (White pieces captured by Black)
            _topCaptured.UpdateCaptured(whiteCaptured, blackAdv, true);
            // Bottom player captured (Black pieces captured by White)
            _bottomCaptured.UpdateCaptured(blackCaptured, whiteAdv, false);
        }

        private int GetPieceValue(char p)
        {
            switch (char.ToLower(p))
            {
                case 'p': return 1;
                case 'n': return 3;
                case 'b': return 3;
                case 'r': return 5;
                case 'q': return 9;
                default: return 0;
            }
        }

        private void UpdateTurnIndicators()
        {
            if (_aiThinking)
            {
                _pnlTopTurn.BackColor = Color.FromArgb(220, 150, 40); // AI Thinking alert: Amber
                _pnlTopTurn.Visible = true;
                _pnlBottomTurn.Visible = false;
            }
            else
            {
                _pnlTopTurn.BackColor = Color.FromArgb(212, 175, 55);
                _pnlTopTurn.Visible = !_engine.WhiteTurn;
                _pnlBottomTurn.Visible = _engine.WhiteTurn;
            }
        }

        // ================= PAWN PROMOTION OVERLAY =================
        private void BoardControl_OnPromotionRequired(int r, int c, Action<char> promoteCallback)
        {
            // Instead of standard prompt, open a small popup form centered on the board square
            var boardScreenPos = _boardControl.PointToScreen(Point.Empty);
            
            var popup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(240, 72),
                BackColor = Color.FromArgb(32, 28, 24),
                ShowInTaskbar = false
            };

            popup.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(popup.ClientRectangle, 6))
                {
                    using (var br = new SolidBrush(Color.FromArgb(32, 28, 24)))
                    {
                        g.FillPath(br, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(212, 175, 55), 2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            };

            // Layout 4 piece promotion buttons
            char[] promoOptions = { 'q', 'r', 'b', 'n' };
            string[] symbols = { "♛", "♜", "♝", "♞" };

            for (int i = 0; i < 4; i++)
            {
                char piece = promoOptions[i];
                string sym = symbols[i];

                var btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI Symbol", 20f, FontStyle.Bold),
                    Size = new Size(50, 50),
                    Location = new Point(10 + i * 56, 11),
                    ForeColor = Color.FromArgb(255, 235, 200),
                    BackColor = Color.FromArgb(44, 40, 36),
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;

                btn.Paint += (sender, e) =>
                {
                    var b = (Button)sender;
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = RoundedRect(b.ClientRectangle, 4))
                    {
                        using (var br = new SolidBrush(b.BackColor))
                        {
                            g.FillPath(br, path);
                        }
                        using (var pen = new Pen(Color.FromArgb(80, 70, 60), 1f))
                        {
                            g.DrawPath(pen, path);
                        }

                        // Draw glyph
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (var brush = new SolidBrush(b.ForeColor))
                        {
                            g.DrawString(sym, b.Font, brush, b.ClientRectangle, sf);
                        }
                    }
                };

                btn.MouseEnter += (sender, e) => { btn.BackColor = Color.FromArgb(60, 54, 48); btn.Invalidate(); };
                btn.MouseLeave += (sender, e) => { btn.BackColor = Color.FromArgb(44, 40, 36); btn.Invalidate(); };

                btn.Click += (sender, e) =>
                {
                    promoteCallback(piece);
                    popup.Close();
                };

                popup.Controls.Add(btn);
            }

            // Position centered on the screen relative to where the board click happened
            // Center of the form is easiest
            var parentCenter = this.PointToScreen(new Point(this.Width / 2, this.Height / 2));
            popup.Location = new Point(parentCenter.X - popup.Width / 2, parentCenter.Y - popup.Height / 2);
            popup.ShowDialog(this);
        }

        // ================= GAME OVER OVERLAY =================
        private void ShowGameOverOverlay(string title, string subtitle, Color accentColor)
        {
            RemoveGameOverOverlay();

            // Create glass panel covering board control
            _pnlGameOverOverlay = new Panel
            {
                Size = _boardControl.Size,
                Location = _boardControl.Location,
                BackColor = Color.FromArgb(190, 12, 10, 8) // Dark translucent
            };

            // Recenter/Resize handler
            _boardControl.Resize += BoardControl_OnResizeForOverlay;

            _pnlGameOverOverlay.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                var rect = _pnlGameOverOverlay.ClientRectangle;

                // Center coordinates
                float cx = rect.Width / 2f;
                float cy = rect.Height / 2f;

                // 1. Draw large title
                using (var titleFont = new Font("Georgia", 28f, FontStyle.Bold))
                using (var brush = new SolidBrush(accentColor))
                {
                    var sz = g.MeasureString(title, titleFont);
                    g.DrawString(title, titleFont, brush, cx - sz.Width / 2f, cy - 60);
                }

                // 2. Draw subtitle description
                using (var subFont = new Font("Segoe UI", 11f, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                {
                    var sz = g.MeasureString(subtitle, subFont);
                    g.DrawString(subtitle, subFont, brush, cx - sz.Width / 2f, cy - 10);
                }
            };

            // Play Again button
            var btnPlayAgain = new Button
            {
                Text = "Play Again",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                ForeColor = Color.FromArgb(20, 18, 16),
                BackColor = Color.FromArgb(212, 175, 55),
                Size = new Size(130, 36),
                UseVisualStyleBackColor = false
            };
            btnPlayAgain.FlatAppearance.BorderSize = 0;
            btnPlayAgain.Click += (s, e) => ResetGame();

            btnPlayAgain.Paint += (s, e) =>
            {
                var b = (Button)s;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(b.ClientRectangle, 4))
                {
                    using (var br = new SolidBrush(b.BackColor)) g.FillPath(br, path);
                    using (var pen = new Pen(Color.FromArgb(255, 215, 0), 1f)) g.DrawPath(pen, path);
                }
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var brush = new SolidBrush(b.ForeColor))
                {
                    g.DrawString(b.Text, b.Font, brush, b.ClientRectangle, sf);
                }
            };
            btnPlayAgain.MouseEnter += (s, e) => { btnPlayAgain.BackColor = Color.FromArgb(245, 215, 120); btnPlayAgain.Invalidate(); };
            btnPlayAgain.MouseLeave += (s, e) => { btnPlayAgain.BackColor = Color.FromArgb(212, 175, 55); btnPlayAgain.Invalidate(); };

            // Center the button below text
            btnPlayAgain.Location = new Point(
                _pnlGameOverOverlay.Width / 2 - btnPlayAgain.Width / 2,
                _pnlGameOverOverlay.Height / 2 + 30
            );

            _pnlGameOverOverlay.Controls.Add(btnPlayAgain);

            // Add overlay to the board's parent container
            _boardControl.Parent.Controls.Add(_pnlGameOverOverlay);
            _pnlGameOverOverlay.BringToFront();
        }

        private void BoardControl_OnResizeForOverlay(object sender, EventArgs e)
        {
            if (_pnlGameOverOverlay != null)
            {
                _pnlGameOverOverlay.Size = _boardControl.Size;
                _pnlGameOverOverlay.Location = _boardControl.Location;
                // Recenter button
                foreach (Control ctrl in _pnlGameOverOverlay.Controls)
                {
                    if (ctrl is Button)
                    {
                        ctrl.Location = new Point(
                            _pnlGameOverOverlay.Width / 2 - ctrl.Width / 2,
                            _pnlGameOverOverlay.Height / 2 + 30
                        );
                    }
                }
            }
        }

        private void RemoveGameOverOverlay()
        {
            if (_pnlGameOverOverlay != null)
            {
                _boardControl.Resize -= BoardControl_OnResizeForOverlay;
                _pnlGameOverOverlay.Parent.Controls.Remove(_pnlGameOverOverlay);
                _pnlGameOverOverlay.Dispose();
                _pnlGameOverOverlay = null;
            }
        }

        // Helper: rounded rectangle path
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _clock.Stop();
            base.OnFormClosing(e);
        }
    }
}
