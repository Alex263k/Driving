using Driving.Models;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Driving.GameEngine;

public class GameDrawable : IDrawable
{
    private readonly GameState _gameState;
    private readonly StartPage.CarInfo _selectedCar;
    private readonly Dictionary<string, Microsoft.Maui.Graphics.IImage> _carImages = new();
    private readonly Dictionary<string, RectF> _hudElements = new();
    private bool _imagesLoaded = false;
    private bool _loadingImages = false;
    private Microsoft.Maui.Graphics.IImage? _customPlayerImage = null;
    private bool _customImageLoaded = false;

    // Size multipliers for different vehicle types (width, height)
    private readonly Dictionary<Enemy.EnemyType, (float widthMultiplier, float heightMultiplier)> _enemySizeMultipliers = new()
    {
        { Enemy.EnemyType.RegularCar, (1.5f, 1.5f) },    // Normal car: 50% bigger
        { Enemy.EnemyType.Truck, (1.5f, 2.0f) },         // Bus: 50% wider, 100% taller (longer)
        { Enemy.EnemyType.Motorcycle, (1.3f, 1.5f) },    // Motorcycle: 30% wider, 50% taller
        { Enemy.EnemyType.Police, (1.5f, 1.5f) }         // Police car: 50% bigger
    };

    // Size multiplier for player car
    private const float PLAYER_SIZE_MULTIPLIER = 1.5f;

    public GameDrawable(GameState state, StartPage.CarInfo selectedCar)
    {
        _gameState = state;
        _selectedCar = selectedCar;

        // Start loading images immediately
        Task.Run(() => LoadCarImagesAsync());

        // Load custom image if selected
        if (selectedCar.Name == "CUSTOM" && !string.IsNullOrEmpty(selectedCar.CustomImagePath))
        {
            Task.Run(() => LoadCustomImageAsync(selectedCar.CustomImagePath));
        }
    }

