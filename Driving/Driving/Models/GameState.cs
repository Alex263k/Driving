using System.Collections.Generic;
using Microsoft.Maui.Storage;

namespace Driving.Models;

/// <summary>
/// Manages the current status, statistics, and entity collections of a game session.
/// Acts as the central data hub for the game loop and UI binding.
/// </summary>
public class GameState
{
    // --- Engine & Session State ---
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }
    public float Speed { get; set; } = 10f; // Global scrolling speed
    public float RoadMarkingOffset { get; set; } = 0; // Used for road animation

    // --- Scoring & Economy ---
    public int Score { get; set; } = 0;
    public int CoinsCollected { get; set; } = 0;
    public int ScoreMultiplier { get; set; } = 1;

    // --- Player Status ---
    public Player Player { get; set; } = new Player();
    public int Lives { get; set; } = 3;
    public int InvulnerabilityFrames { get; set; } = 0;
    public const int InvulnerabilityDuration = 30; // Frames after being hit

    // --- Fuel Management ---
    public float FuelTimer { get; set; } = 0f;
    public float CurrentFuelConsumptionInterval { get; set; } = 0.20f; // Dynamic interval based on efficiency
    public bool IsFuelDepleted { get; set; } = false;

    // --- Entity Collections & Spawning ---
    public List<Enemy> Enemies { get; set; } = new List<Enemy>();
    public int EnemySpawnCounter { get; set; } = 0;
    public const int EnemySpawnRate = 60; // Spawn check every 60 frames

    public List<Collectible> Collectibles { get; set; } = new List<Collectible>();
    public int CollectibleSpawnCounter { get; set; } = 0;
    public const int CollectibleSpawnRate = 45;

    public List<Bonus> Bonuses { get; set; } = new List<Bonus>();
    public int BonusSpawnCounter { get; set; } = 0;
    public const int BonusSpawnRate = 180;

    public List<FuelCan> FuelCans { get; set; } = new List<FuelCan>();
    public int FuelCanSpawnCounter { get; set; } = 0;
    public const int FuelCanSpawnRate = 80;

    // --- Active Power-up States ---
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

    /// <summary>
    /// Initializes a new game state, loading player upgrades and calculating starting stats.
    /// </summary>
    public GameState()
    {
        // Load durability (lives) from stored player upgrades
        Lives = Preferences.Get("DurabilityLevel", 1);

        // Calculate initial fuel consumption logic
        UpdateConsumptionInterval();

        // Calculate maximum fuel capacity based on tank upgrades
        int fuelLevel = Preferences.Get("FuelTankLevel", 1);
        Player.MaxFuel = 100f + (fuelLevel - 1) * 35f;
        Player.CurrentFuel = Player.MaxFuel;
    }

    /// <summary>
    /// Recalculates the rate at which fuel is consumed based on the Engine Efficiency upgrade.
    /// </summary>
    public void UpdateConsumptionInterval()
    {
        int engineLevel = Preferences.Get("EngineEfficiencyLevel", 1);

        // Tuning parameters for fuel consumption:
        // Base interval: fuel is consumed very frequently (every 0.20s)
        float baseInterval = 0.20f;

        // Efficiency bonus: reduces the frequency of consumption slightly per level
        float efficiencyBonus = (engineLevel - 1) * 0.005f;

        // Final calculation: capped at 0.15s to maintain a high difficulty even at max level
        CurrentFuelConsumptionInterval = Math.Max(baseInterval - efficiencyBonus, 0.15f);
    }
}