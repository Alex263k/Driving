using System.Collections.Generic;

namespace Driving.Models;

public class GameState
{
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;

    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }

    public float Speed { get; set; } = 10f;
    public int Score { get; set; } = 0;

    public Player Player { get; set; } = new Player();
    public List<Enemy> Enemies { get; set; } = new List<Enemy>();

    public float RoadMarkingOffset { get; set; } = 0;

    public int EnemySpawnCounter { get; set; } = 0;
    public const int EnemySpawnRate = 60; // Спаун нового врага каждые 60 тиков (~1 сек)

    // НОВЫЕ СВОЙСТВА ДЛЯ СИСТЕМЫ ЖИЗНЕЙ
    public int Lives { get; set; }
    public int InvulnerabilityFrames { get; set; } = 0;
    public const int InvulnerabilityDuration = 30; // 0.5 секунды неуязвимости (30 кадров при 60 FPS)
}