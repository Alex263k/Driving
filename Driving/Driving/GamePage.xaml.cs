using Driving.GameEngine;
using Driving.Models;

namespace Driving;

public partial class GamePage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private readonly IDispatcherTimer _gameLoop;
    private readonly Random _random = new Random();

    // Collision and movement settings
    private enum CollisionResult { None, SideCollision, FrontalCollision }
    private const float FrontalZoneDepth = 20f;
    private const int LaneChangeFrames = 10;
    private const float LeanAmountY = 15f;

    public GamePage()
    {
        InitializeComponent();

        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState);
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

    private void StartNewGame()
    {
        // Reset all game variables
        _gameState.IsRunning = true;
        _gameState.Score = 0;
        _gameState.Lives = 3;
        _gameState.Speed = 10f;
        _gameState.Enemies.Clear();
        _gameState.Collectibles.Clear();

        // Reset player visuals
        _gameState.Player.CurrentLane = 1;
        _gameState.Player.VisualX = 0;

        _gameLoop.Start();
    }

    private void GameLoop_Tick(object? sender, EventArgs e)
    {
        if (!_gameState.IsRunning) return;

        // 1. Process Animations
        UpdatePlayerAnimation();

        // 2. Move environment and update Score
        _gameState.Score++;
        _gameState.RoadMarkingOffset += _gameState.Speed / 2f;
        if (_gameState.RoadMarkingOffset > 60) _gameState.RoadMarkingOffset -= 60;
        if (_gameState.InvulnerabilityFrames > 0) _gameState.InvulnerabilityFrames--;

        // 3. Spawning and Entities
        UpdateEntities();

        // 4. Force Redraw
        GameCanvas.Invalidate();
    }

    private void UpdateEntities()
    {
        // Spawn enemies
        if (++_gameState.EnemySpawnCounter >= GameState.EnemySpawnRate)
        {
            _gameState.Enemies.Add(new Enemy(60, 100, _random.Next(0, 3), _gameState.Speed));
            _gameState.EnemySpawnCounter = 0;
        }

        // Process Enemies and Collisions
        for (int i = _gameState.Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _gameState.Enemies[i];
            enemy.Y += _gameState.Speed;

            if (_gameState.InvulnerabilityFrames == 0)
            {
                var collision = CheckCarCollision(enemy, _gameState.Player);
                if (collision == CollisionResult.FrontalCollision)
                {
                    EndGame("CRASHED!"); return;
                }
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
    }

    private async void EndGame(string title)
    {
        _gameState.IsRunning = false;
        _gameLoop.Stop();

        // Save High Score locally
        int currentHigh = Preferences.Get("HighScore", 0);
        if (_gameState.Score > currentHigh) Preferences.Set("HighScore", _gameState.Score);

        // Show result and pop back to Menu
        await MainThread.InvokeOnMainThreadAsync(async () => {
            await DisplayAlert(title, $"Score: {_gameState.Score}", "MENU");
            await Navigation.PopAsync();
        });
    }

    private void UpdatePlayerAnimation()
    {
        if (!_gameState.Player.IsAnimating) return;
        float progress = 1f - ((float)_gameState.Player.AnimationFramesRemaining / _gameState.Player.AnimationFramesTotal);
        float eased = 0.5f - 0.5f * (float)Math.Cos(progress * Math.PI);

        _gameState.Player.VisualX = _gameState.Player.StartX + (_gameState.Player.TargetX - _gameState.Player.StartX) * eased;
        _gameState.Player.VisualY = (_gameState.ScreenHeight - 150) - (4 * progress * (1 - progress) * LeanAmountY);

        if (--_gameState.Player.AnimationFramesRemaining <= 0) _gameState.Player.IsAnimating = false;
    }

    private CollisionResult CheckCarCollision(Enemy enemy, Player player)
    {
        float pY = _gameState.ScreenHeight - 150, pX = player.CalculateLaneX(_gameState.ScreenWidth);
        enemy.X = enemy.CalculateX(_gameState.ScreenWidth);

        if (pX < enemy.X + enemy.Width && pX + player.Width > enemy.X && pY < enemy.Y + enemy.Height && pY + player.Height > enemy.Y)
        {
            return (enemy.Y + enemy.Height < pY + FrontalZoneDepth) ? CollisionResult.FrontalCollision : CollisionResult.SideCollision;
        }
        return CollisionResult.None;
    }

    private void OnSwiped(object? sender, SwipedEventArgs e)
    {
        if (!_gameState.IsRunning || _gameState.Player.IsAnimating) return;
        int lane = _gameState.Player.CurrentLane;
        if (e.Direction == SwipeDirection.Left) lane--; else lane++;

        if (lane < 0 || lane > 2)
        {
            if (_gameState.InvulnerabilityFrames == 0)
            {
                if (--_gameState.Lives <= 0) EndGame("WALL HIT!");
                else _gameState.InvulnerabilityFrames = GameState.InvulnerabilityDuration;
            }
        }
        else
        {
            _gameState.Player.CurrentLane = lane;
            _gameState.Player.StartX = _gameState.Player.VisualX;
            _gameState.Player.TargetX = _gameState.Player.CalculateLaneX(_gameState.ScreenWidth);
            _gameState.Player.IsAnimating = true;
            _gameState.Player.AnimationFramesRemaining = LaneChangeFrames;
            _gameState.Player.AnimationFramesTotal = LaneChangeFrames;
        }
    }
}