    private async Task LoadCustomImageAsync(string imagePath)
    {
        try
        {
            Debug.WriteLine($"=== LOADING CUSTOM SKIN ===");
            Debug.WriteLine($"Path: {imagePath}");
            Debug.WriteLine($"File exists: {File.Exists(imagePath)}");

            if (File.Exists(imagePath))
            {
                using var stream = File.OpenRead(imagePath);
                _customPlayerImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                _customImageLoaded = true;
                Debug.WriteLine($"✓ Custom skin loaded successfully");
                Debug.WriteLine($"Image dimensions: {_customPlayerImage?.Width}x{_customPlayerImage?.Height}");
            }
            else
            {
                Debug.WriteLine($"✗ Custom skin file not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR loading custom skin: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            _customPlayerImage = null;
        }
    }

    private async Task LoadCarImagesAsync()
    {
        if (_imagesLoaded || _loadingImages) return;
        _loadingImages = true;

        try
        {
            Debug.WriteLine("=== STARTING SPRITE LOADING ===");

            var assembly = Assembly.GetExecutingAssembly();

            // Log all available resources for debugging
            var allResources = assembly.GetManifestResourceNames();
            Debug.WriteLine($"Total resources found: {allResources.Length}");

            var imagesToLoad = new Dictionary<string, string>
            {
                { "Blue", "blue.png" },
                { "Bus", "bus.png" },
                { "Green", "green.png" },
                { "LightBlue", "lightblue.png" },
                { "Police", "police.png" },
                { "Taxi", "taxi.png" },
                { "Heart", "heart.png" }
            };

            foreach (var imagePair in imagesToLoad)
            {
                await LoadImageAsync(assembly, imagePair.Key, imagePair.Value);
            }

            // Create enemy references
            _carImages["EnemyBlue"] = _carImages["Blue"];
            _carImages["EnemyBus"] = _carImages["Bus"];
            _carImages["EnemyGreen"] = _carImages["Green"];
            _carImages["EnemyLightBlue"] = _carImages["LightBlue"];
            _carImages["EnemyPolice"] = _carImages["Police"];
            _carImages["EnemyTaxi"] = _carImages["Taxi"];


            _imagesLoaded = true;
            Debug.WriteLine($"=== SPRITES LOADED ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR loading sprites: {ex.Message}");
        }
        finally
        {
            _loadingImages = false;
        }
    }

    private async Task LoadImageAsync(Assembly assembly, string key, string imageName)
    {
        try
        {
            Debug.WriteLine($"Loading: {key} from {imageName}");

            // Resource paths to try
            string[] possiblePaths =
            {
                $"Driving.Resources.Images.{imageName}",
                $"{assembly.GetName().Name}.Resources.Images.{imageName}",
                $"Resources.Images.{imageName}",
                $"{imageName}"
            };

            foreach (var resourcePath in possiblePaths)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourcePath);
                    if (stream != null)
                    {
                        var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                        if (image != null)
                        {
                            _carImages[key] = image;
                            Debug.WriteLine($"✓ Loaded: {key} from {resourcePath}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"  Failed path {resourcePath}: {ex.Message}");
                }
            }

            Debug.WriteLine($"✗ FAILED to load: {key}");
            _carImages[key] = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Critical error for {key}: {ex.Message}");
            _carImages[key] = null;
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Update screen dimensions
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // 2. Initialize player position
        if (_gameState.IsRunning && _gameState.Player.VisualX == 0 && _gameState.ScreenWidth > 0)
        {
            _gameState.Player.VisualX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.StartX = _gameState.Player.VisualX;
            _gameState.Player.VisualY = _gameState.ScreenHeight - 150;
        }

        // Clear HUD elements
        _hudElements.Clear();

        // 3. Draw background
        DrawBackground(canvas, dirtyRect);

        // 4. Draw road markings
        DrawRoadMarkings(canvas, dirtyRect);

        // 5. Draw game entities (under player)
        DrawGameEntities(canvas);

        // 6. Draw player
        DrawPlayer(canvas);

        // 7. Draw HUD
        DrawHud(canvas);

        // 8. Draw game states
        DrawGameStates(canvas, dirtyRect);
    }

    private void DrawBackground(ICanvas canvas, RectF dirtyRect)
    {
        // Simple color instead of gradient
        canvas.FillColor = Color.FromArgb("#2F4F4F");
        canvas.FillRectangle(dirtyRect);
    }

    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        // Create dashed lines manually
        float dashLength = 20;
        float gapLength = 20;
        float totalLength = dashLength + gapLength;

        // Draw lane dividers
        float laneWidth = dirtyRect.Width / 3f;
        canvas.StrokeColor = Colors.White.WithAlpha(0.8f);
        canvas.StrokeSize = 3;

        for (int i = 1; i < 3; i++)
        {
            float x = laneWidth * i;

            // Draw dashed line
            for (float y = _gameState.RoadMarkingOffset; y < dirtyRect.Height; y += totalLength)
            {
                if (y + dashLength > 0)
                {
                    float startY = Math.Max(y, 0);
                    float endY = Math.Min(y + dashLength, dirtyRect.Height);
                    if (startY < endY)
                    {
                        canvas.DrawLine(x, startY, x, endY);
                    }
                }
            }
        }

        // Road edges
        canvas.StrokeColor = Colors.Yellow;
        canvas.StrokeSize = 2;

        // Left edge
        for (float y = _gameState.RoadMarkingOffset; y < dirtyRect.Height; y += totalLength)
        {
            if (y + dashLength > 0)
            {
                float startY = Math.Max(y, 0);
                float endY = Math.Min(y + dashLength, dirtyRect.Height);
                if (startY < endY)
                {
                    canvas.DrawLine(5, startY, 5, endY);
                }
            }
        }

        // Right edge
        for (float y = _gameState.RoadMarkingOffset; y < dirtyRect.Height; y += totalLength)
        {
            if (y + dashLength > 0)
            {
                float startY = Math.Max(y, 0);
                float endY = Math.Min(y + dashLength, dirtyRect.Height);
                if (startY < endY)
                {
                    canvas.DrawLine(dirtyRect.Width - 5, startY, dirtyRect.Width - 5, endY);
                }
            }
        }
    }

    private void DrawGameEntities(ICanvas canvas)
    {
        // Draw enemies
        foreach (var enemy in _gameState.Enemies)
        {
            enemy.X = enemy.CalculateX(_gameState.ScreenWidth);
            DrawEnemy(canvas, enemy);
        }

        // Draw collectibles
        foreach (var collectible in _gameState.Collectibles)
        {
            collectible.X = collectible.CalculateX(_gameState.ScreenWidth);
            DrawCollectible(canvas, collectible);
        }

        // Draw bonuses
        foreach (var bonus in _gameState.Bonuses)
        {
            bonus.X = bonus.CalculateX(_gameState.ScreenWidth);
            DrawBonus(canvas, bonus);
        }

        // Draw fuel cans
        foreach (var fuelCan in _gameState.FuelCans)
        {
            fuelCan.X = fuelCan.CalculateX(_gameState.ScreenWidth);
            DrawFuelCan(canvas, fuelCan);
        }
    }

    private void DrawEnemy(ICanvas canvas, Enemy enemy)
    {
        float x = enemy.X;
        float y = enemy.Y;
        float width = enemy.Width;
        float height = enemy.Height;

        // Get size multipliers for this enemy type
        var multipliers = _enemySizeMultipliers[enemy.Type];
        float drawWidth = width * multipliers.widthMultiplier;
        float drawHeight = height * multipliers.heightMultiplier;

        // Try to draw sprite
        string imageKey = GetEnemyImageKey(enemy.Type);

        if (_carImages.TryGetValue(imageKey, out var image) && image != null)
        {
            try
            {
                // Save canvas state
                canvas.SaveState();

                // Move to center of enemy position
                canvas.Translate(x + width / 2, y + height / 2);

                // Rotate 180 degrees to face backwards
                canvas.Rotate(180);

                // Draw image centered at (0,0) with appropriate size
                canvas.DrawImage(image, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight);

                // Restore canvas state
                canvas.RestoreState();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing enemy sprite {imageKey}: {ex.Message}");
                // Fallback to colored rectangle
            }
        }

        // Fallback: colored rectangle
        DrawEnemyFallback(canvas, enemy, drawWidth, drawHeight);
    }

    private string GetEnemyImageKey(Enemy.EnemyType type)
    {
        return type switch
        {
            Enemy.EnemyType.RegularCar => "EnemyBlue",
            Enemy.EnemyType.Truck => "EnemyBus",
            Enemy.EnemyType.Motorcycle => "EnemyGreen",
            Enemy.EnemyType.Police => "EnemyPolice",
            _ => "EnemyBlue"
        };
    }

    private void DrawEnemyFallback(ICanvas canvas, Enemy enemy, float drawWidth, float drawHeight)
    {
        float x = enemy.X;
        float y = enemy.Y;
        float width = enemy.Width;
        float height = enemy.Height;

        Color color = enemy.Type switch
        {
            Enemy.EnemyType.RegularCar => Colors.Red,
            Enemy.EnemyType.Truck => Colors.DarkBlue,
            Enemy.EnemyType.Motorcycle => Colors.DarkRed,
            Enemy.EnemyType.Police => Colors.Blue,
            _ => Colors.Red
        };

        // Save canvas state for rotation
        canvas.SaveState();

        // Move to center and rotate
        canvas.Translate(x + width / 2, y + height / 2);
        canvas.Rotate(180);

        // Draw car shape centered at (0,0)
        DrawCarShape(canvas, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight, color);

        // Restore canvas state
        canvas.RestoreState();
    }

    private void DrawPlayer(ICanvas canvas)
    {
        if (!_gameState.IsRunning) return;

        float x = _gameState.Player.VisualX;
        float y = _gameState.Player.VisualY;
        float width = _gameState.Player.Width;
        float height = _gameState.Player.Height;

        // Blink effect during invulnerability
        if (_gameState.InvulnerabilityFrames > 0)
        {
            if (_gameState.InvulnerabilityFrames % 4 == 0)
            {
                return; // Skip this frame
            }
        }

        // Increased size by PLAYER_SIZE_MULTIPLIER (50%)
        float drawWidth = width * PLAYER_SIZE_MULTIPLIER;
        float drawHeight = height * PLAYER_SIZE_MULTIPLIER;

        // Calculate offset to keep center at same position
        float offsetX = (drawWidth - width) / 2;
        float offsetY = (drawHeight - height) / 2;

        // Draw custom skin if selected and loaded
        if (_selectedCar.Name == "CUSTOM" && _customImageLoaded && _customPlayerImage != null)
        {
            try
            {
                // Save canvas state
                canvas.SaveState();

                // Draw custom image
                canvas.DrawImage(_customPlayerImage, x - offsetX, y - offsetY, drawWidth, drawHeight);

                // Restore canvas state
                canvas.RestoreState();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing custom skin: {ex.Message}");
                // Fallback to standard drawing
            }
        }

        // Try to draw sprite
        string imageKey = GetPlayerImageKey(_selectedCar.Name);

        if (_carImages.TryGetValue(imageKey, out var image) && image != null)
        {
            try
            {
                canvas.DrawImage(image, x - offsetX, y - offsetY, drawWidth, drawHeight);
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing player sprite {imageKey}: {ex.Message}");
                // Fallback to colored rectangle
            }
        }

        // Fallback: colored rectangle
        DrawCarShape(canvas, x - offsetX, y - offsetY, drawWidth, drawHeight, _selectedCar.Color);
    }

    private string GetPlayerImageKey(string carName)
    {
        return carName switch
        {
            "BASIC" => "Blue",
            "SPORTS" => "Green",
            "POLICE" => "Police",
            "TAXI" => "Taxi",
            "RACING" => "LightBlue",
            "VIP" => "Blue", // VIP uses Blue as placeholder
            "CUSTOM" => "Blue", // Custom also uses Blue as fallback
            _ => "Blue"
        };
    }

    private void DrawCarShape(ICanvas canvas, float x, float y, float width, float height, Color color)
    {
        // Car body
        canvas.FillColor = color;
        canvas.FillRoundedRectangle(x, y, width, height, 8);

        // Windows
        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(x + width * 0.1f, y + height * 0.1f,
                                  width * 0.8f, height * 0.25f, 4);

        // Headlights
        canvas.FillColor = Colors.Yellow;
        canvas.FillCircle(x + 5, y + height - 5, 4);
        canvas.FillCircle(x + width - 5, y + height - 5, 4);

        // Taillights
        canvas.FillColor = Colors.Red;
        canvas.FillCircle(x + 5, y + 5, 4);
        canvas.FillCircle(x + width - 5, y + 5, 4);
    }

    private void DrawCollectible(ICanvas canvas, Collectible collectible)
    {
        float x = collectible.X;
        float y = collectible.Y;
        float size = collectible.Width;

        // Glow effect
        canvas.FillColor = Colors.Gold.WithAlpha(0.3f);
        canvas.FillCircle(x + size / 2, y + size / 2, size / 2 + 2);

        // Coin body
        canvas.FillColor = Colors.Gold;
        canvas.FillCircle(x + size / 2, y + size / 2, size / 2);

        // Dollar sign
        var paint = new SolidPaint(Colors.DarkGoldenrod);
        canvas.SetFillPaint(paint, new RectF(x, y, size, size));
        canvas.FontSize = size * 0.6f;
        canvas.DrawString("$", x, y, size, size,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawBonus(ICanvas canvas, Bonus bonus)
    {
        float x = bonus.X;
        float y = bonus.Y;
        float size = bonus.Width;
        float centerX = x + size / 2;
        float centerY = y + size / 2;

        // Glow based on type
        Color glowColor = bonus.Type switch
        {
            Bonus.BonusType.Shield => Colors.Cyan.WithAlpha(0.3f),
            Bonus.BonusType.Magnet => Colors.Red.WithAlpha(0.3f),
            Bonus.BonusType.SlowMotion => Colors.Yellow.WithAlpha(0.3f),
            Bonus.BonusType.Multiplier => Colors.Green.WithAlpha(0.3f),
            Bonus.BonusType.CoinRain => Colors.Gold.WithAlpha(0.3f),
            _ => Colors.White.WithAlpha(0.3f)
        };

        canvas.FillColor = glowColor;
        canvas.FillCircle(centerX, centerY, size / 2 + 3);

        // Bonus body
        Color bodyColor = bonus.Type switch
        {
            Bonus.BonusType.Shield => Colors.Cyan,
            Bonus.BonusType.Magnet => Colors.Red,
            Bonus.BonusType.SlowMotion => Colors.Yellow,
            Bonus.BonusType.Multiplier => Colors.Green,
            Bonus.BonusType.CoinRain => Colors.Gold,
            _ => Colors.White
        };

        canvas.FillColor = bodyColor;
        canvas.FillCircle(centerX, centerY, size / 2);

        // Bonus symbol
        string symbol = bonus.Type switch
        {
            Bonus.BonusType.Shield => "🛡️",
            Bonus.BonusType.Magnet => "🧲",
            Bonus.BonusType.SlowMotion => "🐌",
            Bonus.BonusType.Multiplier => "x2",
            Bonus.BonusType.CoinRain => "💰",
            _ => "?"
        };

        var paint = new SolidPaint(Colors.White);
        canvas.SetFillPaint(paint, new RectF(x, y, size, size));
        canvas.FontSize = size * 0.5f;
        canvas.DrawString(symbol, x, y, size, size,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawFuelCan(ICanvas canvas, FuelCan fuelCan)
    {
        float x = fuelCan.X;
        float y = fuelCan.Y;
        float width = fuelCan.Width;
        float height = fuelCan.Height;

        // Highlight when fuel is low
        if (_gameState.Player.CurrentFuel < 30)
        {
            canvas.StrokeColor = Colors.Yellow;
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(x - 2, y - 2, width + 4, height + 4, 4);
        }

        // Canister body
        canvas.FillColor = Colors.Red;
        canvas.FillRoundedRectangle(x, y, width, height * 0.7f, 3);

        // Canister neck
        canvas.FillColor = Colors.DarkGray;
        canvas.FillRoundedRectangle(x + width * 0.4f, y - 5, width * 0.2f, 10, 2);

        // Fuel symbol
        var paint = new SolidPaint(Colors.White);
        canvas.SetFillPaint(paint, new RectF(x, y, width, height));
        canvas.FontSize = height * 0.4f;
        canvas.DrawString("⛽", x, y, width, height,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawHud(ICanvas canvas)
    {
        DrawScoreHud(canvas);
        DrawLivesHud(canvas);
        DrawActiveBonusesHud(canvas);
        DrawFuelIndicator(canvas);
    }

    private void DrawScoreHud(ICanvas canvas)
    {
        float padding = 15;
        float currentY = padding;

        // Score
        var scorePaint = new SolidPaint(Colors.White);
        canvas.SetFillPaint(scorePaint, new RectF(padding, currentY, 250, 30));
        canvas.FontSize = 22;
        DrawBoldText(canvas, $"SCORE: {_gameState.Score}", padding, currentY, 250, 30);
        currentY += 35;

        // High score
        int highScore = Preferences.Get("HighScore", 0);
        var bestPaint = new SolidPaint(Colors.Gold);
        canvas.SetFillPaint(bestPaint, new RectF(padding, currentY, 250, 25));
        canvas.FontSize = 18;
        DrawBoldText(canvas, $"BEST: {highScore}", padding, currentY, 250, 25);
        currentY += 30;

        // Coins
        var coinsPaint = new SolidPaint(Colors.LightYellow);
        canvas.SetFillPaint(coinsPaint, new RectF(padding, currentY, 250, 25));
        canvas.FontSize = 18;
        DrawBoldText(canvas, $"COINS: {_gameState.CoinsCollected}", padding, currentY, 250, 25);
        currentY += 30;

        // Car name
        var carPaint = new SolidPaint(_selectedCar.Color);
        canvas.SetFillPaint(carPaint, new RectF(padding, currentY, 250, 25));
        canvas.FontSize = 16;
        string carDisplayName = _selectedCar.Name == "CUSTOM" && _customImageLoaded ?
            "CUSTOM (Loaded)" : _selectedCar.Name;
        DrawBoldText(canvas, $"CAR: {carDisplayName}", padding, currentY, 250, 25);
    }

    private void DrawLivesHud(ICanvas canvas)
    {
        float rightX = _gameState.ScreenWidth - 180;
        float y = 20;
        float heartSize = 30; // Размер иконки сердечка

        if (_carImages.TryGetValue("Heart", out var heartImage) && heartImage != null)
        {
            for (int i = 0; i < _gameState.Lives; i++)
            {
                // Отрисовка картинки heart.png
                canvas.DrawImage(heartImage, rightX + (i * (heartSize + 5)), y, heartSize, heartSize);
            }
        }
        else
        {
            // Резервный вариант на случай, если картинка не загрузилась
            var heartPaint = new SolidPaint(Colors.Red);
            canvas.SetFillPaint(heartPaint, new RectF(rightX, y, 120, 30));
            canvas.FontSize = 28;
            for (int i = 0; i < _gameState.Lives; i++)
            {
                DrawBoldText(canvas, "❤️", rightX + (i * 35), y, 30, 30);
            }
        }
    }

    private void DrawActiveBonusesHud(ICanvas canvas)
    {
        float x = _gameState.ScreenWidth - 160;
        float y = 70;
        float iconSize = 28;
        float spacing = 8;
        float currentX = x;

        if (_gameState.IsShieldActive)
        {
            DrawBonusIcon(canvas, "🛡️", Colors.Cyan, currentX, y, iconSize);
            currentX += iconSize + spacing;
        }

        if (_gameState.IsMagnetActive)
        {
            DrawBonusIcon(canvas, "🧲", Colors.Red, currentX, y, iconSize);
            currentX += iconSize + spacing;
        }

        if (_gameState.IsSlowMotionActive)
        {
            DrawBonusIcon(canvas, "🐌", Colors.Yellow, currentX, y, iconSize);
            currentX += iconSize + spacing;
        }

        if (_gameState.IsMultiplierActive)
        {
            DrawBonusIcon(canvas, "x2", Colors.Green, currentX, y, iconSize);
        }
    }

    private void DrawBonusIcon(ICanvas canvas, string symbol, Color color, float x, float y, float size)
    {
        // Background
        canvas.FillColor = color.WithAlpha(0.2f);
        canvas.FillRoundedRectangle(x - 2, y - 2, size + 4, size + 4, 4);

        // Icon
        var paint = new SolidPaint(color);
        canvas.SetFillPaint(paint, new RectF(x, y, size, size));
        canvas.FontSize = size * 0.7f;
        canvas.DrawString(symbol, x, y, size, size,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawFuelIndicator(ICanvas canvas)
    {
        float width = 200;
        float height = 20;
        float x = (_gameState.ScreenWidth - width) / 2;
        float y = _gameState.ScreenHeight - 50;

        // Background
        canvas.FillColor = Color.FromArgb("#333333");
        canvas.FillRoundedRectangle(x, y, width, height, 10);

        // Fill
        float fillPercent = _gameState.Player.CurrentFuel / _gameState.Player.MaxFuel;
        float fillWidth = width * fillPercent;

        Color fillColor = fillPercent > 0.5f ? Colors.LimeGreen :
                         fillPercent > 0.2f ? Colors.Orange :
                         Colors.Red;

        canvas.FillColor = fillColor;
        canvas.FillRoundedRectangle(x, y, fillWidth, height, 10);

        // Border
        canvas.StrokeColor = Colors.White.WithAlpha(0.5f);
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(x, y, width, height, 10);

        // Text
        var paint = new SolidPaint(Colors.White);
        canvas.SetFillPaint(paint, new RectF(x, y + height + 5, width, 20));
        canvas.FontSize = 12;
        canvas.DrawString($"FUEL: {(int)_gameState.Player.CurrentFuel}/{(int)_gameState.Player.MaxFuel}",
            x, y + height + 5, width, 20,
            HorizontalAlignment.Center, VerticalAlignment.Top);
    }

    private void DrawGameStates(ICanvas canvas, RectF dirtyRect)
    {
        // Low fuel warning
        if (_gameState.Player.CurrentFuel < 30 && !_gameState.IsGameOver)
        {
            DrawLowFuelWarning(canvas, dirtyRect);
        }

        // Game over
        if (_gameState.IsGameOver)
        {
            DrawGameOver(canvas, dirtyRect);
        }
    }

    private void DrawLowFuelWarning(ICanvas canvas, RectF dirtyRect)
    {
        // Blinking effect
        bool show = (DateTime.Now.Millisecond / 500) % 2 == 0;
        if (!show) return;

        string text = _gameState.Player.CurrentFuel < 10 ? "⚠️ CRITICAL FUEL!" : "LOW FUEL!";
        Color color = _gameState.Player.CurrentFuel < 10 ? Colors.Red : Colors.Orange;

        var paint = new SolidPaint(color);
        canvas.SetFillPaint(paint, new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 100, 300, 60));
        canvas.FontSize = 28;

        // Shadow
        var shadowPaint = new SolidPaint(Colors.Black.WithAlpha(0.5f));
        canvas.SetFillPaint(shadowPaint, new RectF(dirtyRect.Center.X - 149, dirtyRect.Center.Y - 99, 300, 60));
        canvas.DrawString(text, dirtyRect.Center.X - 149, dirtyRect.Center.Y - 99, 300, 60,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Main text
        canvas.SetFillPaint(paint, new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 100, 300, 60));
        canvas.DrawString(text, dirtyRect.Center.X - 150, dirtyRect.Center.Y - 100, 300, 60,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawGameOver(ICanvas canvas, RectF dirtyRect)
    {
        // Dark overlay
        canvas.FillColor = Colors.Black.WithAlpha(0.7f);
        canvas.FillRectangle(dirtyRect);

        // "Game Over" text
        var textPaint = new SolidPaint(Colors.Red);
        canvas.SetFillPaint(textPaint, new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100));
        canvas.FontSize = 48;

        // Shadow
        var shadowPaint = new SolidPaint(Colors.Black.WithAlpha(0.5f));
        canvas.SetFillPaint(shadowPaint, new RectF(dirtyRect.Center.X - 149, dirtyRect.Center.Y - 49, 300, 100));
        canvas.DrawString("GAME OVER", dirtyRect.Center.X - 149, dirtyRect.Center.Y - 49, 300, 100,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Main text
        canvas.SetFillPaint(textPaint, new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100));
        canvas.DrawString("GAME OVER", dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Final score
        var scorePaint = new SolidPaint(Colors.White);
        canvas.SetFillPaint(scorePaint, new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y + 60, 300, 40));
        canvas.FontSize = 24;
        canvas.DrawString($"Score: {_gameState.Score}", dirtyRect.Center.X - 150, dirtyRect.Center.Y + 60, 300, 40,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawBoldText(ICanvas canvas, string text, float x, float y, float width, float height,
                              HorizontalAlignment hAlign = HorizontalAlignment.Left,
                              VerticalAlignment vAlign = VerticalAlignment.Top)
    {
        // Draw text with shadow for bold effect
        var shadowPaint = new SolidPaint(Colors.Black.WithAlpha(0.3f));
        canvas.SetFillPaint(shadowPaint, new RectF(x + 1, y + 1, width, height));
        canvas.DrawString(text, x + 1, y + 1, width, height, hAlign, vAlign);

        // Main text (color already set in calling method)
        canvas.DrawString(text, x, y, width, height, hAlign, vAlign);
    }
}