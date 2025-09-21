QSnake Final Hotfix (no folders)

Included scripts (drop into Assets/Scripts/, replace if asked):
- Board.cs
- ObstacleRegistry.cs
- Column.cs
- SnakeController.cs  (with "pulse growth": tail grows when the pulse reaches the tail)
- ForestModeBootstrap.cs  (slow start + no-wrap)
- HUDSingleton.cs        (prevents duplicate HUD/TopBar)
- PowerupSpawner.cs      (robust spawner; replace your old one if needed)
- QSnakeAutoSetup.cs     (Tools/QSnake menu; safe to place anywhere)

Quick use
1) Import all scripts.
2) Open your Forest scene (can be empty) and run: Tools → QSnake → Setup ▸ Forest (current scene)
   - This will create Board, Snake (with simple sprites), Canvas+HUD and wire fields.
   - It also adds ObstacleRegistry so columns can kill the snake.
3) To stop double HUD text, run: Tools → QSnake → Setup ▸ HUD Singleton (fix double text).

Pulse growth
- When you eat an apple, the snake does NOT grow instantly.
- A pulse starts at the head and marches along the body (1 segment per tick).
- When it reaches the tail, the tail grows by 1.
- You will see a tiny scale bump on the segment where the pulse is now.

Power-ups
- The provided PowerupSpawner is simple and safe. Assign a pickupPrefab and PowerupSO[] in Inspector.
- SnakeController calls ApplyPowerup(def) when stepping on a spawned cell.

Notes
- Board has safeTopRows and PlayTopY, as some of your scripts expect.
- ObstacleRegistry provides both instance API and a static SetBlocked(cell,bool) wrapper (for older code).
- Column.cs moves left and marks its cell as blocked; when disabled it unblocks.
- ForestModeBootstrap disables wrap and slows the start, so Forest feels explorative.
- If your project already has AppleSpawner/StatusHUD/PowerupSO/etc., this pack does not replace them.

If anything still throws a compiler error, send me the topmost red line (file:line + message) and I’ll patch it quickly.
