using Microsoft.Maui.Graphics;
using Driving.Models;

namespace Driving.GameEngine;

public class GameDrawable : IDrawable
{
    private readonly GameState _gameState;

    public GameDrawable(GameState state)
    {
        _gameState = state;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Always update screen dimensions first
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // FIX: Ensure the player's starting position is set after dimensions are known (run once)
        if (_gameState.IsRunning && _gameState.Player.VisualX == 0 && _gameState.ScreenWidth > 0)
        {
            _gameState.Player.VisualX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.StartX = _gameState.Player.VisualX;
            _gameState.Player.VisualY = _gameState.ScreenHeight - 150;
        }

        // 2. Draw the asphalt
        canvas.FillColor = Colors.DarkSlateGray;
        canvas.FillRectangle(dirtyRect);

        // 3. Draw the moving road markings
        DrawRoadMarkings(canvas, dirtyRect);

        // 4. Draw the player car (using VisualX/Y for animation, and blinking logic)
        bool shouldDrawPlayer = true;

        if (_gameState.InvulnerabilityFrames > 0)
        {
            if (_gameState.InvulnerabilityFrames % 4 == 0)
            {
                shouldDrawPlayer = false;
            }
        }

        if (shouldDrawPlayer)
        {
            DrawPlayer(canvas);
        }

        // 5. Draw the enemies
        DrawEnemies(canvas);

        // 6. Draw Collectibles
        DrawCollectibles(canvas);

        // 7. Draw the HUD (Score and Lives)
        DrawHud(canvas);

        // 8. Draw Game Over text
        if (_gameState.IsGameOver)
        {
            DrawGameOver(canvas, dirtyRect);
        }
    }

    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 4;
        canvas.StrokeDashPattern = new float[] { 30, 30 };

        float third = dirtyRect.Width / 3;

        for (int i = 0; i < dirtyRect.Height / 60 + 2; i++)
        {
            float y = (i * 60) + _gameState.RoadMarkingOffset;

            if (y > dirtyRect.Height + 30) continue;

            canvas.DrawLine(third, y, third, y - 30);
            canvas.DrawLine(third * 2, y, third * 2, y - 30);
        }
    }

    private void DrawPlayer(ICanvas canvas)
    {
        // Use the animated visual coordinates
        float playerX = _gameState.Player.VisualX;
        float playerY = _gameState.Player.VisualY;
        float width = _gameState.Player.Width;
        float height = _gameState.Player.Height;

        // 1. Shadow 
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(playerX + 5, playerY + 5, width, height, 8);

        // 2. Body 
        canvas.FillColor = Colors.LimeGreen;
        canvas.FillRoundedRectangle(playerX, playerY, width, height, 8);

        // 3. Windshield 
        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        float windshieldHeight = height * 0.25f;
        float windshieldY = playerY;

        canvas.FillRoundedRectangle(playerX + 5, windshieldY + 5, width - 10, windshieldHeight, 4);

        // 4. Headlights 
        canvas.FillColor = Colors.Yellow;
        float lightSize = 5f;
        canvas.FillCircle(playerX + lightSize, playerY + height - lightSize, lightSize);
        canvas.FillCircle(playerX + width - lightSize, playerY + height - lightSize, lightSize);
    }

    private void DrawEnemies(ICanvas canvas)
    {
        foreach (var enemy in _gameState.Enemies)
        {
            enemy.X = enemy.CalculateX(_gameState.ScreenWidth);
            float width = enemy.Width;
            float height = enemy.Height;

            float windshieldHeight = height * 0.25f;
            float lightSize = 5f;

            // 1. Body (Red)
            canvas.FillColor = Colors.Red;
            canvas.FillRoundedRectangle(enemy.X, enemy.Y, width, height, 8);

            // 2. Windshield (Near the bottom/front edge of the car)
            float windshieldY = enemy.Y + height - windshieldHeight - 10;

            canvas.FillColor = Colors.Gray;
            canvas.FillRoundedRectangle(enemy.X + 5, windshieldY, width - 10, windshieldHeight, 4);

            // 3. Headlights (Bottom/Front edge)
            canvas.FillColor = Colors.Yellow;
            float lightY = enemy.Y + height - lightSize;

            canvas.FillCircle(enemy.X + lightSize, lightY, lightSize);
            canvas.FillCircle(enemy.X + width - lightSize, lightY, lightSize);

            // 4. Taillights (Top/Rear edge)
            canvas.FillColor = Colors.DarkRed;
            canvas.FillCircle(enemy.X + lightSize, enemy.Y + lightSize, lightSize);
            canvas.FillCircle(enemy.X + width - lightSize, enemy.Y + lightSize, lightSize);
        }
    }

    private void DrawCollectibles(ICanvas canvas)
    {
        canvas.FillColor = Colors.Gold;
        canvas.StrokeColor = Colors.DarkGoldenrod;
        canvas.StrokeSize = 2;

        foreach (var collectible in _gameState.Collectibles)
        {
            collectible.X = collectible.CalculateX(_gameState.ScreenWidth);

            float radius = collectible.Width / 2f;
            float centerX = collectible.X + radius;
            float centerY = collectible.Y + radius;

            // Draw a circle for the coin
            canvas.FillCircle(centerX, centerY, radius);
            canvas.DrawCircle(centerX, centerY, radius);

            // Draw a "$" symbol in the middle
            canvas.FontColor = Colors.Black;
            canvas.FontSize = 20;
            canvas.DrawString("$", collectible.X, collectible.Y, collectible.Width, collectible.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    private void DrawHud(ICanvas canvas)
    {
        // Score and High Score
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;

        canvas.DrawString($"Очки: {_gameState.Score}", 20, 40, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        int highScore = Preferences.Get("HighScore", 0);
        canvas.DrawString($"Рекорд: {highScore}", 20, 70, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Coins Collected
        canvas.FontColor = Colors.Gold;
        canvas.FontSize = 24;
        canvas.DrawString($"Монеты: {_gameState.CoinsCollected} 💰", 20, 100, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Lives (hearts)
        canvas.FontColor = Colors.Red;
        canvas.FontSize = 30;
        float heartX = _gameState.ScreenWidth - 120;

        for (int i = 0; i < _gameState.Lives; i++)
        {
            canvas.DrawString("❤️", heartX + (i * 30), 40, 30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
        }
    }

    private void DrawGameOver(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Colors.Red;
        canvas.FontSize = 48;
        canvas.DrawString("CRASHED!", dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}