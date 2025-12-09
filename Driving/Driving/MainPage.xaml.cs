using Driving.GameEngine;
using Driving.Models;
using Microsoft.Maui.Controls;

namespace Driving;

public partial class MainPage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private readonly IDispatcherTimer _gameLoop;
    private readonly Random _random = new Random();

    // CONSTANTS for Collision
    private enum CollisionResult { None, SideCollision, FrontalCollision }
    private const float FrontalZoneDepth = 20f; // Top 20px of player car is instant death zone

    // CONSTANTS for Animation
    private const int LaneChangeDurationFrames = 10; // Animation duration in frames (~0.16s)
    private const float LaneChangeForwardLeanY = 15f; // Max vertical shift (forward lean) during turn

    public MainPage()
    {
        InitializeComponent();

        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState);
        GameCanvas.Drawable = _gameDrawable;

        _gameLoop = Dispatcher.CreateTimer();
        _gameLoop.Interval = TimeSpan.FromMilliseconds(16);
        _gameLoop.Tick += GameLoop_Tick;
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        _gameState.IsRunning = true;
        _gameState.IsGameOver = false;
        _gameState.Score = 0;
        _gameState.Lives = 3;
        _gameState.InvulnerabilityFrames = 0;

        _gameState.Enemies.Clear();
        _gameState.Collectibles.Clear();
        _gameState.EnemySpawnCounter = 0;
        _gameState.CollectibleSpawnCounter = 0;

        // Initialize player visual position on start
        _gameState.Player.VisualY = _gameState.ScreenHeight - 150;

        StartMenu.IsVisible = false;
        _gameLoop.Start();
    }

    private void GameLoop_Tick(object sender, EventArgs e)
    {
        if (!_gameState.IsRunning) return;

        // 1. Invulnerability logic
        if (_gameState.InvulnerabilityFrames > 0)
        {
            _gameState.InvulnerabilityFrames--;
        }

        // 2. Player Animation Logic
        AnimatePlayerTurn();

        // 3. Road movement and score
        _gameState.Score++;
        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) _gameState.RoadMarkingOffset -= 60;

        // 4. Enemy movement, collision check, and removal
        for (int i = _gameState.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _gameState.Enemies[i];
            enemy.Y += _gameState.Speed;

            if (_gameState.InvulnerabilityFrames == 0)
            {
                var collision = CheckCarCollision(enemy, _gameState.Player);

                if (collision == CollisionResult.FrontalCollision)
                {
                    _gameState.IsGameOver = true;
                    StopGame();
                    return;
                }
                else if (collision == CollisionResult.SideCollision)
                {
                    HandleLifeDeduction();
                    _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;

                    _gameState.Enemies.RemoveAt(i);
                    continue;
                }
            }

            if (enemy.Y > _gameState.ScreenHeight)
            {
                _gameState.Enemies.RemoveAt(i);
            }
        }

        // 5. Collectible Movement and Pickup Check
        for (int i = _gameState.Collectibles.Count - 1; i >= 0; i--)
        {
            var collectible = _gameState.Collectibles[i];

            collectible.Y += _gameState.Speed;

            if (CheckCollectibleCollision(collectible, _gameState.Player))
            {
                HandleCoinPickup(collectible, i);
                continue;
            }

            if (collectible.Y > _gameState.ScreenHeight)
            {
                _gameState.Collectibles.RemoveAt(i);
            }
        }

        // 6. Spawn logic
        _gameState.EnemySpawnCounter++;
        if (_gameState.EnemySpawnCounter >= GameState.EnemySpawnRate)
        {
            SpawnNewEnemy();
            _gameState.EnemySpawnCounter = 0;
        }

        _gameState.CollectibleSpawnCounter++;
        if (_gameState.CollectibleSpawnCounter >= GameState.CollectibleSpawnRate)
        {
            SpawnNewCollectible();
            _gameState.CollectibleSpawnCounter = 0;
        }

        GameCanvas.Invalidate();
    }

    private void AnimatePlayerTurn()
    {
        if (!_gameState.Player.IsAnimating)
        {
            // If not animating, ensure the VisualX is the target lane X
            _gameState.Player.VisualX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.VisualY = _gameState.ScreenHeight - 150;
            return;
        }

        int total = _gameState.Player.AnimationFramesTotal;
        int remaining = _gameState.Player.AnimationFramesRemaining;

        float linearProgress = 1f - ((float)remaining / total); // Linear progress (0.0 to 1.0)

        // 1. Calculate Eased Progress (EaseInOut Sine)
        // This makes the animation start slow, speed up in the middle, and slow down at the end.
        float easedProgress = 0.5f - 0.5f * (float)Math.Cos(linearProgress * Math.PI);

        // 2. Interpolate X position using Eased Progress
        _gameState.Player.VisualX = _gameState.Player.StartX + (_gameState.Player.TargetX - _gameState.Player.StartX) * easedProgress;

        // 3. Calculate Y position (Forward Lean using a parabolic curve)
        // The lean factor still relies on the linear progress to peak in the middle of the transition.
        float leanFactor = 4 * linearProgress * (1 - linearProgress); // Factor goes from 0 to 1 back to 0

        float baseY = _gameState.ScreenHeight - 150;
        _gameState.Player.VisualY = baseY - (LaneChangeForwardLeanY * leanFactor);

        // 4. Decrement timer
        _gameState.Player.AnimationFramesRemaining--;

        if (_gameState.Player.AnimationFramesRemaining <= 0)
        {
            // Animation finished. Snap to final position.
            _gameState.Player.IsAnimating = false;
            _gameState.Player.VisualX = _gameState.Player.TargetX;
            _gameState.Player.VisualY = baseY;
        }
    }
    private CollisionResult CheckCarCollision(Enemy enemy, Player player)
    {
        float playerY = _gameState.ScreenHeight - 150;
        float playerX = player.CalculateLaneX(_gameState.ScreenWidth); // Using fixed lane X for collision checks
        enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

        // AABB check 
        bool collisionX = playerX < enemy.X + enemy.Width &&
                          playerX + player.Width > enemy.X;
        bool collisionY = playerY < enemy.Y + enemy.Height &&
                          playerY + player.Height > enemy.Y;

        if (collisionX && collisionY)
        {
            float impactY = enemy.Y + enemy.Height;
            float playerFrontalLimit = playerY + FrontalZoneDepth;

            // FRONTAL collision: Enemy's bottom edge inside player's top 20px
            if (impactY > playerY && impactY < playerFrontalLimit)
            {
                return CollisionResult.FrontalCollision;
            }

            // SIDE collision
            return CollisionResult.SideCollision;
        }

        return CollisionResult.None;
    }

    private bool CheckCollectibleCollision(Collectible collectible, Player player)
    {
        float collectibleX = collectible.CalculateX(_gameState.ScreenWidth);
        float playerX = player.CalculateLaneX(_gameState.ScreenWidth); // Using fixed lane X for collision checks
        float playerY = _gameState.ScreenHeight - 150;

        collectible.X = collectibleX;

        // AABB check
        bool collisionX = playerX < collectible.X + collectible.Width &&
                          playerX + player.Width > collectible.X;

        bool collisionY = playerY < collectible.Y + collectible.Height &&
                          playerY + player.Height > collectible.Y;

        return collisionX && collisionY;
    }

    private void HandleLifeDeduction()
    {
        _gameState.Lives--;

        if (_gameState.Lives <= 0)
        {
            _gameState.IsGameOver = true;
            StopGame();
        }
    }

    private void HandleCoinPickup(Collectible coin, int index)
    {
        _gameState.Score += 100;
        _gameState.Collectibles.RemoveAt(index);
    }

    private void OnSwiped(object sender, SwipedEventArgs e)
    {
        // Ignore input while animating
        if (!_gameState.IsRunning || _gameState.Player.IsAnimating) return;

        int targetLane = _gameState.Player.CurrentLane;
        bool boundaryHit = false;

        switch (e.Direction)
        {
            case SwipeDirection.Left:
                targetLane = _gameState.Player.CurrentLane - 1;
                if (targetLane < 0) boundaryHit = true;
                break;
            case SwipeDirection.Right:
                targetLane = _gameState.Player.CurrentLane + 1;
                if (targetLane > 2) boundaryHit = true;
                break;
        }

        if (boundaryHit)
        {
            // Wall Hit (Side Collision)
            if (_gameState.InvulnerabilityFrames == 0)
            {
                HandleLifeDeduction();
                _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
            }
        }
        else
        {
            // Successful Lane Change -> Start Animation
            _gameState.Player.CurrentLane = targetLane;

            _gameState.Player.StartX = _gameState.Player.VisualX;
            _gameState.Player.TargetX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.IsAnimating = true;
            _gameState.Player.AnimationFramesTotal = LaneChangeDurationFrames;
            _gameState.Player.AnimationFramesRemaining = LaneChangeDurationFrames;
        }

        GameCanvas.Invalidate();
    }

    private void SpawnNewEnemy()
    {
        int lane = _random.Next(0, 3);

        var newEnemy = new Enemy(
            width: 60f,
            height: 100f,
            lane: lane,
            initialSpeed: _gameState.Speed
        );

        _gameState.Enemies.Add(newEnemy);
    }

    private void SpawnNewCollectible()
    {
        int lane = _random.Next(0, 3);

        var newCollectible = new Collectible(
            width: 30f,
            height: 30f,
            lane: lane
        );

        _gameState.Collectibles.Add(newCollectible);
    }

    private void StopGame()
    {
        _gameState.IsRunning = false;
        _gameLoop.Stop();
        StartMenu.IsVisible = true;

        int currentHigh = Preferences.Get("HighScore", 0);
        if (_gameState.Score > currentHigh)
        {
            Preferences.Set("HighScore", _gameState.Score);
        }
    }
}