namespace Driving.Models;

public class GameState
{
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }
    public float Speed { get; set; } = 10f;
    public int Score { get; set; } = 0;
    public int CoinsCollected { get; set; } = 0;

    public Player Player { get; set; } = new Player();

    // ТАЙМЕР И ИНТЕРВАЛ (Динамический вместо константы)
    public float FuelTimer { get; set; } = 0f;
    public float CurrentFuelConsumptionInterval { get; set; } = 0.20f;

    public List<Enemy> Enemies { get; set; } = new List<Enemy>();
    public int EnemySpawnCounter { get; set; } = 0;
    public const int EnemySpawnRate = 60;

    public List<Collectible> Collectibles { get; set; } = new List<Collectible>();
    public int CollectibleSpawnCounter { get; set; } = 0;
    public const int CollectibleSpawnRate = 45;

    public List<Bonus> Bonuses { get; set; } = new List<Bonus>();
    public int BonusSpawnCounter { get; set; } = 0;
    public const int BonusSpawnRate = 180;

    public bool IsShieldActive { get; set; } = false;
    public int ShieldFrames { get; set; } = 0;
    public const int ShieldDuration = 180;

    public bool IsMagnetActive { get; set; } = false;
    public int MagnetFrames { get; set; } = 0;
    public const int MagnetDuration = 300;

    public bool IsSlowMotionActive { get; set; } = false;
    public int SlowMotionFrames { get; set; } = 0;
    public const int SlowMotionDuration = 240;

    public bool IsMultiplierActive { get; set; } = false;
    public int MultiplierFrames { get; set; } = 0;
    public const int MultiplierDuration = 300;
    public int ScoreMultiplier { get; set; } = 1;

    public float RoadMarkingOffset { get; set; } = 0;

    public int Lives { get; set; } = 3;
    public int InvulnerabilityFrames { get; set; } = 0;
    public const int InvulnerabilityDuration = 30;

    public List<FuelCan> FuelCans { get; set; } = new List<FuelCan>();
    public int FuelCanSpawnCounter { get; set; } = 0;
    public const int FuelCanSpawnRate = 80;

    public bool IsFuelDepleted { get; set; } = false;

    public GameState()
    {
        // Загрузка жизней из улучшений
        Lives = Preferences.Get("DurabilityLevel", 1);

        // РАСЧЕТ РАСХОДА ТОПЛИВА ПРИ СТАРТЕ
        UpdateConsumptionInterval();

        // РАСЧЕТ МАКСИМАЛЬНОГО ТОПЛИВА
        int fuelLevel = Preferences.Get("FuelTankLevel", 1);
        Player.MaxFuel = 100f + (fuelLevel - 1) * 35f;
        Player.CurrentFuel = Player.MaxFuel;
    }

    public void UpdateConsumptionInterval()
    {
        int engineLevel = Preferences.Get("EngineEfficiencyLevel", 1);

        // Настройка "плохого" улучшения:
        // Топливо тратится очень часто (каждые 0.20 сек)
        float baseInterval = 0.20f;
        // Улучшение почти не помогает (всего 0.005 сек за уровень)
        float efficiencyBonus = (engineLevel - 1) * 0.005f;
        // Даже на 10 уровне интервал будет 0.15 сек (очень быстро)
        CurrentFuelConsumptionInterval = Math.Max(baseInterval - efficiencyBonus, 0.15f);
    }
}