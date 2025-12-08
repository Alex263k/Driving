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

    // Enum to define the type of collision
    private enum CollisionResult
    {
        None,
        SideCollision,    // Side, deducts a life
        FrontalCollision  // Frontal, instant Game Over
    }

    // CONSTANT: The vertical distance (in pixels) from the player's front (top edge) 
    // that counts as a "frontal" impact zone. 
    // Player height is 100px. We use the top 20px as the frontal zone.
    private const float FrontalZoneDepth = 20f;

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
        _gameState.Lives = 3; // Start with 3 lives
        _gameState.InvulnerabilityFrames = 0; // Reset invulnerability

        _gameState.Enemies.Clear();
        _gameState.EnemySpawnCounter = 0;

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

        // 2. Road movement and score
        _gameState.Score++;
        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) _gameState.RoadMarkingOffset -= 60;

        // 3. Enemy movement, collision check, and removal
        for (int i = _gameState.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _gameState.Enemies[i];
            enemy.Y += _gameState.Speed;

            // Check collision only if the player is NOT in invulnerability mode
            if (_gameState.InvulnerabilityFrames == 0)
            {
                var collision = CheckCarCollision(enemy, _gameState.Player);

                if (collision == CollisionResult.FrontalCollision)
                {
                    // FRONTAL COLLISION -> INSTANT GAME OVER
                    _gameState.IsGameOver = true;
                    StopGame();
                    return;
                }
                else if (collision == CollisionResult.SideCollision)
                {
                    // SIDE COLLISION
                    HandleLifeDeduction();
                    // Activate invulnerability
                    _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;

                    // Remove the enemy that was hit (optional, but makes sense for impact)
                    _gameState.Enemies.RemoveAt(i);
                    continue;
                }
            }

            // Remove enemy if it has left the bottom edge
            if (enemy.Y > _gameState.ScreenHeight)
            {
                _gameState.Enemies.RemoveAt(i);
            }
        }

        // 4. Spawn logic
        _gameState.EnemySpawnCounter++;
        if (_gameState.EnemySpawnCounter >= GameState.EnemySpawnRate)
        {
            SpawnNewEnemy();
            _gameState.EnemySpawnCounter = 0;
        }

        GameCanvas.Invalidate();
    }

    private CollisionResult CheckCarCollision(Enemy enemy, Player player)
    {
        // Player Y position (fixed top edge)
        float playerY = _gameState.ScreenHeight - 150;
        float playerX = player.CalculateX(_gameState.ScreenWidth);

        // Enemy X position (calculated based on lane)
        enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

        // AABB check (Axis-Aligned Bounding Box)
        bool collisionX = playerX < enemy.X + enemy.Width &&
                          playerX + player.Width > enemy.X;
        bool collisionY = playerY < enemy.Y + enemy.Height &&
                          playerY + player.Height > enemy.Y;

        if (collisionX && collisionY)
        {
            // Enemy's bottom edge (the impact point)
            float impactY = enemy.Y + enemy.Height;

            // Player's frontal zone (top FrontalZoneDepth pixels)
            float playerFrontalLimit = playerY + FrontalZoneDepth;

            // 1. Check for FRONTAL collision:
            // Is the enemy's bottom edge inside the player's frontal zone (top 20px)?
            if (impactY > playerY && impactY < playerFrontalLimit)
            {
                return CollisionResult.FrontalCollision;
            }

            // 2. Otherwise, it's a SIDE collision:
            // The enemy's bottom edge hit the side/rear part of the player's car.
            return CollisionResult.SideCollision;
        }

        return CollisionResult.None;
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

    // Handle swiping (for lane change and wall hit)
    private void OnSwiped(object sender, SwipedEventArgs e)
    {
        if (!_gameState.IsRunning) return;

        int newLane = _gameState.Player.CurrentLane;
        bool boundaryHit = false;

        switch (e.Direction)
        {
            case SwipeDirection.Left:
                newLane = _gameState.Player.CurrentLane - 1;
                if (newLane < 0)
                {
                    boundaryHit = true;
                }
                break;
            case SwipeDirection.Right:
                newLane = _gameState.Player.CurrentLane + 1;
                if (newLane > 2)
                {
                    boundaryHit = true;
                }
                break;
        }

        if (boundaryHit)
        {
            // Wall Hit -> Side Collision (deduct a life)
            if (_gameState.InvulnerabilityFrames == 0)
            {
                HandleLifeDeduction();
                _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
            }
        }
        else
        {
            // Successful lane change
            _gameState.Player.CurrentLane = newLane;
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