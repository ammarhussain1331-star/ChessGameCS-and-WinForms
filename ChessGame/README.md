# ♔ Chess Desktop Client

A high-fidelity, modern desktop Chess client built in C# using Windows Forms (.NET). The application features custom GDI+ vector-like piece rendering, smooth sliding animations, blitz/rapid digital chess clocks, a minimax chess engine, and an elegant editorial dark-gold landscape user interface.

---

## ✦ Key Features

- **Landscape Dashboard UI**: A modern dashboard design optimized for widescreen screens with a glassmorphic menu container, player cards, and live metrics.
- **High-Fidelity Custom Rendering**: Custom-drawn chess board with double-buffering to eliminate flickering. Pieces are rendered using vector paths (`GraphicsPath`) filled with warm gradients, drop-shadows, and contrasting strokes.
- **Dynamic Sliding Animations**: Real-time timer-interpolated sliding animations when pieces move, ensuring smooth gameplay transitions.
- **Full Chess Rules Engine**: Correctly validates checks, checkmates, stalemates, castling, en passant, and pawn promotion overlays.
- **Asynchronous Minimax AI**: Multi-threaded AI player running on a background thread utilizing alpha-beta pruning, positional piece-square tables (PST), and move ordering heuristics for smooth, freeze-free thinking.
- **Live Digital Chess Clocks**: Clocks with time-increment support that glow active and show tenths-of-a-second precision under time pressure (<20s).
- **Move Logs & Captured Pieces**: Algebraic notation move tracker (e.g., `1. e4 e5`) in a scrollable panel, alongside sorted captured pieces and material advantages.
- **Visual Board Customization**: Real-time board theme switcher featuring four default styles: *Classic Forest*, *Midnight Ocean*, *Warm Walnut*, and *Carbon Slate*.

---

## 🗂 Project Structure

```
ChessGame/
│
├── Program.cs                 # Main application bootstrapper
├── MenuForm.cs                # Fullscreen Landscape launcher & configuration settings
├── ChessForm.cs               # Gameplay orchestration dashboard & check/promotion overlays
│
├── ChessBoardControl.cs       # Custom-drawn animated board & mouse hit-testing
├── ChessClock.cs              # Dual countdown digital timers with incremental support
├── CapturedPiecesControl.cs   # Displays captured pieces sorted by value with score math
├── MoveHistoryControl.cs      # Algebraic notation dual-column scrollable grid log
│
├── ChessEngine.cs             # State machine validating move legality, checkmates, and undos
├── ChessAI.cs                 # Search engine playing moves asynchronously (Minimax + Alpha-Beta)
│
├── ChessGame.csproj           # C# project settings targeting .NET Windows Forms
├── ChessGame.slnx             # XML Visual Studio Solution mapping
└── README.md                  # Project documentation
```

---

## 🚀 Getting Started

### Prerequisites
- **.NET SDK 8.0, 9.0, or 10.0** installed on your machine.
- Windows Operating System (required for native Windows Forms).

---

### 💻 Option A: Run inside Visual Studio (Recommended)

1. Launch **Visual Studio** (VS 2022 recommended).
2. Go to **File > Open > Project/Solution**.
3. Select and open **`ChessGame.slnx`** (or select **`ChessGame.csproj`**).
4. Select **Debug > Start Debugging** (or press **`F5`**) to build and launch the game.

---

### ⌨ Option B: Run from the Command Line

1. Open PowerShell or Command Prompt.
2. Navigate to the directory containing the project:
   ```powershell
   cd path\to\ChessGame
   ```
3. Run the application:
   ```powershell
   dotnet run -c Release
   ```

---

## 🕹 Controls & Gameplay

- **Select Piece**: Left-click on any friendly piece. Legal target destinations will highlight with blue translucent dots (or red corner brackets for captures).
- **Move Piece**: Left-click on any highlighted destination square.
- **Deselect / Reselect**: Left-click on an empty square to deselect, or click another of your pieces to change selection.
- **Pawn Promotion**: A glassmorphic modal will overlay in the center of the board, allowing you to choose a Queen, Rook, Bishop, or Knight.
- **Undo Move**: Click **↩ Undo** to revert the last move (reverts both your move and the AI's move in single-player).
- **Flip Board**: Click **⇄ Flip** to rotate the board view 180 degrees.
- **Esc Key**: 
  - Exit fullscreen mode (if active).
  - Close the active match and return to the main menu.
