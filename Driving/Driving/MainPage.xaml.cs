using Driving.GameEngine;
using Driving.Models;
using Microsoft.Maui.Controls; // Убедитесь, что этот using есть

namespace Driving; // Корневое пространство имен

public partial class MainPage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private readonly IDispatcherTimer _gameLoop;

    public MainPage()
    {
        InitializeComponent(); // Должно работать

        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState);
        GameCanvas.Drawable = _gameDrawable; // Должно работать

        _gameLoop = Dispatcher.CreateTimer();
        _gameLoop.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        _gameLoop.Tick += GameLoop_Tick; // Ошибка CS6022 исправлена, так как делегат соответствует
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        _gameState.IsRunning = true;
        _gameState.IsGameOver = false;
        _gameState.Score = 0;
        StartMenu.IsVisible = false; // Должно работать
        _gameLoop.Start();
    }

    private void GameLoop_Tick(object sender, EventArgs e)
    {
        if (!_gameState.IsRunning) return;

        // Логика движения дороги и счета
        _gameState.Score++;

        GameCanvas.Invalidate();
    }

    // НОВЫЙ МЕТОД: Обработка свайпов (SwipeGestureRecognizer)
    private void OnSwiped(object sender, SwipedEventArgs e)
    {
        if (!_gameState.IsRunning) return;

        switch (e.Direction)
        {
            case SwipeDirection.Left:
                _gameState.Player.ChangeLane(-1);
                break;
            case SwipeDirection.Right:
                _gameState.Player.ChangeLane(1);
                break;
        }
        GameCanvas.Invalidate();
    }

    private void StopGame()
    {
        _gameState.IsRunning = false;
        _gameLoop.Stop();
        StartMenu.IsVisible = true;

        // Сохранение рекорда
        int currentHigh = Preferences.Get("HighScore", 0);
        if (_gameState.Score > currentHigh)
        {
            Preferences.Set("HighScore", _gameState.Score);
        }
    }
}