namespace Driving.Models;

/// <summary>
/// Represents a basic pick-up item (like a coin) within the game world.
/// These entities grant the player score or currency upon collision.
/// </summary>
public class Collectible : GameEntity
{
    /// <summary>
    /// Categorizes the item for different value tiers or visual variations.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// The specific road lane (0, 1, or 2) where the item is positioned.
    /// </summary>
    public int Lane { get; set; }

    /// <summary>
    /// Initializes a new collectible entity.
    /// </summary>
    /// <param name="width">The width of the collectible's hitbox.</param>
    /// <param name="height">The height of the collectible's hitbox.</param>
    /// <param name="lane">The lane index where it should appear.</param>
    public Collectible(float width, float height, int lane)
    {
        Width = width;
        Height = height;
        Lane = lane;

        // Spawns the entity just above the screen bounds to allow for a natural entrance
        Y = -height;
    }

    /// <summary>
    /// Maps the abstract Lane index to a physical horizontal pixel coordinate.
    /// </summary>
    /// <param name="screenWidth">Total available width of the rendering surface.</param>
    /// <returns>The calculated X position for the left edge of the collectible.</returns>
    public float CalculateX(float screenWidth)
    {
        // Prevent calculation if the screen has not yet been measured
        if (screenWidth <= 0) return 0;

        // Logic for a standard 3-lane highway layout
        float laneWidth = screenWidth / 3f;

        // Find the absolute horizontal center of the target lane
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        // Substract half the entity's width so that the object itself is centered in the lane
        return centerOfLane - (Width / 2f);
    }
}