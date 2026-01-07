namespace Driving.Models;

/// <summary>
/// Represents the player's vehicle. 
/// Handles lane-based movement, transition animations, and fuel consumption logic.
/// </summary>
public class Player : GameEntity
{
    // --- Navigation State ---

    /// <summary>
    /// The current logical lane index (0 = Left, 1 = Center, 2 = Right).
    /// </summary>
    public int CurrentLane { get; set; } = 1;

    // --- Visual Interpolation (Smooth Movement) ---

    /// <summary>
    /// Indicates if the player is currently transitioning between lanes.
    /// </summary>
    public bool IsAnimating { get; set; } = false;

    /// <summary>
    /// The total duration of the current lane-switch animation in frames.
    /// </summary>
    public int AnimationFramesTotal { get; set; } = 0;

    /// <summary>
    /// The number of frames remaining until the current animation completes.
    /// </summary>
    public int AnimationFramesRemaining { get; set; } = 0;

    /// <summary>
    /// The horizontal starting point of a lane-switch movement.
    /// </summary>
    public float StartX { get; set; }

    /// <summary>
    /// The horizontal target point for the lane-switch movement.
    /// </summary>
    public float TargetX { get; set; }

    /// <summary>
    /// The precise X-coordinate currently being rendered on screen.
    /// Use this for drawing to achieve smooth sub-pixel movement.
    /// </summary>
    public float VisualX { get; set; }

    /// <summary>
    /// The precise Y-coordinate currently being rendered on screen.
    /// Useful for adding visual effects like tilting or leaning during acceleration.
    /// </summary>
    public float VisualY { get; set; }

    // --- Fuel Management System ---

    /// <summary>
    /// Current amount of fuel remaining in the tank.
    /// </summary>
    public float CurrentFuel { get; set; }

    /// <summary>
    /// The maximum capacity of the fuel tank, influenced by upgrades.
    /// </summary>
    public float MaxFuel { get; set; }

    /// <summary>
    /// The base amount of fuel consumed per second during normal operation.
    /// </summary>
    public float FuelConsumptionRate { get; set; } = 1.1f;

    /// <summary>
    /// Initializes a new Player instance with default dimensions and fuel stats.
    /// </summary>
    public Player()
    {
        Width = 60f;
        Height = 100f;
        CurrentLane = 1;

        // Initialize with high default fuel to balance the high consumption rate
        MaxFuel = 120f;
        CurrentFuel = MaxFuel;
    }

    /// <summary>
    /// Calculates the static horizontal position required to center the player 
    /// within their current logical lane.
    /// </summary>
    /// <param name="screenWidth">The total width of the game viewport.</param>
    /// <returns>The X-coordinate for the left edge of the car hitbox.</returns>
    public float CalculateLaneX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * CurrentLane) + (laneWidth / 2f);

        // Center the vehicle's footprint within the lane segment
        return centerOfLane - (Width / 2f);
    }
}