using Microsoft.Maui.Graphics;
using Driving.Models;
using Microsoft.Maui.Storage;
using System;

namespace Driving.GameEngine;

public class GameDrawable : IDrawable
{
    private readonly GameState _gameState;
    private readonly StartPage.CarInfo _selectedCar;

    public GameDrawable(GameState state, StartPage.CarInfo selectedCar)
    {
        _gameState = state;
        _selectedCar = selectedCar;
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

        // 7. Draw Bonuses
        DrawBonuses(canvas);

        // 8. Draw the HUD (Score and Lives)
        DrawHud(canvas);

        // 9. Draw Game Over text
        if (_gameState.IsGameOver)
        {
            DrawGameOver(canvas, dirtyRect);
        }

        // 10. Draw Fuel Indicator
        DrawFuelIndicator(canvas);

        // 11. Draw Fuel Cans
        DrawFuelCans(canvas);

        // 12. Draw Low Fuel Warning
        if (_gameState.Player.CurrentFuel < 30 && !_gameState.IsGameOver)
        {
            DrawLowFuelWarning(canvas, dirtyRect);
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
        float playerX = _gameState.Player.VisualX;
        float playerY = _gameState.Player.VisualY;
        float width = _gameState.Player.Width;
        float height = _gameState.Player.Height;

        switch (_selectedCar.Name)
        {
            case "BASIC":
                DrawBasicCar(canvas, playerX, playerY, width, height);
                break;
            case "SPORTS":
                DrawSportsCar(canvas, playerX, playerY, width, height);
                break;
            case "POLICE":
                DrawPoliceCar(canvas, playerX, playerY, width, height, true);
                break;
            case "TAXI":
                DrawTaxi(canvas, playerX, playerY, width, height);
                break;
            case "RACING":
                DrawRacingCar(canvas, playerX, playerY, width, height);
                break;
            case "VIP":
                DrawVIPCar(canvas, playerX, playerY, width, height);
                break;
            default:
                DrawBasicCar(canvas, playerX, playerY, width, height);
                break;
        }
    }

    private void DrawBasicCar(ICanvas canvas, float x, float y, float width, float height)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width, height, 8);

        canvas.FillColor = _selectedCar.Color;
        canvas.FillRoundedRectangle(x, y, width, height, 8);

        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        float windshieldHeight = height * 0.25f;
        canvas.FillRoundedRectangle(x + 5, y + 5, width - 10, windshieldHeight, 4);

        canvas.FillColor = Colors.Yellow;
        float lightSize = 5f;
        canvas.FillCircle(x + lightSize, y + height - lightSize, lightSize);
        canvas.FillCircle(x + width - lightSize, y + height - lightSize, lightSize);

