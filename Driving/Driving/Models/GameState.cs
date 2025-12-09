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

    // Enemy Properties
    public List<Enemy> Enemies { get; set; } = new List<Enemy>();
    public int EnemySpawnCounter { get; set; } = 0;
    public const int EnemySpawnRate = 60;

    // Collectible Properties
    public List<Collectible> Collectibles { get; set; } = new List<Collectible>();
    public int CollectibleSpawnCounter { get; set; } = 0;
    public const int CollectibleSpawnRate = 45;

    public float RoadMarkingOffset { get; set; } = 0;

    // Life/Invulnerability Properties
    public int Lives { get; set; }
    public int InvulnerabilityFrames { get; set; } = 0;
    public const int InvulnerabilityDuration = 30;
}