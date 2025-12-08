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
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // 1. Рисуем асфальт
        canvas.FillColor = Colors.DarkSlateGray;
        canvas.FillRectangle(dirtyRect);

        // 2. Рисуем движущуюся разметку
        DrawRoadMarkings(canvas, dirtyRect);

        // ==========================================================
        // 3. ИСПРАВЛЕННАЯ ЛОГИКА ОТРИСОВКИ ИГРОКА И МИГАНИЯ
        // ==========================================================

        bool shouldDrawPlayer = true;

        if (_gameState.InvulnerabilityFrames > 0)
        {
            // Если в режиме неуязвимости, пропускаем каждый 4-й кадр, чтобы мигать
            if (_gameState.InvulnerabilityFrames % 4 == 0)
            {
                shouldDrawPlayer = false;
            }
        }

        if (shouldDrawPlayer)
        {
            DrawPlayer(canvas);
        }
        // ==========================================================


        // 4. Рисуем врагов
        DrawEnemies(canvas);

        // 5. Рисуем счет и жизни
        DrawHud(canvas);

        // 6. Надпись Game Over (если необходимо)
        if (_gameState.IsGameOver)
        {
            DrawGameOver(canvas, dirtyRect);
        }
    }

    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 4;
        canvas.StrokeDashPattern = new float[] { 30, 30 }; // Период 60

        float third = dirtyRect.Width / 3;

        for (int i = 0; i < dirtyRect.Height / 60 + 2; i++)
        {
            float y = (i * 60) + _gameState.RoadMarkingOffset;

            if (y > dirtyRect.Height + 30) continue;

            // Левая линия
            canvas.DrawLine(third, y, third, y - 30);

            // Правая линия
            canvas.DrawLine(third * 2, y, third * 2, y - 30);
        }
    }

    private void DrawPlayer(ICanvas canvas)
    {
        canvas.FillColor = Colors.LimeGreen;

        float playerX = _gameState.Player.CalculateX(_gameState.ScreenWidth);
        float playerY = _gameState.ScreenHeight - 150;

        canvas.FillRoundedRectangle(playerX, playerY,
                                    _gameState.Player.Width,
                                    _gameState.Player.Height, 5);
    }

    private void DrawEnemies(ICanvas canvas)
    {
        canvas.FillColor = Colors.Red;

        foreach (var enemy in _gameState.Enemies)
        {
            enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

            canvas.FillRoundedRectangle(enemy.X, enemy.Y, enemy.Width, enemy.Height, 5);
        }
    }

    private void DrawHud(ICanvas canvas)
    {
        // 1. Счет
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString($"Score: {_gameState.Score}", 20, 40, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // 2. Рекорд
        int highScore = Preferences.Get("HighScore", 0);
        canvas.DrawString($"High: {highScore}", 20, 70, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // 3. Жизни (сердечки)
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