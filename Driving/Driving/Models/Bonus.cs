namespace Driving.Models;

/// <summary>
/// Represents a power-up entity that provides temporary advantages to the player.
/// Inherits from GameEntity to utilize standard movement and collision logic.
/// </summary>
public class Bonus : GameEntity
{
    /// <summary>
    /// Defines the unique behavior and visual representation of the power-up.
    /// </summary>
    public enum BonusType
    {
        Shield,        // Protects against one collision or provides timed invulnerability
        Magnet,        // Automatically pulls nearby coins toward the player's position
        SlowMotion,    // Reduces the speed of all enemies and road movement
        Multiplier,    // Doubles the points gained during the active duration
        CoinRain       // Triggers a high-density spawn of collectible coins
    }

    /// <summary>
    /// The specific category of this power-up instance.
    /// </summary>
    public BonusType Type { get; set; }

    /// <summary>
    /// The specific road lane index (0-2) where the bonus is spawned.
    /// </summary>
    public int Lane { get; set; }

    /// <summary>
    /// Initializes a new instance of a Bonus power-up.
    /// </summary>
    /// <param name="width">The hit-box width.</param>
    /// <param name="height">The hit-box height.</param>
    /// <param name="lane">Target lane index.</param>
    /// <param name="type">The specific type of power-up.</param>
    public Bonus(float width, float height, int lane, BonusType type)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Type = type;

        // Position the entity just above the visible screen area for a smooth entry
        Y = -height;
    }

    /// <summary>
    /// Converts the lane index into a physical horizontal pixel coordinate.
    /// Places the bonus exactly in the center of the specified lane.
    /// </summary>
    /// <param name="screenWidth">The current width of the game canvas.</param>
    /// <returns>The X-coordinate for the left edge of the bonus.</returns>
    public float CalculateX(float screenWidth)
    {
        // Avoid division by zero errors during early initialization
        if (screenWidth <= 0) return 0;

        // Determine lane dimensions (3-lane road system)
        float laneWidth = screenWidth / 3f;

        // Calculate the horizontal midpoint of the specific lane
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        // Offset the center by half the width of the entity to align it perfectly
        return centerOfLane - (Width / 2f);
    }
}