        canvas.FillColor = Colors.DarkRed;
        canvas.FillCircle(x + lightSize, y + lightSize, lightSize);
        canvas.FillCircle(x + width - lightSize, y + lightSize, lightSize);
    }

    private void DrawSportsCar(ICanvas canvas, float x, float y, float width, float height)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width, height, 12);

        canvas.FillColor = _selectedCar.Color;
        float sportsHeight = height * 0.8f;
        canvas.FillRoundedRectangle(x, y + (height - sportsHeight), width, sportsHeight, 12);

        canvas.FillColor = Colors.Black;
        canvas.FillRoundedRectangle(x + width * 0.3f, y - 5, width * 0.4f, 10, 5);

        canvas.FillColor = Colors.DarkSlateBlue.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(x + 8, y + 10, width - 16, height * 0.25f, 6);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(x + width * 0.4f, y + (height - sportsHeight), width * 0.2f, sportsHeight);

        canvas.FillColor = Colors.Yellow;
        canvas.FillRectangle(x + 3, y + height - 15, 8, 10);
        canvas.FillRectangle(x + width - 11, y + height - 15, 8, 10);
    }

    private void DrawPoliceCar(ICanvas canvas, float x, float y, float width, float height, bool isPlayer = false)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width, height, 8);

        canvas.FillColor = Colors.Blue;
        canvas.FillRoundedRectangle(x, y, width, height, 8);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(x, y + height * 0.4f, width, height * 0.2f);

        if (isPlayer)
        {
            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(x + width * 0.3f, y - 10, width * 0.4f, 12);
            canvas.FillColor = Colors.Blue;
            canvas.FillRectangle(x + width * 0.2f, y - 10, width * 0.1f, 12);
            canvas.FillRectangle(x + width * 0.7f, y - 10, width * 0.1f, 12);
        }

        float windshieldHeight = height * 0.25f;
        float windshieldY = y + height - windshieldHeight - 10;
        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(x + 5, windshieldY, width - 10, windshieldHeight, 4);

        canvas.FillColor = Colors.Yellow;
        float lightSize = 5f;
        float lightY = y + height - lightSize;
        canvas.FillCircle(x + lightSize, lightY, lightSize);
        canvas.FillCircle(x + width - lightSize, lightY, lightSize);

        // Используем DrawString вместо DrawBoldText для текста
        canvas.FontColor = Colors.White;
        canvas.FontSize = 12;
        canvas.DrawString("POLICE", x + width * 0.35f, y + height * 0.45f, width * 0.3f, 15,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawTaxi(ICanvas canvas, float x, float y, float width, float height)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width, height, 8);

        canvas.FillColor = Colors.Yellow;
        canvas.FillRoundedRectangle(x, y, width, height, 8);

        canvas.FillColor = Colors.Black;
        float checkerSize = 8f;
        for (float cx = x + 10; cx < x + width - 10; cx += checkerSize * 2)
        {
            for (float cy = y + 5; cy < y + 20; cy += checkerSize * 2)
            {
                canvas.FillRectangle(cx, cy, checkerSize, checkerSize);
                canvas.FillRectangle(cx + checkerSize, cy + checkerSize, checkerSize, checkerSize);
            }
        }

        canvas.FillColor = Colors.Red;
        canvas.FillRectangle(x + width * 0.4f, y - 5, width * 0.2f, 8);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 10;
        canvas.DrawString("TAXI", x + width * 0.4f, y - 5, width * 0.2f, 8,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width - 10, height * 0.25f, 4);

        canvas.FillColor = Colors.White;
        float lightSize = 5f;
        canvas.FillCircle(x + lightSize, y + height - lightSize, lightSize);
        canvas.FillCircle(x + width - lightSize, y + height - lightSize, lightSize);
    }

    private void DrawRacingCar(ICanvas canvas, float x, float y, float width, float height)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.5f);
        canvas.FillRoundedRectangle(x + 5, y + 5, width, height, 10);

        canvas.FillColor = _selectedCar.Color;
        float racingHeight = height * 0.7f;
        canvas.FillRoundedRectangle(x, y + (height - racingHeight), width, racingHeight, 10);

        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(x + width * 0.2f, y + (height - racingHeight) + 5, width * 0.6f, 15);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 10;
        canvas.DrawString("RACING", x + width * 0.3f, y + height * 0.3f, width * 0.4f, 15,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FontColor = Colors.Black;
        canvas.FontSize = 16;
        canvas.DrawString("01", x + width * 0.4f, y + height * 0.5f, width * 0.2f, 20,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FillColor = Colors.DarkGray;
        canvas.FillRectangle(x + width * 0.15f, y + height - 8, 6, 10);
        canvas.FillRectangle(x + width * 0.85f - 6, y + height - 8, 6, 10);
    }

    private void DrawVIPCar(ICanvas canvas, float x, float y, float width, float height)
    {
        canvas.FillColor = Colors.Gold.WithAlpha(0.3f);
        canvas.FillRoundedRectangle(x + 3, y + 3, width + 4, height + 4, 10);

        canvas.FillColor = Colors.Gold;
        canvas.FillRoundedRectangle(x, y, width, height, 10);

        canvas.StrokeColor = Colors.Silver;
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(x + 3, y + 3, width - 6, height - 6, 8);

        canvas.FillColor = Colors.DarkSlateBlue.WithAlpha(0.9f);
        canvas.FillRoundedRectangle(x + 8, y + 8, width - 16, height * 0.3f, 6);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 20;
        canvas.DrawString("⭐", x + width * 0.45f, y + height * 0.6f, width * 0.1f, 20,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FillColor = Colors.Silver;
        canvas.FillRectangle(x + width * 0.3f, y + height - 20, width * 0.4f, 10);

        canvas.FillColor = Colors.Cyan;
        canvas.FillCircle(x + 8, y + height - 15, 6);
        canvas.FillColor = Colors.Magenta;
        canvas.FillCircle(x + width - 8, y + height - 15, 6);
    }

    private void DrawEnemies(ICanvas canvas)
    {
        foreach (var enemy in _gameState.Enemies)
        {
            enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

            switch (enemy.Type)
            {
                case Enemy.EnemyType.RegularCar:
                    DrawRegularCar(canvas, enemy);
                    break;
                case Enemy.EnemyType.Truck:
                    DrawTruck(canvas, enemy);
                    break;
                case Enemy.EnemyType.Motorcycle:
                    DrawMotorcycle(canvas, enemy);
                    break;
                case Enemy.EnemyType.Police:
                    DrawEnemyPoliceCar(canvas, enemy);
                    break;
            }
        }
    }

    private void DrawRegularCar(ICanvas canvas, Enemy enemy)
    {
        float width = enemy.Width;
        float height = enemy.Height;
        float windshieldHeight = height * 0.25f;
        float lightSize = 5f;

        canvas.FillColor = Colors.Red;
        canvas.FillRoundedRectangle(enemy.X, enemy.Y, width, height, 8);

        float windshieldY = enemy.Y + height - windshieldHeight - 10;
        canvas.FillColor = Colors.Gray;
        canvas.FillRoundedRectangle(enemy.X + 5, windshieldY, width - 10, windshieldHeight, 4);

        canvas.FillColor = Colors.Yellow;
        float lightY = enemy.Y + height - lightSize;
        canvas.FillCircle(enemy.X + lightSize, lightY, lightSize);
        canvas.FillCircle(enemy.X + width - lightSize, lightY, lightSize);

        canvas.FillColor = Colors.DarkRed;
        canvas.FillCircle(enemy.X + lightSize, enemy.Y + lightSize, lightSize);
        canvas.FillCircle(enemy.X + width - lightSize, enemy.Y + lightSize, lightSize);
    }

    private void DrawTruck(ICanvas canvas, Enemy enemy)
    {
        float width = enemy.Width;
        float height = enemy.Height;

        canvas.FillColor = Colors.DarkBlue;
        canvas.FillRoundedRectangle(enemy.X, enemy.Y, width * 0.4f, height, 8);

        canvas.FillColor = Colors.Blue;
        canvas.FillRectangle(enemy.X + width * 0.4f, enemy.Y, width * 0.6f, height);

        canvas.FillColor = Colors.LightGray;
        canvas.FillRoundedRectangle(enemy.X + 5, enemy.Y + 5, width * 0.4f - 10, height * 0.3f, 4);

        canvas.FillColor = Colors.Black;
        float wheelSize = 8f;
        canvas.FillCircle(enemy.X + width * 0.1f, enemy.Y + height - wheelSize / 2, wheelSize);
        canvas.FillCircle(enemy.X + width * 0.3f, enemy.Y + height - wheelSize / 2, wheelSize);
        canvas.FillCircle(enemy.X + width * 0.7f, enemy.Y + height - wheelSize / 2, wheelSize);
        canvas.FillCircle(enemy.X + width * 0.9f, enemy.Y + height - wheelSize / 2, wheelSize);
    }

    private void DrawMotorcycle(ICanvas canvas, Enemy enemy)
    {
        float width = enemy.Width;
        float height = enemy.Height;

        canvas.FillColor = Colors.DarkRed;
        canvas.FillRoundedRectangle(enemy.X, enemy.Y + height * 0.4f, width, height * 0.6f, 10);

        canvas.FillColor = Colors.Black;
        canvas.FillRoundedRectangle(enemy.X + width * 0.3f, enemy.Y + height * 0.3f, width * 0.4f, height * 0.2f, 5);

        canvas.FillColor = Colors.Black;
        float wheelSize = 10f;
        canvas.FillCircle(enemy.X + width * 0.25f, enemy.Y + height - wheelSize / 2, wheelSize);
        canvas.FillCircle(enemy.X + width * 0.75f, enemy.Y + height - wheelSize / 2, wheelSize);

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 3;
        float handlebarY = enemy.Y + height * 0.4f;
        canvas.DrawLine(enemy.X + width * 0.5f, handlebarY, enemy.X + width * 0.5f, handlebarY - 15);
        canvas.DrawLine(enemy.X + width * 0.5f, handlebarY - 15, enemy.X + width * 0.2f, handlebarY - 10);
        canvas.DrawLine(enemy.X + width * 0.5f, handlebarY - 15, enemy.X + width * 0.8f, handlebarY - 10);
    }

    private void DrawEnemyPoliceCar(ICanvas canvas, Enemy enemy)
    {
        float width = enemy.Width;
        float height = enemy.Height;
        float windshieldHeight = height * 0.25f;
        float lightSize = 5f;

        canvas.FillColor = Colors.Blue;
        canvas.FillRoundedRectangle(enemy.X, enemy.Y, width, height, 8);

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(enemy.X, enemy.Y + height * 0.4f, width, height * 0.2f);

        float windshieldY = enemy.Y + height - windshieldHeight - 10;
        canvas.FillColor = Colors.Gray;
        canvas.FillRoundedRectangle(enemy.X + 5, windshieldY, width - 10, windshieldHeight, 4);

        canvas.FillColor = Colors.Yellow;
        float lightY = enemy.Y + height - lightSize;
        canvas.FillCircle(enemy.X + lightSize, lightY, lightSize);
        canvas.FillCircle(enemy.X + width - lightSize, lightY, lightSize);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 12;
        canvas.DrawString("POLICE", enemy.X + width * 0.35f, enemy.Y + height * 0.45f,
                         width * 0.3f, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
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

            canvas.FillCircle(centerX, centerY, radius);
            canvas.DrawCircle(centerX, centerY, radius);

            canvas.FontColor = Colors.Black;
            canvas.FontSize = 20;
            canvas.DrawString("$", collectible.X, collectible.Y, collectible.Width, collectible.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    private void DrawBonuses(ICanvas canvas)
    {
        foreach (var bonus in _gameState.Bonuses)
        {
            bonus.X = bonus.CalculateX(_gameState.ScreenWidth);
            float width = bonus.Width;
            float height = bonus.Height;
            float radius = width / 2f;
            float centerX = bonus.X + radius;
            float centerY = bonus.Y + radius;

            switch (bonus.Type)
            {
                case Bonus.BonusType.Shield:
                    canvas.FillColor = Colors.Cyan;
                    canvas.StrokeColor = Colors.Blue;
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(centerX, centerY, radius);
                    canvas.FillCircle(centerX, centerY, radius - 2);
                    canvas.FontColor = Colors.Blue;
                    canvas.FontSize = 24;
                    canvas.DrawString("🛡️", bonus.X, bonus.Y, width, height,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                    break;

                case Bonus.BonusType.Magnet:
                    canvas.FillColor = Colors.Red;
                    canvas.StrokeColor = Colors.DarkRed;
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(centerX, centerY, radius);
                    canvas.FillCircle(centerX, centerY, radius - 2);
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 24;
                    canvas.DrawString("🧲", bonus.X, bonus.Y, width, height,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                    break;

                case Bonus.BonusType.SlowMotion:
                    canvas.FillColor = Colors.Yellow;
                    canvas.StrokeColor = Colors.Orange;
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(centerX, centerY, radius);
                    canvas.FillCircle(centerX, centerY, radius - 2);
                    canvas.FontColor = Colors.Black;
                    canvas.FontSize = 24;
                    canvas.DrawString("🐌", bonus.X, bonus.Y, width, height,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                    break;

                case Bonus.BonusType.Multiplier:
                    canvas.FillColor = Colors.Green;
                    canvas.StrokeColor = Colors.DarkGreen;
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(centerX, centerY, radius);
                    canvas.FillCircle(centerX, centerY, radius - 2);
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 20;
                    canvas.DrawString("x2", bonus.X, bonus.Y, width, height,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                    break;

                case Bonus.BonusType.CoinRain:
                    canvas.FillColor = Colors.Gold;
                    canvas.StrokeColor = Colors.DarkGoldenrod;
                    canvas.StrokeSize = 3;
                    canvas.DrawCircle(centerX, centerY, radius);
                    canvas.FillCircle(centerX, centerY, radius - 2);
                    canvas.FontColor = Colors.Black;
                    canvas.FontSize = 24;
                    canvas.DrawString("💰", bonus.X, bonus.Y, width, height,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                    break;
            }
        }
    }

    private void DrawHud(ICanvas canvas)
    {
        // Score and High Score
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        DrawBoldText(canvas, $"Score: {_gameState.Score}", 20, 40, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        int highScore = Preferences.Get("HighScore", 0);
        DrawBoldText(canvas, $"High Score: {highScore}", 20, 70, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Coins Collected
        canvas.FontColor = Colors.Gold;
        canvas.FontSize = 24;
        DrawBoldText(canvas, $"Coins: {_gameState.CoinsCollected} 💰", 20, 100, 200, 50, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Current Car Indicator
        canvas.FontColor = _selectedCar.Color;
        canvas.FontSize = 16;
        DrawBoldText(canvas, $"Car: {_selectedCar.Name}", 20, 130, 200, 30, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Lives (hearts)
        canvas.FontColor = Colors.Red;
        canvas.FontSize = 30;
        float heartX = _gameState.ScreenWidth - 120;

        for (int i = 0; i < _gameState.Lives; i++)
        {
            DrawBoldText(canvas, "❤️", heartX + (i * 30), 40, 30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
        }

        // Active Bonuses Icons
        float bonusX = _gameState.ScreenWidth - 120;
        float bonusY = 100;
        int activeBonusCount = 0;

        if (_gameState.IsShieldActive)
        {
            canvas.FontColor = Colors.Cyan;
            canvas.FontSize = 24;
            DrawBoldText(canvas, "🛡️", bonusX + (activeBonusCount * 30), bonusY,
                30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
            activeBonusCount++;
        }

        if (_gameState.IsMagnetActive)
        {
            canvas.FontColor = Colors.Red;
            canvas.FontSize = 24;
            DrawBoldText(canvas, "🧲", bonusX + (activeBonusCount * 30), bonusY,
                30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
            activeBonusCount++;
        }

        if (_gameState.IsSlowMotionActive)
        {
            canvas.FontColor = Colors.Yellow;
            canvas.FontSize = 24;
            DrawBoldText(canvas, "🐌", bonusX + (activeBonusCount * 30), bonusY,
                30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
            activeBonusCount++;
        }

        if (_gameState.IsMultiplierActive)
        {
            canvas.FontColor = Colors.Green;
            canvas.FontSize = 24;
            DrawBoldText(canvas, "x2", bonusX + (activeBonusCount * 30), bonusY,
                30, 30, HorizontalAlignment.Left, VerticalAlignment.Top);
            activeBonusCount++;
        }
    }

    private void DrawFuelIndicator(ICanvas canvas)
    {
        float fuelBarX = _gameState.ScreenWidth - 220;
        float fuelBarY = 40;
        float fuelBarWidth = 200;
        float fuelBarHeight = 20;

        canvas.FillColor = Colors.DarkGray.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(fuelBarX, fuelBarY, fuelBarWidth, fuelBarHeight, 10);

        float fuelPercentage = _gameState.Player.CurrentFuel / _gameState.Player.MaxFuel;
        float fillWidth = fuelBarWidth * fuelPercentage;

        if (fuelPercentage > 0.5f)
            canvas.FillColor = Colors.LimeGreen;
        else if (fuelPercentage > 0.2f)
            canvas.FillColor = Colors.Orange;
        else
            canvas.FillColor = Colors.Red;

        canvas.FillRoundedRectangle(fuelBarX, fuelBarY, fillWidth, fuelBarHeight, 10);

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        canvas.DrawRoundedRectangle(fuelBarX, fuelBarY, fuelBarWidth, fuelBarHeight, 10);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 12;
        DrawBoldText(canvas, $"FUEL: {_gameState.Player.CurrentFuel:F0}/{_gameState.Player.MaxFuel:F0}",
            fuelBarX, fuelBarY + fuelBarHeight + 5, fuelBarWidth, 20,
            HorizontalAlignment.Center, VerticalAlignment.Top);
    }

    private void DrawFuelCans(ICanvas canvas)
    {
        canvas.FillColor = Colors.Red;
        canvas.StrokeColor = Colors.DarkRed;
        canvas.StrokeSize = 2;

        foreach (var fuelCan in _gameState.FuelCans)
        {
            fuelCan.X = fuelCan.CalculateX(_gameState.ScreenWidth);

            float width = fuelCan.Width;
            float height = fuelCan.Height;

            canvas.FillColor = Colors.Red;
            canvas.FillRectangle(fuelCan.X, fuelCan.Y, width, height * 0.7f);

            canvas.FillColor = Colors.DarkGray;
            canvas.FillRoundedRectangle(fuelCan.X + width * 0.4f, fuelCan.Y - 5,
                width * 0.2f, 10, 5);

            canvas.FillColor = Colors.White;
            canvas.FontSize = 16;
            DrawBoldText(canvas, "⛽", fuelCan.X, fuelCan.Y, width, height,
                HorizontalAlignment.Center, VerticalAlignment.Center);

            if (_gameState.Player.CurrentFuel < 30)
            {
                canvas.StrokeColor = Colors.Yellow.WithAlpha(0.5f);
                canvas.StrokeSize = 4;
                canvas.DrawRectangle(fuelCan.X - 2, fuelCan.Y - 2,
                    width + 4, height + 4);
            }
        }
    }

    private void DrawLowFuelWarning(ICanvas canvas, RectF dirtyRect)
    {
        if ((DateTime.Now.Millisecond / 500) % 2 == 0)
        {
            canvas.FontColor = Colors.Red;
            canvas.FontSize = 24;

            string warning = _gameState.Player.CurrentFuel < 10 ? "CRITICAL FUEL!" : "LOW FUEL!";
            DrawBoldText(canvas, warning, dirtyRect.Center.X - 100, dirtyRect.Center.Y - 100,
                200, 50, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    private void DrawGameOver(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Colors.Red;
        canvas.FontSize = 48;
        DrawBoldText(canvas, "CRASHED!", dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50,
            300, 100, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawBoldText(ICanvas canvas, string text, float x, float y, float width, float height,
                              HorizontalAlignment hAlign = HorizontalAlignment.Left,
                              VerticalAlignment vAlign = VerticalAlignment.Top)
    {
        // Используем текущие настройки шрифта (уже установлены перед вызовом)
        canvas.DrawString(text, x - 0.5f, y, width, height, hAlign, vAlign);
        canvas.DrawString(text, x + 0.5f, y, width, height, hAlign, vAlign);
        canvas.DrawString(text, x, y - 0.5f, width, height, hAlign, vAlign);
        canvas.DrawString(text, x, y + 0.5f, width, height, hAlign, vAlign);
        canvas.DrawString(text, x, y, width, height, hAlign, vAlign);
    }
}