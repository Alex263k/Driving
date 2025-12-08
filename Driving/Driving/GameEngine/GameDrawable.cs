using Driving.Models;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Driving.GameEngine;

public class GameDrawable : IDrawable
{
    private readonly GameState _gameState;
    // ...

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Обновление размеров и RoadMarkingOffset
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // Увеличиваем смещение разметки, чтобы создать эффект движения
        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) // Период пунктира 30+30
        {
            _gameState.RoadMarkingOffset -= 60;
        }

        // 2. Рисуем асфальт
        canvas.FillColor = Colors.DarkSlateGray;
        canvas.FillRectangle(dirtyRect);

        // 3. Рисуем движущуюся разметку
        DrawRoadMarkings(canvas, dirtyRect);

        // 4. Рисуем игрока
        canvas.FillColor = Colors.LimeGreen;

        float playerX = _gameState.Player.CalculateX(dirtyRect.Width);
        float playerY = dirtyRect.Height - 150;

        canvas.FillRoundedRectangle(playerX, playerY,
                                    _gameState.Player.Width,
                                    _gameState.Player.Height, 5);

        // 5. Рисуем счет
        // ...
    }

    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 4;
        canvas.StrokeDashPattern = new float[] { 30, 30 }; // Период 60

        float third = dirtyRect.Width / 3;

        // Отрисовка левой и правой разделительных линий
        for (int i = 0; i < dirtyRect.Height / 60 + 2; i++)
        {
            float y = (i * 60) + _gameState.RoadMarkingOffset;

            // Если линия выходит за пределы, пропустить
            if (y > dirtyRect.Height) continue;

            // Левая линия
            canvas.DrawLine(third, y, third, y - 30);

            // Правая линия
            canvas.DrawLine(third * 2, y, third * 2, y - 30);
        }
    }
}
