namespace Driving.Models;

public class GameState
{
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;

    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }

    public float Speed { get; set; } = 10f;
    public int Score { get; set; } = 0;

    public int CoinsCollected { get; set; } = 0; // Coin counter

    public Player Player { get; set; } = new Player();

    // Fuel consumption timer (for 3 units per second consumption)
    public float FuelTimer { get; set; } = 0f;
    public const float FuelConsumptionInterval = 0.333f; // 1/3 second (3 times per second)

    // Enemy Properties
    public List<Enemy> Enemies { get; set; } = new List<Enemy>();
    public int EnemySpawnCounter { get; set; } = 0;
    public const int EnemySpawnRate = 60;

    // Collectible Properties
    public List<Collectible> Collectibles { get; set; } = new List<Collectible>();
    public int CollectibleSpawnCounter { get; set; } = 0;
    public const int CollectibleSpawnRate = 45;

    // Bonus Properties
    public List<Bonus> Bonuses { get; set; } = new List<Bonus>();
    public int BonusSpawnCounter { get; set; } = 0;
    public const int BonusSpawnRate = 180; // Less frequent than coins

    // Active Bonus Effects
    public bool IsShieldActive { get; set; } = false;
    public int ShieldFrames { get; set; } = 0;
    public const int ShieldDuration = 180; // 6 seconds at 30 FPS

    public bool IsMagnetActive { get; set; } = false;
    public int MagnetFrames { get; set; } = 0;
    public const int MagnetDuration = 300; // 10 seconds

    public bool IsSlowMotionActive { get; set; } = false;
    public int SlowMotionFrames { get; set; } = 0;
    public const int SlowMotionDuration = 240; // 8 seconds

    public bool IsMultiplierActive { get; set; } = false;
    public int MultiplierFrames { get; set; } = 0;
    public const int MultiplierDuration = 300; // 10 seconds
    public int ScoreMultiplier { get; set; } = 1;

    public float RoadMarkingOffset { get; set; } = 0;

    // Life/Invulnerability Properties
    public int Lives { get; set; } = 3;
    public int InvulnerabilityFrames { get; set; } = 0;
    public const int InvulnerabilityDuration = 30;

    // Fuel cans
    public List<FuelCan> FuelCans { get; set; } = new List<FuelCan>();
    public int FuelCanSpawnCounter { get; set; } = 0;
    public const int FuelCanSpawnRate = 80; // Changed from 120 to 80 (appear more frequently)

    // Fuel statistics
    public bool IsFuelDepleted { get; set; } = false;

    // Constructor for initialization
    public GameState()
    {
        Lives = Preferences.Get("DurabilityLevel", 1);
    }
}