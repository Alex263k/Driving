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

/// <summary>
/// Core rendering engine for the game, implementing MAUI's IDrawable.
/// Handles high-performance 2D drawing of the road, vehicles, and HUD.
/// </summary>
public class GameDrawable : IDrawable
{
    private readonly GameState _gameState;
    private readonly StartPage.CarInfo _selectedCar;
    private readonly Dictionary<string, Microsoft.Maui.Graphics.IImage> _carImages = new();
    private readonly Dictionary<string, RectF> _hudElements = new();

    // State flags for asset management
    private bool _imagesLoaded = false;
    private bool _loadingImages = false;
    private Microsoft.Maui.Graphics.IImage? _customPlayerImage = null;
    private bool _customImageLoaded = false;

    /// <summary>
    /// Relative sizing for different vehicle classes to ensure visual variety.
    /// Format: (Width Scale, Height/Length Scale)
    /// </summary>
    private readonly Dictionary<Enemy.EnemyType, (float widthMultiplier, float heightMultiplier)> _enemySizeMultipliers = new()
    {
        { Enemy.EnemyType.RegularCar, (1.5f, 1.5f) },   // Standard car: Uniform 50% increase
        { Enemy.EnemyType.Truck, (1.5f, 2.0f) },        // Bus/Truck: Standard width but significantly longer
        { Enemy.EnemyType.Motorcycle, (1.3f, 1.5f) },   // Motorcycle: Slender profile
        { Enemy.EnemyType.Police, (1.5f, 1.5f) }        // Police cruiser: High visibility scale
    };

    // Constant scaling factor for the player's vehicle
    private const float PLAYER_SIZE_MULTIPLIER = 1.5f;

    public GameDrawable(GameState state, StartPage.CarInfo selectedCar)
    {
        _gameState = state;
        _selectedCar = selectedCar;

        // Initialize background tasks for asset loading to avoid blocking the UI thread
        Task.Run(() => LoadCarImagesAsync());

        // Process external image if the user selected a custom skin
        if (selectedCar.Name == "CUSTOM" && !string.IsNullOrEmpty(selectedCar.CustomImagePath))
        {
            Task.Run(() => LoadCustomImageAsync(selectedCar.CustomImagePath));
        }
    }

