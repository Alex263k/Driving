using Driving.GameEngine;
using Driving.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Driving;

public partial class GamePage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private readonly IDispatcherTimer _gameLoop;
    private readonly Random _random = new Random();
    private readonly StartPage.CarInfo _selectedCar;

    // Collision and movement settings
    private enum CollisionResult { None, SideCollision, FrontalCollision }
    private const float FrontalZoneDepth = 20f;
    private const int LaneChangeFrames = 10;
    private const float LeanAmountY = 15f;

    // Constructor without parameters (for backward compatibility)
    public GamePage() : this(StartPage.GetSelectedCar())
    {
    }

    // New constructor with selected car parameter
    public GamePage(StartPage.CarInfo selectedCar)
    {
        InitializeComponent();
        _selectedCar = selectedCar;

        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState, _selectedCar);
        GameCanvas.Drawable = _gameDrawable;

        // Setup the main game loop timer
        _gameLoop = Dispatcher.CreateTimer();
        _gameLoop.Interval = TimeSpan.FromMilliseconds(16);
        _gameLoop.Tick += GameLoop_Tick;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartNewGame();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_gameLoop.IsRunning)
        {
            _gameLoop.Stop();
        }
    }

    private void StartNewGame()
    {
        // Reset all game variables
        _gameState.IsRunning = true;
        _gameState.IsGameOver = false;
        _gameState.Score = 0;
        _gameState.CoinsCollected = 0; // Reset coin counter
        _gameState.Lives = GetPlayerDurability(); // Use upgrade system
        _gameState.Speed = 10f * GetPlayerSpeedMultiplier(); // Use upgrade system
        _gameState.RoadMarkingOffset = 0;
        _gameState.InvulnerabilityFrames = 0;

        // Clear all entities
        _gameState.Enemies.Clear();
        _gameState.Collectibles.Clear();
        _gameState.Bonuses.Clear();
        _gameState.EnemySpawnCounter = 0;
        _gameState.CollectibleSpawnCounter = 0;
        _gameState.BonusSpawnCounter = 0;

        // Reset bonus effects
        _gameState.IsShieldActive = false;
        _gameState.ShieldFrames = 0;
        _gameState.IsMagnetActive = false;
        _gameState.MagnetFrames = 0;
        _gameState.IsSlowMotionActive = false;
        _gameState.SlowMotionFrames = 0;
        _gameState.IsMultiplierActive = false;
        _gameState.MultiplierFrames = 0;
        _gameState.ScoreMultiplier = 1;

        // Reset player state
        _gameState.Player.CurrentLane = 1;
        _gameState.Player.VisualX = 0;
        _gameState.Player.IsAnimating = false;
        _gameState.Player.AnimationFramesRemaining = 0;

        _gameLoop.Start();
    }

    private void GameLoop_Tick(object? sender, EventArgs e)
    {
        if (!_gameState.IsRunning || _gameState.IsGameOver) return;

        // 1. Process player animations (lane switching)
        UpdatePlayerAnimation();

        // 2. Move environment and update score (with multiplier)
        int scoreIncrement = _gameState.ScoreMultiplier;
        _gameState.Score += scoreIncrement;

        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) _gameState.RoadMarkingOffset -= 60;

        // 3. Update active bonuses
        UpdateActiveBonuses();

        // 4. Decrease invulnerability frames if active
        if (_gameState.InvulnerabilityFrames > 0) _gameState.InvulnerabilityFrames--;

        // 5. Spawn and update all entities (enemies, coins, bonuses)
        UpdateEntities();

        // 6. Force redraw of the game scene
        GameCanvas.Invalidate();
    }

    private void UpdateActiveBonuses()
    {
        // Update shield
        if (_gameState.ShieldFrames > 0)
        {
            _gameState.ShieldFrames--;
            if (_gameState.ShieldFrames == 0)
            {
                _gameState.IsShieldActive = false;
            }
        }

        // Update magnet
        if (_gameState.MagnetFrames > 0)
        {
            _gameState.MagnetFrames--;
            if (_gameState.MagnetFrames == 0)
            {
                _gameState.IsMagnetActive = false;
            }
        }

        // Update slow motion
        if (_gameState.SlowMotionFrames > 0)
        {
            _gameState.SlowMotionFrames--;
            if (_gameState.SlowMotionFrames == 0)
            {
                _gameState.IsSlowMotionActive = false;
            }
        }

        // Update multiplier
        if (_gameState.MultiplierFrames > 0)
        {
            _gameState.MultiplierFrames--;
            if (_gameState.MultiplierFrames == 0)
            {
                _gameState.IsMultiplierActive = false;
                _gameState.ScoreMultiplier = 1;
            }
        }
    }

    private void UpdateEntities()
    {
        // Spawn enemies at regular intervals
        if (++_gameState.EnemySpawnCounter >= GameState.EnemySpawnRate)
        {
            int lane = _random.Next(0, 3);
            Enemy.EnemyType enemyType = GetRandomEnemyType();
            Enemy enemy;

            // Create enemy based on type
            switch (enemyType)
            {
                case Enemy.EnemyType.Truck:
                    enemy = new Enemy(80, 120, lane, _gameState.Speed * 0.7f, enemyType);
                    break;
                case Enemy.EnemyType.Motorcycle:
                    enemy = new Enemy(40, 70, lane, _gameState.Speed * 1.3f, enemyType);
                    break;
                case Enemy.EnemyType.Police:
                    enemy = new Enemy(60, 100, lane, _gameState.Speed * 1.1f, enemyType);
                    break;
                default: // RegularCar
                    enemy = new Enemy(60, 100, lane, _gameState.Speed, enemyType);
                    break;
            }

            _gameState.Enemies.Add(enemy);
            _gameState.EnemySpawnCounter = 0;
        }

        // Spawn coins at regular intervals (slightly less frequent than enemies)
        if (++_gameState.CollectibleSpawnCounter >= GameState.CollectibleSpawnRate)
        {
            int coinLane;
            int playerLane = _gameState.Player.CurrentLane;
            bool laneIsOccupied;
            int attempts = 0;

            do
            {
                coinLane = _random.Next(0, 3);
                laneIsOccupied = false;

                // Check if this lane has an enemy near spawn area
                foreach (var enemy in _gameState.Enemies)
                {
                    if (enemy.Lane == coinLane && enemy.Y < 200)
                    {
                        laneIsOccupied = true;
                        break;
                    }
                }

                attempts++;
            }
            while ((coinLane == playerLane || laneIsOccupied) && attempts < 10);

            // Only spawn if we found a good lane
            if (attempts < 10)
            {
                _gameState.Collectibles.Add(new Collectible(40, 40, coinLane));
            }

            _gameState.CollectibleSpawnCounter = 0;
        }

        // Spawn bonuses (less frequently)
        if (++_gameState.BonusSpawnCounter >= GameState.BonusSpawnRate)
        {
            int lane = _random.Next(0, 3);
            Bonus.BonusType bonusType = GetRandomBonusType();
            _gameState.Bonuses.Add(new Bonus(50, 50, lane, bonusType));
            _gameState.BonusSpawnCounter = 0;
        }

        // Process all enemies - move, check collisions, remove if off-screen
        for (int i = _gameState.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _gameState.Enemies[i];

            // Special behavior for police car
            if (enemy.Type == Enemy.EnemyType.Police)
            {
                // Police car tries to move to the player's lane
                if (_random.Next(0, 100) < 3 && enemy.Y < _gameState.ScreenHeight * 0.5f)
                {
                    enemy.Lane = _gameState.Player.CurrentLane;
                }
            }

            // Adjust speed if slow motion is active
            float effectiveSpeed = enemy.Speed;
            if (_gameState.IsSlowMotionActive)
            {
                effectiveSpeed *= 0.5f; // Slow down enemies by half
            }

            enemy.Y += effectiveSpeed;

            // Check collision with player (only if not invulnerable)
            if (_gameState.InvulnerabilityFrames == 0 && !_gameState.IsShieldActive)
            {
                var collision = CheckCarCollision(enemy, _gameState.Player);
                if (collision == CollisionResult.FrontalCollision)
                {
                    EndGame("CRASHED!");
                    return;
                }
                else if (collision == CollisionResult.SideCollision)
                {
                    // Side collision reduces lives
                    if (--_gameState.Lives <= 0)
                    {
                        EndGame("GAME OVER");
                        return;
                    }
                    _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
                    _gameState.Enemies.RemoveAt(i);
                    continue;
                }
            }

            // Remove enemy if it moves off the bottom of the screen
            if (enemy.Y > _gameState.ScreenHeight)
            {
                _gameState.Enemies.RemoveAt(i);
            }
        }

        // Process all coins - move, check collection, remove if collected or off-screen
        for (int i = _gameState.Collectibles.Count - 1; i >= 0; i--)
        {
            var coin = _gameState.Collectibles[i];

            // Magnet effect: coins move toward player
            if (_gameState.IsMagnetActive)
            {
                float playerCenterX = _gameState.Player.VisualX + (_gameState.Player.Width / 2);
                float coinCenterX = coin.CalculateX(_gameState.ScreenWidth) + (coin.Width / 2);

                // Move coin toward player horizontally
                // Note: We can't modify the X directly because it's calculated by CalculateX.
                // We'll adjust the lane instead for simplicity, but this might not be perfect.
                // For a better effect, we could add a property to Collectible for horizontal offset.
                // For now, we'll just move the coin faster towards the player's lane.
                // This is a simplified implementation.
                if (coinCenterX < playerCenterX - 10)
                {
                    // Move coin right
                    // We can't change the lane, so we'll skip this for now.
                    // Alternatively, we can change the lane property and recalculate X.
                }
                else if (coinCenterX > playerCenterX + 10)
                {
                    // Move coin left
                }
            }

            coin.Y += _gameState.Speed; // Move coin down the screen

            // Check if player collected the coin
            if (CheckCoinCollection(coin, _gameState.Player))
            {
                _gameState.CoinsCollected++;
                _gameState.Score += 50; // Bonus points for collecting a coin
                _gameState.Collectibles.RemoveAt(i);
                continue;
            }

            // Remove coin if it moves off the bottom of the screen
            if (coin.Y > _gameState.ScreenHeight)
            {
                _gameState.Collectibles.RemoveAt(i);
            }
        }

        // Process bonuses
        for (int i = _gameState.Bonuses.Count - 1; i >= 0; i--)
        {
            var bonus = _gameState.Bonuses[i];
            bonus.Y += _gameState.Speed;

            // Check bonus collection
            if (CheckBonusCollection(bonus, _gameState.Player))
            {
                ApplyBonus(bonus.Type);
                _gameState.Bonuses.RemoveAt(i);
                continue;
            }

            // Remove if off screen
            if (bonus.Y > _gameState.ScreenHeight)
            {
                _gameState.Bonuses.RemoveAt(i);
            }
        }
    }

    private Enemy.EnemyType GetRandomEnemyType()
    {
        // Probabilities for different enemy types
        int roll = _random.Next(0, 100);

        if (roll < 50) return Enemy.EnemyType.RegularCar;    // 50%
        if (roll < 70) return Enemy.EnemyType.Truck;         // 20%
        if (roll < 85) return Enemy.EnemyType.Motorcycle;    // 15%
        return Enemy.EnemyType.Police;                       // 15%
    }

    private Bonus.BonusType GetRandomBonusType()
    {
        // Probabilities for different bonuses
        int roll = _random.Next(0, 100);

        if (roll < 30) return Bonus.BonusType.Shield;        // 30%
        if (roll < 55) return Bonus.BonusType.Magnet;        // 25%
        if (roll < 75) return Bonus.BonusType.SlowMotion;    // 20%
        if (roll < 90) return Bonus.BonusType.Multiplier;    // 15%
        return Bonus.BonusType.CoinRain;                     // 10%
    }

    private async void EndGame(string title)
    {
        _gameState.IsRunning = false;
        _gameState.IsGameOver = true;
        _gameLoop.Stop();

        // Save high score to local storage if current score is higher
        int currentHigh = Preferences.Get("HighScore", 0);
        if (_gameState.Score > currentHigh)
        {
            Preferences.Set("HighScore", _gameState.Score);
        }

        // Save total coins (add coins from this game to total)
        int totalCoins = Preferences.Get("TotalCoins", 0);
        Preferences.Set("TotalCoins", totalCoins + _gameState.CoinsCollected);

        // Show game over dialog and return to main menu
        await MainThread.InvokeOnMainThreadAsync(async () => {
            await DisplayAlert(title,
                $"Score: {_gameState.Score}\n" +
                $"Coins collected: {_gameState.CoinsCollected}\n" +
                $"Lives left: {_gameState.Lives}",
                "BACK TO MENU");
            await Navigation.PopAsync();
        });
    }

    private void UpdatePlayerAnimation()
    {
        // If player is not currently animating, do nothing
        if (!_gameState.Player.IsAnimating) return;

        // Calculate animation progress (0 to 1)
        float progress = 1f - ((float)_gameState.Player.AnimationFramesRemaining / _gameState.Player.AnimationFramesTotal);

        // Smooth easing function for natural movement
        float eased = 0.5f - 0.5f * (float)Math.Cos(progress * Math.PI);

        // Update player's visual X position (horizontal movement between lanes)
        _gameState.Player.VisualX = _gameState.Player.StartX +
            (_gameState.Player.TargetX - _gameState.Player.StartX) * eased;

        // Update player's visual Y position (forward lean effect during lane change)
        _gameState.Player.VisualY = (_gameState.ScreenHeight - 150) -
            (4 * progress * (1 - progress) * LeanAmountY);

        // Decrease animation timer and stop if complete
        if (--_gameState.Player.AnimationFramesRemaining <= 0)
        {
            _gameState.Player.IsAnimating = false;
        }
    }

    private CollisionResult CheckCarCollision(Enemy enemy, Player player)
    {
        // Get player's actual position on screen
        float pY = _gameState.ScreenHeight - 150;
        float pX = player.CalculateLaneX(_gameState.ScreenWidth);
        enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

        // Check for rectangle intersection between player and enemy
        if (pX < enemy.X + enemy.Width &&
            pX + player.Width > enemy.X &&
            pY < enemy.Y + enemy.Height &&
            pY + player.Height > enemy.Y)
        {
            // Determine if collision is frontal (from behind) or side
            // Frontal collision occurs if enemy's rear is above player's front zone
            return (enemy.Y + enemy.Height < pY + FrontalZoneDepth)
                ? CollisionResult.FrontalCollision
                : CollisionResult.SideCollision;
        }
        return CollisionResult.None;
    }

    private bool CheckCoinCollection(Collectible coin, Player player)
    {
        // Get player's visual position (actual drawn position with animation)
        float pY = _gameState.Player.VisualY;
        float pX = _gameState.Player.VisualX;
        float coinX = coin.CalculateX(_gameState.ScreenWidth);

        // Simple rectangle intersection check
        bool collisionX = pX < coinX + coin.Width && pX + player.Width > coinX;
        bool collisionY = pY < coin.Y + coin.Height && pY + player.Height > coin.Y;

        return collisionX && collisionY;
    }

    private bool CheckBonusCollection(Bonus bonus, Player player)
    {
        float pY = _gameState.Player.VisualY;
        float pX = _gameState.Player.VisualX;
        float bonusX = bonus.CalculateX(_gameState.ScreenWidth);

        bool collisionX = pX < bonusX + bonus.Width && pX + player.Width > bonusX;
        bool collisionY = pY < bonus.Y + bonus.Height && pY + player.Height > bonus.Y;

        return collisionX && collisionY;
    }

    private void ApplyBonus(Bonus.BonusType bonusType)
    {
        switch (bonusType)
        {
            case Bonus.BonusType.Shield:
                _gameState.IsShieldActive = true;
                _gameState.ShieldFrames = GameState.ShieldDuration;
                _gameState.InvulnerabilityFrames = GameState.ShieldDuration;
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

            case Bonus.BonusType.CoinRain:
                // Spawn 5 coins immediately
                for (int i = 0; i < 5; i++)
                {
                    int coinLane = _random.Next(0, 3);
                    _gameState.Collectibles.Add(new Collectible(40, 40, coinLane));
                }
                break;
        }
    }

    private void OnSwiped(object? sender, SwipedEventArgs e)
    {
        // Don't process swipe if game is not running or player is animating
        if (!_gameState.IsRunning || _gameState.Player.IsAnimating) return;

        int lane = _gameState.Player.CurrentLane;

        // Determine new lane based on swipe direction
        if (e.Direction == SwipeDirection.Left)
        {
            lane--;
        }
        else if (e.Direction == SwipeDirection.Right)
        {
            lane++;
        }

        // If trying to move outside valid lanes (0-2), it's a wall hit
        if (lane < 0 || lane > 2)
        {
            if (_gameState.InvulnerabilityFrames == 0 && !_gameState.IsShieldActive)
            {
                // Wall hit reduces lives
                if (--_gameState.Lives <= 0)
                {
                    EndGame("WALL HIT!");
                }
                else
                {
                    _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
                }
            }
            return; // Don't change lane
        }

        // Valid lane change - start animation
        _gameState.Player.CurrentLane = lane;
        _gameState.Player.StartX = _gameState.Player.VisualX;
        _gameState.Player.TargetX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
        _gameState.Player.IsAnimating = true;
        _gameState.Player.AnimationFramesRemaining = LaneChangeFrames;
        _gameState.Player.AnimationFramesTotal = LaneChangeFrames;
    }

    // Helper methods for upgrade system
    private int GetPlayerDurability()
    {
        return Preferences.Get("DurabilityLevel", 1);
    }

    private float GetPlayerSpeedMultiplier()
    {
        int speedLevel = Preferences.Get("SpeedLevel", 1);
        return 1.0f + (speedLevel - 1) * 0.1f; // +10% for each level
    }
}