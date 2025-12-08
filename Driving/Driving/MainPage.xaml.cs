using Driving.GameEngine;
using Driving.GameEngine;
using Driving.Models;

namespace Driving;

public partial class MainPage : ContentPage
{
    private readonly GameState _gameState;
    private readonly GameDrawable _gameDrawable;
    private IDispatcherTimer _gameLoop;

    public MainPage()
    {
        InitializeComponent();

        // Initialize State
        _gameState = new GameState();
        _gameDrawable = new GameDrawable(_gameState);

        // Assign the Drawable to the View
        GameCanvas.Drawable = _gameDrawable;

        // Setup the Game Loop (60 FPS target)
        _gameLoop = Dispatcher.CreateTimer();
        _gameLoop.Interval = TimeSpan.FromMilliseconds(16);
        _gameLoop.Tick += GameLoop_Tick;
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        _gameState.IsRunning = true;
        _gameState.IsGameOver = false;
        _gameState.Score = 0;

        StartMenu.IsVisible = false;

        _gameLoop.Start();
    }

    private void GameLoop_Tick(object sender, EventArgs e)
    {
        if (!_gameState.IsRunning) return;

        // --- UPDATE LOGIC WILL GO HERE ---
        // 1. Move Background
        // 2. Spawn Enemies
        // 3. Move Enemies
        // 4. Check Collisions

        _gameState.Score++; // Fake score for now

        // Trigger a redraw of the GraphicsView
        GameCanvas.Invalidate();
    }

    private Point _touchStart;

    // Обработка касания/движения пальца
    private void GameCanvas_Touch(object sender, Microsoft.Maui.Controls.TouchEventArgs e)
    {
        if (!_gameState.IsRunning) return;

        // Получаем текущую точку касания
        Point currentPoint = e.Touches[0];

        switch (e.Type)
        {
            case TouchActionType.Pressed:
                _touchStart = currentPoint;
                break;

            case TouchActionType.Released:
                // Определяем, был ли это свайп или просто тап
                float deltaX = currentPoint.X - _touchStart.X;

                if (Math.Abs(deltaX) > 50) // Порог для определения свайпа
                {
                    // Свайп вправо
                    if (deltaX > 0)
                    {
                        _gameState.Player.ChangeLane(1);
                    }
                    // Свайп влево
                    else
                    {
                        _gameState.Player.ChangeLane(-1);
                    }

                    // Перерисовываем экран, чтобы увидеть игрока в новой полосе сразу
                    GameCanvas.Invalidate();
                }
                break;
        }
    }
}