    /// <summary>
    /// Loads a user-provided image file from the device storage.
    /// </summary>
    private async Task LoadCustomImageAsync(string imagePath)
    {
        try
        {
            Debug.WriteLine($"=== LOADING CUSTOM SKIN ===");
            Debug.WriteLine($"Path: {imagePath}");

            if (File.Exists(imagePath))
            {
                using var stream = File.OpenRead(imagePath);
                // Convert file stream to MAUI platform-specific image
                _customPlayerImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                _customImageLoaded = true;
                Debug.WriteLine($"✓ Custom skin loaded successfully: {_customPlayerImage?.Width}x{_customPlayerImage?.Height}");
            }
            else
            {
                Debug.WriteLine($"✗ Custom skin file not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR loading custom skin: {ex.Message}");
            _customPlayerImage = null;
        }
    }

    /// <summary>
    /// Loads all standard game sprites from embedded assembly resources.
    /// </summary>
    private async Task LoadCarImagesAsync()
    {
        if (_imagesLoaded || _loadingImages) return;
        _loadingImages = true;

        try
        {
            Debug.WriteLine("=== STARTING SPRITE LOADING ===");
            var assembly = Assembly.GetExecutingAssembly();

            // Mapping of internal keys to physical filenames in Resources/Images
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

            // Map enemy sprites to the loaded car images
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

    /// <summary>
    /// Attempts to find an embedded resource using several common path conventions.
    /// </summary>
    private async Task LoadImageAsync(Assembly assembly, string key, string imageName)
    {
        try
        {
            // Resource paths can vary depending on project structure/namespacing
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
                            return;
                        }
                    }
                }
                catch { /* Continue trying other paths */ }
            }
            _carImages[key] = null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Critical error for {key}: {ex.Message}");
            _carImages[key] = null;
        }
    }

    /// <summary>
    /// Main render loop called by the MAUI GraphicsView.
    /// </summary>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Sync engine state with actual UI dimensions
        _gameState.ScreenWidth = dirtyRect.Width;
        _gameState.ScreenHeight = dirtyRect.Height;

        // 2. Initialize player starting position if not yet set
        if (_gameState.IsRunning && _gameState.Player.VisualX == 0 && _gameState.ScreenWidth > 0)
        {
            _gameState.Player.VisualX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.StartX = _gameState.Player.VisualX;
            _gameState.Player.VisualY = _gameState.ScreenHeight - 150;
        }

        _hudElements.Clear();

        // 3. Layered Drawing Order:
        DrawBackground(canvas, dirtyRect);     // Layer 0: Tarmac
        DrawRoadMarkings(canvas, dirtyRect);  // Layer 1: Lane dividers
        DrawGameEntities(canvas);             // Layer 2: NPCs, Power-ups, Coins
        DrawPlayer(canvas);                   // Layer 3: Controlled vehicle
        DrawHud(canvas);                      // Layer 4: UI overlays
        DrawGameStates(canvas, dirtyRect);    // Layer 5: Warnings and Game Over screens
    }

    private void DrawBackground(ICanvas canvas, RectF dirtyRect)
    {
        // Draw the dark grey asphalt
        canvas.FillColor = Color.FromArgb("#2F4F4F");
        canvas.FillRectangle(dirtyRect);
    }

    /// <summary>
    /// Renders moving lane markers and road borders to simulate speed.
    /// </summary>
    private void DrawRoadMarkings(ICanvas canvas, RectF dirtyRect)
    {
        float dashLength = 20;
        float gapLength = 20;
        float totalLength = dashLength + gapLength;

        float laneWidth = dirtyRect.Width / 3f;
        canvas.StrokeColor = Colors.White.WithAlpha(0.8f);
        canvas.StrokeSize = 3;

        // Render vertical lane dividers
        for (int i = 1; i < 3; i++)
        {
            float x = laneWidth * i;
            // The RoadMarkingOffset is updated in GameState to create movement
            for (float y = _gameState.RoadMarkingOffset; y < dirtyRect.Height; y += totalLength)
            {
                if (y + dashLength > 0)
                {
                    float startY = Math.Max(y, 0);
                    float endY = Math.Min(y + dashLength, dirtyRect.Height);
                    if (startY < endY) canvas.DrawLine(x, startY, x, endY);
                }
            }
        }

        // Render yellow safety lines on the left/right edges
        canvas.StrokeColor = Colors.Yellow;
        canvas.StrokeSize = 2;

        for (float y = _gameState.RoadMarkingOffset; y < dirtyRect.Height; y += totalLength)
        {
            if (y + dashLength > 0)
            {
                float startY = Math.Max(y, 0);
                float endY = Math.Min(y + dashLength, dirtyRect.Height);
                if (startY < endY)
                {
                    canvas.DrawLine(5, startY, 5, endY);
                    canvas.DrawLine(dirtyRect.Width - 5, startY, dirtyRect.Width - 5, endY);
                }
            }
        }
    }

    private void DrawGameEntities(ICanvas canvas)
    {
        // Update coordinates and render all active game objects
        foreach (var enemy in _gameState.Enemies)
        {
            enemy.X = enemy.CalculateX(_gameState.ScreenWidth);
            DrawEnemy(canvas, enemy);
        }

        foreach (var collectible in _gameState.Collectibles)
        {
            collectible.X = collectible.CalculateX(_gameState.ScreenWidth);
            DrawCollectible(canvas, collectible);
        }

        foreach (var bonus in _gameState.Bonuses)
        {
            bonus.X = bonus.CalculateX(_gameState.ScreenWidth);
            DrawBonus(canvas, bonus);
        }

        foreach (var fuelCan in _gameState.FuelCans)
        {
            fuelCan.X = fuelCan.CalculateX(_gameState.ScreenWidth);
            DrawFuelCan(canvas, fuelCan);
        }
    }

    /// <summary>
    /// Renders an enemy vehicle with correct orientation (facing towards the player).
    /// </summary>
    private void DrawEnemy(ICanvas canvas, Enemy enemy)
    {
        var multipliers = _enemySizeMultipliers[enemy.Type];
        float drawWidth = enemy.Width * multipliers.widthMultiplier;
        float drawHeight = enemy.Height * multipliers.heightMultiplier;

        string imageKey = GetEnemyImageKey(enemy.Type);

        if (_carImages.TryGetValue(imageKey, out var image) && image != null)
        {
            try
            {
                canvas.SaveState();
                // Pivot around the center of the entity
                canvas.Translate(enemy.X + enemy.Width / 2, enemy.Y + enemy.Height / 2);
                // Enemies face south (180 degrees)
                canvas.Rotate(180);
                canvas.DrawImage(image, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight);
                canvas.RestoreState();
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing enemy sprite: {ex.Message}");
            }
        }

        // Render geometry if the sprite fails to load
        DrawEnemyFallback(canvas, enemy, drawWidth, drawHeight);
    }

    private string GetEnemyImageKey(Enemy.EnemyType type) => type switch
    {
        Enemy.EnemyType.RegularCar => "EnemyBlue",
        Enemy.EnemyType.Truck => "EnemyBus",
        Enemy.EnemyType.Motorcycle => "EnemyGreen",
        Enemy.EnemyType.Police => "EnemyPolice",
        _ => "EnemyBlue"
    };

    private void DrawEnemyFallback(ICanvas canvas, Enemy enemy, float drawWidth, float drawHeight)
    {
        Color color = enemy.Type switch
        {
            Enemy.EnemyType.RegularCar => Colors.Red,
            Enemy.EnemyType.Truck => Colors.DarkBlue,
            Enemy.EnemyType.Motorcycle => Colors.DarkRed,
            Enemy.EnemyType.Police => Colors.Blue,
            _ => Colors.Red
        };

        canvas.SaveState();
        canvas.Translate(enemy.X + enemy.Width / 2, enemy.Y + enemy.Height / 2);
        canvas.Rotate(180);
        DrawCarShape(canvas, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight, color);
        canvas.RestoreState();
    }

    private void DrawPlayer(ICanvas canvas)
    {
        if (!_gameState.IsRunning) return;

        float x = _gameState.Player.VisualX;
        float y = _gameState.Player.VisualY;
        float width = _gameState.Player.Width;
        float height = _gameState.Player.Height;

        // Handle blinking effect when player is recently hit (invulnerable)
        if (_gameState.InvulnerabilityFrames > 0 && _gameState.InvulnerabilityFrames % 4 == 0)
        {
            return;
        }

        float drawWidth = width * PLAYER_SIZE_MULTIPLIER;
        float drawHeight = height * PLAYER_SIZE_MULTIPLIER;
        float offsetX = (drawWidth - width) / 2;
        float offsetY = (drawHeight - height) / 2;

        // Render Custom Skin if active
        if (_selectedCar.Name == "CUSTOM" && _customImageLoaded && _customPlayerImage != null)
        {
            try
            {
                canvas.DrawImage(_customPlayerImage, x - offsetX, y - offsetY, drawWidth, drawHeight);
                return;
            }
            catch { /* Fallback */ }
        }

        // Render Selected Preset Skin
        string imageKey = GetPlayerImageKey(_selectedCar.Name);
        if (_carImages.TryGetValue(imageKey, out var image) && image != null)
        {
            try
            {
                canvas.DrawImage(image, x - offsetX, y - offsetY, drawWidth, drawHeight);
                return;
            }
            catch { /* Fallback */ }
        }

        // Last resort: Procedural drawing
        DrawCarShape(canvas, x - offsetX, y - offsetY, drawWidth, drawHeight, _selectedCar.Color);
    }

    private string GetPlayerImageKey(string carName) => carName switch
    {
        "BASIC" => "Blue",
        "SPORTS" => "Green",
        "POLICE" => "Police",
        "TAXI" => "Taxi",
        "RACING" => "LightBlue",
        "VIP" => "Blue",
        "CUSTOM" => "Blue",
        _ => "Blue"
    };

    /// <summary>
    /// Procedurally draws a car using basic shapes (used as fallback or for geometry effects).
    /// </summary>
    private void DrawCarShape(ICanvas canvas, float x, float y, float width, float height, Color color)
    {
        // Chassis
        canvas.FillColor = color;
        canvas.FillRoundedRectangle(x, y, width, height, 8);

        // Windshield
        canvas.FillColor = Colors.LightSkyBlue.WithAlpha(0.8f);
        canvas.FillRoundedRectangle(x + width * 0.1f, y + height * 0.1f, width * 0.8f, height * 0.25f, 4);

        // Front Headlights
        canvas.FillColor = Colors.Yellow;
        canvas.FillCircle(x + 5, y + height - 5, 4);
        canvas.FillCircle(x + width - 5, y + height - 5, 4);

        // Rear Taillights
        canvas.FillColor = Colors.Red;
        canvas.FillCircle(x + 5, y + 5, 4);
        canvas.FillCircle(x + width - 5, y + 5, 4);
    }

    private void DrawCollectible(ICanvas canvas, Collectible collectible)
    {
        float x = collectible.X;
        float y = collectible.Y;
        float size = collectible.Width;

        // Outer glow
        canvas.FillColor = Colors.Gold.WithAlpha(0.3f);
        canvas.FillCircle(x + size / 2, y + size / 2, size / 2 + 2);

        // Main coin face
        canvas.FillColor = Colors.Gold;
        canvas.FillCircle(x + size / 2, y + size / 2, size / 2);

        // Currency symbol
        var paint = new SolidPaint(Colors.DarkGoldenrod);
        canvas.SetFillPaint(paint, new RectF(x, y, size, size));
        canvas.FontSize = size * 0.6f;
        canvas.DrawString("$", x, y, size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawBonus(ICanvas canvas, Bonus bonus)
    {
        float size = bonus.Width;
        float centerX = bonus.X + size / 2;
        float centerY = bonus.Y + size / 2;

        // Dynamic color selection based on bonus effect
        Color themeColor = bonus.Type switch
        {
            Bonus.BonusType.Shield => Colors.Cyan,
            Bonus.BonusType.Magnet => Colors.Red,
            Bonus.BonusType.SlowMotion => Colors.Yellow,
            Bonus.BonusType.Multiplier => Colors.Green,
            Bonus.BonusType.CoinRain => Colors.Gold,
            _ => Colors.White
        };

        // Ambient glow
        canvas.FillColor = themeColor.WithAlpha(0.3f);
        canvas.FillCircle(centerX, centerY, size / 2 + 3);

        // Bonus orb
        canvas.FillColor = themeColor;
        canvas.FillCircle(centerX, centerY, size / 2);

        // Visual icon
        string symbol = bonus.Type switch
        {
            Bonus.BonusType.Shield => "🛡️",
            Bonus.BonusType.Magnet => "🧲",
            Bonus.BonusType.SlowMotion => "🐌",
            Bonus.BonusType.Multiplier => "x2",
            Bonus.BonusType.CoinRain => "💰",
            _ => "?"
        };

        canvas.SetFillPaint(new SolidPaint(Colors.White), new RectF(bonus.X, bonus.Y, size, size));
        canvas.FontSize = size * 0.5f;
        canvas.DrawString(symbol, bonus.X, bonus.Y, size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawFuelCan(ICanvas canvas, FuelCan fuelCan)
    {
        // Add a stroke if fuel is dangerously low to draw player's attention
        if (_gameState.Player.CurrentFuel < 30)
        {
            canvas.StrokeColor = Colors.Yellow;
            canvas.StrokeSize = 2;
            canvas.DrawRoundedRectangle(fuelCan.X - 2, fuelCan.Y - 2, fuelCan.Width + 4, fuelCan.Height + 4, 4);
        }

        // Red jerrycan body
        canvas.FillColor = Colors.Red;
        canvas.FillRoundedRectangle(fuelCan.X, fuelCan.Y, fuelCan.Width, fuelCan.Height * 0.7f, 3);

        // Cap detail
        canvas.FillColor = Colors.DarkGray;
        canvas.FillRoundedRectangle(fuelCan.X + fuelCan.Width * 0.4f, fuelCan.Y - 5, fuelCan.Width * 0.2f, 10, 2);

        canvas.SetFillPaint(new SolidPaint(Colors.White), new RectF(fuelCan.X, fuelCan.Y, fuelCan.Width, fuelCan.Height));
        canvas.FontSize = fuelCan.Height * 0.4f;
        canvas.DrawString("⛽", fuelCan.X, fuelCan.Y, fuelCan.Width, fuelCan.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawHud(ICanvas canvas)
    {
        DrawScoreHud(canvas);
        DrawLivesHud(canvas);
        DrawActiveBonusesHud(canvas);
        DrawFuelIndicator(canvas);
    }

    /// <summary>
    /// Renders text stats (Score, High Score, Coins) in the top-left.
    /// </summary>
    private void DrawScoreHud(ICanvas canvas)
    {
        float padding = 15;
        float currentY = padding;

        canvas.FontSize = 22;
        canvas.SetFillPaint(new SolidPaint(Colors.White), new RectF(padding, currentY, 250, 30));
        DrawBoldText(canvas, $"SCORE: {_gameState.Score}", padding, currentY, 250, 30);

        currentY += 35;
        int highScore = Preferences.Get("HighScore", 0);
        canvas.FontSize = 18;
        canvas.SetFillPaint(new SolidPaint(Colors.Gold), new RectF(padding, currentY, 250, 25));
        DrawBoldText(canvas, $"BEST: {highScore}", padding, currentY, 250, 25);

        currentY += 30;
        canvas.SetFillPaint(new SolidPaint(Colors.LightYellow), new RectF(padding, currentY, 250, 25));
        DrawBoldText(canvas, $"COINS: {_gameState.CoinsCollected}", padding, currentY, 250, 25);

        currentY += 30;
        canvas.SetFillPaint(new SolidPaint(_selectedCar.Color), new RectF(padding, currentY, 250, 25));
        canvas.FontSize = 16;
        string carDisplayName = (_selectedCar.Name == "CUSTOM" && _customImageLoaded) ? "CUSTOM (Loaded)" : _selectedCar.Name;
        DrawBoldText(canvas, $"CAR: {carDisplayName}", padding, currentY, 250, 25);
    }

    /// <summary>
    /// Renders remaining health (lives) using heart icons in the top-right.
    /// </summary>
    private void DrawLivesHud(ICanvas canvas)
    {
        float rightX = _gameState.ScreenWidth - 180;
        float y = 20;
        float heartSize = 30;

        if (_carImages.TryGetValue("Heart", out var heartImage) && heartImage != null)
        {
            for (int i = 0; i < _gameState.Lives; i++)
            {
                canvas.DrawImage(heartImage, rightX + (i * (heartSize + 5)), y, heartSize, heartSize);
            }
        }
        else
        {
            // Emoji fallback if the heart image isn't available
            canvas.FontSize = 28;
            for (int i = 0; i < _gameState.Lives; i++)
            {
                DrawBoldText(canvas, "❤️", rightX + (i * 35), y, 30, 30);
            }
        }
    }

    /// <summary>
    /// Shows active power-up icons near the top-right.
    /// </summary>
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
        canvas.FillColor = color.WithAlpha(0.2f);
        canvas.FillRoundedRectangle(x - 2, y - 2, size + 4, size + 4, 4);
        canvas.SetFillPaint(new SolidPaint(color), new RectF(x, y, size, size));
        canvas.FontSize = size * 0.7f;
        canvas.DrawString(symbol, x, y, size, size, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    /// Progress bar at the bottom showing remaining fuel and depletion levels.
    /// </summary>
    private void DrawFuelIndicator(ICanvas canvas)
    {
        float width = 200;
        float height = 20;
        float x = (_gameState.ScreenWidth - width) / 2;
        float y = _gameState.ScreenHeight - 50;

        // Container
        canvas.FillColor = Color.FromArgb("#333333");
        canvas.FillRoundedRectangle(x, y, width, height, 10);

        // Calculation of bar color and width
        float fillPercent = _gameState.Player.CurrentFuel / _gameState.Player.MaxFuel;
        float fillWidth = width * fillPercent;
        Color fillColor = fillPercent > 0.5f ? Colors.LimeGreen : fillPercent > 0.2f ? Colors.Orange : Colors.Red;

        canvas.FillColor = fillColor;
        canvas.FillRoundedRectangle(x, y, fillWidth, height, 10);

        // Text label
        canvas.SetFillPaint(new SolidPaint(Colors.White), new RectF(x, y + height + 5, width, 20));
        canvas.FontSize = 12;
        canvas.DrawString($"FUEL: {(int)_gameState.Player.CurrentFuel}/{(int)_gameState.Player.MaxFuel}",
            x, y + height + 5, width, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
    }

    private void DrawGameStates(ICanvas canvas, RectF dirtyRect)
    {
        if (_gameState.Player.CurrentFuel < 30 && !_gameState.IsGameOver)
            DrawLowFuelWarning(canvas, dirtyRect);

        if (_gameState.IsGameOver)
            DrawGameOver(canvas, dirtyRect);
    }

    /// <summary>
    /// Renders a blinking alert when fuel is critical.
    /// </summary>
    private void DrawLowFuelWarning(ICanvas canvas, RectF dirtyRect)
    {
        // Toggle visibility every 500ms
        if ((DateTime.Now.Millisecond / 500) % 2 != 0) return;

        bool isCritical = _gameState.Player.CurrentFuel < 10;
        string text = isCritical ? "⚠️ CRITICAL FUEL!" : "LOW FUEL!";
        Color color = isCritical ? Colors.Red : Colors.Orange;

        // Draw shadow then text for readability
        canvas.SetFillPaint(new SolidPaint(Colors.Black.WithAlpha(0.5f)), new RectF(dirtyRect.Center.X - 149, dirtyRect.Center.Y - 99, 300, 60));
        canvas.FontSize = 28;
        canvas.DrawString(text, dirtyRect.Center.X - 149, dirtyRect.Center.Y - 99, 300, 60, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.SetFillPaint(new SolidPaint(color), new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 100, 300, 60));
        canvas.DrawString(text, dirtyRect.Center.X - 150, dirtyRect.Center.Y - 100, 300, 60, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    /// Overlay shown when player loses all lives or runs out of fuel.
    /// </summary>
    private void DrawGameOver(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.7f);
        canvas.FillRectangle(dirtyRect);

        // Header
        canvas.FontSize = 48;
        canvas.SetFillPaint(new SolidPaint(Colors.Black.WithAlpha(0.5f)), new RectF(dirtyRect.Center.X - 149, dirtyRect.Center.Y - 49, 300, 100));
        canvas.DrawString("GAME OVER", dirtyRect.Center.X - 149, dirtyRect.Center.Y - 49, 300, 100, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.SetFillPaint(new SolidPaint(Colors.Red), new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100));
        canvas.DrawString("GAME OVER", dirtyRect.Center.X - 150, dirtyRect.Center.Y - 50, 300, 100, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Final score display
        canvas.FontSize = 24;
        canvas.SetFillPaint(new SolidPaint(Colors.White), new RectF(dirtyRect.Center.X - 150, dirtyRect.Center.Y + 60, 300, 40));
        canvas.DrawString($"Score: {_gameState.Score}", dirtyRect.Center.X - 150, dirtyRect.Center.Y + 60, 300, 40, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    /// Utility to simulate bold text by rendering a faint shadow offset.
    /// </summary>
    private void DrawBoldText(ICanvas canvas, string text, float x, float y, float width, float height,
                             HorizontalAlignment hAlign = HorizontalAlignment.Left,
                             VerticalAlignment vAlign = VerticalAlignment.Top)
    {
        canvas.SaveState();
        var shadowPaint = new SolidPaint(Colors.Black.WithAlpha(0.3f));
        canvas.SetFillPaint(shadowPaint, new RectF(x + 1, y + 1, width, height));
        canvas.DrawString(text, x + 1, y + 1, width, height, hAlign, vAlign);
        canvas.RestoreState();

        canvas.DrawString(text, x, y, width, height, hAlign, vAlign);
    }
}