using Microsoft.Maui.Graphics;
using Driving.Models;

namespace Driving.GameEngine;

public class GameDrawable : IDrawable
{
    // Ошибка CS8618 решена, так как _gameState теперь всегда устанавливается в конструкторе
    private readonly GameState _gameState;

    public GameDrawable(GameState state)
    {
        _gameState = state;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // Обновление смещения разметки для анимации
        if (_gameState.IsRunning)
        {
            _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
            if (_gameState.RoadMarkingOffset > 60)
            {
                _gameState.RoadMarkingOffset -= 60;
            }
        }

        // 1. Рисуем асфальт
        canvas.FillColor = Colors.DarkSlateGray;
        canvas.FillRectangle(dirtyRect);

        // 2. Рисуем движущуюся разметку
        DrawRoadMarkings(canvas, dirtyRect);

        // 3. Рисуем машину игрока
        canvas.FillColor = Colors.LimeGreen;

        float playerX = _gameState.Player.CalculateX(dirtyRect.Width);
        float playerY = dirtyRect.Height - 150;

        canvas.FillRoundedRectangle(playerX, playerY,
                                    _gameState.Player.Width,
                                    _gameState.Player.Height, 5);

        // 4. Рисуем счет
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString($"Score: {_gameState.Score}", 20, 40, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);
    }

    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 4;
        canvas.StrokeDashPattern = new float[] { 30, 30 }; // Период 60

        float third = dirtyRect.Width / 3;

        // Цикл для отрисовки линий с учетом смещения
        for (int i = 0; i < dirtyRect.Height / 60 + 2; i++)
        {
            float y = (i * 60) + _gameState.RoadMarkingOffset;

            if (y > dirtyRect.Height + 30) continue; // Оптимизация

            // Левая линия
            canvas.DrawLine(third, y, third, y - 30);

            // Правая линия
            canvas.DrawLine(third * 2, y, third * 2, y - 30);
        }
    }
}