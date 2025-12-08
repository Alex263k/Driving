namespace Driving.Models;

public class GameState
{
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;

    // Размеры экрана (для GameDrawable)
    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }

    public float Speed { get; set; } = 10f;
    public int Score { get; set; } = 0;

    // Смещение для анимации разметки
    public float RoadMarkingOffset { get; set; } = 0;

    // Игрок
    public Player Player { get; set; } = new Player();
}