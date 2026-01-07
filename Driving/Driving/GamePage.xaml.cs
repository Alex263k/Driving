using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;
using Plugin.Maui.Audio;
using Driving.GameEngine;
using Driving.Models;

namespace Driving;

/// <summary>
/// Main game page logic. Handles game loop, collision detection, audio, and player input.
/// </summary>
public partial class GamePage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private readonly IDispatcherTimer _gameLoop;
    private readonly Random _random = new Random();
    private readonly StartPage.CarInfo _selectedCar;

    // Audio Players
    private IAudioPlayer _backgroundMusic;
    private IAudioPlayer _collectSound;
    private IAudioPlayer _powerUpSound;
    private IAudioPlayer _gameOverSound;
    private bool _isAudioLoaded = false;
    private bool _isMuted = false; // Added for mute support

    // Collision and movement settings
    private enum CollisionResult { None, SideCollision, FrontalCollision }
    private const float FrontalZoneDepth = 20f;
    private const int LaneChangeFrames = 10;
    private const float LeanAmountY = 15f;

    // Speed settings
    private const float BaseSpeed = 14f;
    private const float MaxSpeed = 45f;
    private const int SpeedIncreaseInterval = 250;
    private const float SpeedIncrement = 0.8f;

    /// <summary>
    /// Default constructor using the currently selected car.
    /// </summary>
    public GamePage() : this(StartPage.GetSelectedCar()) { }

    /// <summary>
    /// Constructor with a specific car selection.
    /// </summary>
    /// <param name="selectedCar">The car to use in this game session.</param>
    public GamePage(StartPage.CarInfo selectedCar)
    {
        InitializeComponent();
        _selectedCar = selectedCar;

        // Load mute state from preferences
        _isMuted = Preferences.Get("IsMuted", false);

        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState, _selectedCar);
        GameCanvas.Drawable = _gameDrawable;

        // Initialize the game loop at ~60 FPS
        _gameLoop = Dispatcher.CreateTimer();
        _gameLoop.Interval = TimeSpan.FromMilliseconds(16);
        _gameLoop.Tick += GameLoop_Tick;

        // Add a Tap Gesture to handle "Tap to Exit" after Game Over
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnCanvasTapped;
        GameCanvas.GestureRecognizers.Add(tapGesture);
    }

    /// <summary>
    /// Called when the page appears. Sets up audio and starts a new game.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SetupAudioAsync();
        StartNewGame();
    }

    /// <summary>
    /// Called when the page disappears. Stops the game loop and background music.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _gameLoop.Stop();
        _backgroundMusic?.Stop();
    }

    /// <summary>
    /// Loads all audio files asynchronously.
    /// </summary>
    private async Task SetupAudioAsync()
    {
        if (_isAudioLoaded) return;

        try
        {
            // Load engine background sound
            _backgroundMusic = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("Engine.wav"));
            _backgroundMusic.Loop = true;
            _backgroundMusic.Volume = _isMuted ? 0 : 0.5; // Respect mute state

            // Load SFX
            _collectSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("coin_collect.wav"));
            _collectSound.Volume = _isMuted ? 0 : 0.6; // Respect mute state

            _gameOverSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("game_over.wav"));
            _gameOverSound.Volume = _isMuted ? 0 : 0.7; // Respect mute state

            _powerUpSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("upgrade_success.wav"));
            _powerUpSound.Volume = _isMuted ? 0 : 0.6; // Respect mute state

            _isAudioLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AUDIO ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to play a sound with mute check.
    /// </summary>
    /// <param name="player">The audio player to use.</param>
    private void PlaySound(IAudioPlayer player)
    {
        if (player != null && !_isMuted)
        {
            if (player.IsPlaying) player.Stop();
            player.Play();
        }
    }

    /// <summary>
    /// Resets the game state and starts a new game session.
    /// </summary>
    private void StartNewGame()
    {
        _gameState.IsRunning = true;
        _gameState.IsGameOver = false;
        _gameState.Score = 0;
        _gameState.CoinsCollected = 0;
        _gameState.Lives = GetPlayerDurability();
        _gameState.Speed = BaseSpeed * GetPlayerSpeedMultiplier();
        _gameState.RoadMarkingOffset = 0;
        _gameState.InvulnerabilityFrames = 0;
        _gameState.IsFuelDepleted = false;

        // Clear all game entities
        _gameState.Enemies.Clear();
        _gameState.Collectibles.Clear();
        _gameState.Bonuses.Clear();
        _gameState.FuelCans.Clear();

        // Reset player position
        _gameState.Player.CurrentLane = 1;
        _gameState.Player.VisualX = 0;
        _gameState.Player.IsAnimating = false;

        // Apply fuel upgrades from player progress
        ApplyFuelUpgrades();

        // Start background music if not muted
        if (_backgroundMusic != null && !_isMuted)
        {
            _backgroundMusic.Play();
        }

        // Start the game loop
        _gameLoop.Start();
    }

    /// <summary>
    /// Main game loop that updates game state 60 times per second.
    /// </summary>
    private void GameLoop_Tick(object? sender, EventArgs e)
    {
        if (!_gameState.IsRunning || _gameState.IsGameOver) return;

        UpdateFuelSystem();
        if (_gameState.IsFuelDepleted) { EndGame("OUT OF FUEL!"); return; }

        UpdateGameSpeed();
        UpdatePlayerAnimation();

        // Update score based on multiplier
        _gameState.Score += _gameState.ScoreMultiplier;

        // Road animation logic
        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) _gameState.RoadMarkingOffset -= 60;

        UpdateActiveBonuses();
        if (_gameState.InvulnerabilityFrames > 0) _gameState.InvulnerabilityFrames--;

        UpdateEntities();

        // Redraw the UI
        GameCanvas.Invalidate();
    }

    /// <summary>
    /// Updates the game speed based on player upgrades and score progression.
    /// </summary>
    private void UpdateGameSpeed()
    {
        float baseSpeed = BaseSpeed * GetPlayerSpeedMultiplier();
        float scoreBonus = Math.Min(_gameState.Score / SpeedIncreaseInterval, 40f) * SpeedIncrement;
        _gameState.Speed = Math.Min(baseSpeed + scoreBonus, MaxSpeed);
    }

    /// <summary>
    /// Applies fuel tank and engine efficiency upgrades to the player's car.
    /// </summary>
    private void ApplyFuelUpgrades()
    {
        int tankLevel = Preferences.Get("FuelTankLevel", 1);
        _gameState.Player.MaxFuel = 100f + (tankLevel - 1) * 25f;
        _gameState.Player.CurrentFuel = _gameState.Player.MaxFuel;

        int engineLevel = Preferences.Get("EngineEfficiencyLevel", 1);
        _gameState.Player.FuelConsumptionRate = 0.1f * (1f - (engineLevel - 1) * 0.1f);
    }

    /// <summary>
    /// Updates the duration of active power-ups.
    /// </summary>
    private void UpdateActiveBonuses()
    {
        if (_gameState.ShieldFrames > 0 && --_gameState.ShieldFrames == 0) _gameState.IsShieldActive = false;
        if (_gameState.MagnetFrames > 0 && --_gameState.MagnetFrames == 0) _gameState.IsMagnetActive = false;
        if (_gameState.SlowMotionFrames > 0 && --_gameState.SlowMotionFrames == 0) _gameState.IsSlowMotionActive = false;
        if (_gameState.MultiplierFrames > 0 && --_gameState.MultiplierFrames == 0)
        {
            _gameState.IsMultiplierActive = false;
            _gameState.ScoreMultiplier = 1;
        }
    }

    /// <summary>
    /// Updates all game entities (enemies, collectibles, bonuses).
    /// </summary>
    private void UpdateEntities()
    {
        // Enemy Spawning
        if (++_gameState.EnemySpawnCounter >= GameState.EnemySpawnRate)
        {
            int lane = _random.Next(0, 3);
            _gameState.Enemies.Add(new Enemy(60, 100, lane, _gameState.Speed, GetRandomEnemyType()));
            _gameState.EnemySpawnCounter = 0;
        }

        // Enemy Update & Collision
        for (int i = _gameState.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _gameState.Enemies[i];
            enemy.Y += _gameState.IsSlowMotionActive ? enemy.Speed * 0.5f : enemy.Speed;

            if (_gameState.InvulnerabilityFrames == 0 && !_gameState.IsShieldActive)
            {
                var collision = CheckCarCollision(enemy, _gameState.Player);
                if (collision == CollisionResult.FrontalCollision) { EndGame("CRASHED!"); return; }
                else if (collision == CollisionResult.SideCollision)
                {
                    if (--_gameState.Lives <= 0) { EndGame("GAME OVER"); return; }
                    _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
                    _gameState.Enemies.RemoveAt(i);
                    continue;
                }
            }
            if (enemy.Y > _gameState.ScreenHeight) _gameState.Enemies.RemoveAt(i);
        }

        // Coins Spawning
        if (++_gameState.CollectibleSpawnCounter >= GameState.CollectibleSpawnRate)
        {
            _gameState.Collectibles.Add(new Collectible(40, 40, _random.Next(0, 3)));
            _gameState.CollectibleSpawnCounter = 0;
        }

        for (int i = _gameState.Collectibles.Count - 1; i >= 0; i--)
        {
            var coin = _gameState.Collectibles[i];
            coin.Y += _gameState.Speed;

            if (CheckCoinCollection(coin, _gameState.Player) || _gameState.IsMagnetActive)
            {
                _gameState.CoinsCollected++;
                _gameState.Score += 50;
                PlaySound(_collectSound);
                _gameState.Collectibles.RemoveAt(i);
            }
            else if (coin.Y > _gameState.ScreenHeight) _gameState.Collectibles.RemoveAt(i);
        }

        // Bonuses Spawning
        if (++_gameState.BonusSpawnCounter >= GameState.BonusSpawnRate)
        {
            _gameState.Bonuses.Add(new Bonus(50, 50, _random.Next(0, 3), GetRandomBonusType()));
            _gameState.BonusSpawnCounter = 0;
        }

        for (int i = _gameState.Bonuses.Count - 1; i >= 0; i--)
        {
            var bonus = _gameState.Bonuses[i];
            bonus.Y += _gameState.Speed;
            if (CheckBonusCollection(bonus, _gameState.Player))
            {
                ApplyBonus(bonus.Type);
                PlaySound(_powerUpSound);
                _gameState.Bonuses.RemoveAt(i);
            }
            else if (bonus.Y > _gameState.ScreenHeight) _gameState.Bonuses.RemoveAt(i);
        }
    }

    /// <summary>
    /// Updates the fuel system including consumption and fuel can collection.
    /// </summary>
    private void UpdateFuelSystem()
    {
        // Use time-based fuel consumption
        _gameState.FuelTimer += 0.016f;

        while (_gameState.FuelTimer >= _gameState.CurrentFuelConsumptionInterval)
        {
            // Reduce fuel
            _gameState.Player.CurrentFuel -= 1.5f;
            _gameState.FuelTimer -= _gameState.CurrentFuelConsumptionInterval;
        }

        // Check for empty tank
        if (_gameState.Player.CurrentFuel <= 0)
        {
            _gameState.Player.CurrentFuel = 0;
            _gameState.IsFuelDepleted = true;
            return;
        }

        // Fuel Can Spawning
        if (++_gameState.FuelCanSpawnCounter >= GameState.FuelCanSpawnRate)
        {
            _gameState.FuelCans.Add(new FuelCan(40, 60, _random.Next(0, 3)));
            _gameState.FuelCanSpawnCounter = 0;
        }

        // Fuel Collection
        for (int i = _gameState.FuelCans.Count - 1; i >= 0; i--)
        {
            var fuel = _gameState.FuelCans[i];
            fuel.Y += _gameState.Speed;

            if (CheckFuelCanCollection(fuel, _gameState.Player))
            {
                _gameState.Player.CurrentFuel = Math.Min(_gameState.Player.MaxFuel, _gameState.Player.CurrentFuel + 30f);
                PlaySound(_collectSound);
                _gameState.FuelCans.RemoveAt(i);
            }
            else if (fuel.Y > _gameState.ScreenHeight)
            {
                _gameState.FuelCans.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Returns a random enemy type.
    /// </summary>
    private Enemy.EnemyType GetRandomEnemyType() => (Enemy.EnemyType)_random.Next(0, 4);

    /// <summary>
    /// Returns a random bonus type.
    /// </summary>
    private Bonus.BonusType GetRandomBonusType() => (Bonus.BonusType)_random.Next(0, 5);

    /// <summary>
    /// Ends the current game session with the specified title.
    /// </summary>
    /// <param name="title">The game over title/message.</param>
    private void EndGame(string title)
    {
        _gameState.IsRunning = false;
        _gameState.IsGameOver = true;
        _gameLoop.Stop();
        _backgroundMusic?.Stop();
        PlaySound(_gameOverSound);

        // Save Highscore and Currency
        Preferences.Set("HighScore", Math.Max(Preferences.Get("HighScore", 0), _gameState.Score));
        Preferences.Set("TotalCoins", Preferences.Get("TotalCoins", 0) + _gameState.CoinsCollected);

        // Force a final redraw to show the Game Over overlay (handled in GameDrawable)
        GameCanvas.Invalidate();
    }

    /// <summary>
    /// Handles canvas tap gestures (used to exit after game over).
    /// </summary>
    private void OnCanvasTapped(object? sender, TappedEventArgs e)
    {
        // If the game is over, any tap on the screen returns the user to the previous page
        if (_gameState.IsGameOver)
        {
            Navigation.PopAsync();
        }
    }

    /// <summary>
    /// Updates the player's lane change animation.
    /// </summary>
    private void UpdatePlayerAnimation()
    {
        if (!_gameState.Player.IsAnimating) return;

        float progress = 1f - ((float)_gameState.Player.AnimationFramesRemaining / LaneChangeFrames);
        float eased = 0.5f - 0.5f * (float)Math.Cos(progress * Math.PI);

        _gameState.Player.VisualX = _gameState.Player.StartX + (_gameState.Player.TargetX - _gameState.Player.StartX) * eased;
        _gameState.Player.VisualY = (_gameState.ScreenHeight - 150) - (4 * progress * (1 - progress) * LeanAmountY);

        if (--_gameState.Player.AnimationFramesRemaining <= 0) _gameState.Player.IsAnimating = false;
    }

    /// <summary>
    /// Checks for collision between an enemy and the player.
    /// </summary>
    /// <returns>The type of collision that occurred.</returns>
    private CollisionResult CheckCarCollision(Enemy e, Player p)
    {
        float pY = _gameState.ScreenHeight - 150;
        float pX = p.CalculateLaneX(_gameState.ScreenWidth);
        float eX = e.CalculateX(_gameState.ScreenWidth);

        if (pX < eX + e.Width && pX + p.Width > eX && pY < e.Y + e.Height && pY + p.Height > e.Y)
            return (e.Y + e.Height < pY + FrontalZoneDepth) ? CollisionResult.FrontalCollision : CollisionResult.SideCollision;

        return CollisionResult.None;
    }

    /// <summary>
    /// Checks if a coin was collected by the player.
    /// </summary>
    private bool CheckCoinCollection(Collectible c, Player p) => GenericCollision(c.CalculateX(_gameState.ScreenWidth), c.Y, c.Width, c.Height);

    /// <summary>
    /// Checks if a bonus was collected by the player.
    /// </summary>
    private bool CheckBonusCollection(Bonus b, Player p) => GenericCollision(b.CalculateX(_gameState.ScreenWidth), b.Y, b.Width, b.Height);

    /// <summary>
    /// Checks if a fuel can was collected by the player.
    /// </summary>
    private bool CheckFuelCanCollection(FuelCan f, Player p) => GenericCollision(f.CalculateX(_gameState.ScreenWidth), f.Y, f.Width, f.Height);

    /// <summary>
    /// Generic collision detection between the player and any rectangular entity.
    /// </summary>
    private bool GenericCollision(float x, float y, float w, float h) =>
        _gameState.Player.VisualX < x + w && _gameState.Player.VisualX + 60 > x &&
        _gameState.Player.VisualY < y + h && _gameState.Player.VisualY + 100 > y;

    /// <summary>
    /// Applies the effect of a collected bonus to the game state.
    /// </summary>
    private void ApplyBonus(Bonus.BonusType type)
    {
        switch (type)
        {
            case Bonus.BonusType.Shield:
                _gameState.IsShieldActive = true;
                _gameState.ShieldFrames = GameState.ShieldDuration;
                break;
            case Bonus.BonusType.Magnet:
                _gameState.IsMagnetActive = true;
                _gameState.MagnetFrames = GameState.MagnetDuration;
                break;
            case Bonus.BonusType.SlowMotion:
                _gameState.IsSlowMotionActive = true;
                _gameState.SlowMotionFrames = GameState.SlowMotionDuration;
                break;
            case Bonus.BonusType.Multiplier:
                _gameState.IsMultiplierActive = true;
                _gameState.MultiplierFrames = GameState.MultiplierDuration;
                _gameState.ScoreMultiplier = 2;
                break;
        }
    }

    /// <summary>
    /// Handles swipe gestures for lane changes.
    /// </summary>
    private void OnSwiped(object? sender, SwipedEventArgs e)
    {
        if (!_gameState.IsRunning || _gameState.Player.IsAnimating || _gameState.IsGameOver) return;

        int lane = _gameState.Player.CurrentLane;
        if (e.Direction == SwipeDirection.Left) lane--;
        else if (e.Direction == SwipeDirection.Right) lane++;

        if (lane < 0 || lane > 2) return;

        _gameState.Player.CurrentLane = lane;
        _gameState.Player.StartX = _gameState.Player.VisualX;
        _gameState.Player.TargetX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
        _gameState.Player.IsAnimating = true;
        _gameState.Player.AnimationFramesRemaining = LaneChangeFrames;
    }

    /// <summary>
    /// Gets the player's durability level from upgrades.
    /// </summary>
    private int GetPlayerDurability() => Preferences.Get("DurabilityLevel", 1);

    /// <summary>
    /// Gets the player's speed multiplier from upgrades.
    /// </summary>
    private float GetPlayerSpeedMultiplier() => 1.0f + (Preferences.Get("SpeedLevel", 1) - 1) * 0.1f;
}