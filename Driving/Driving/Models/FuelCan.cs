namespace Driving.Models;

/// <summary>
/// Represents a fuel replenishment item. 
/// Necessary for extending the gameplay duration as the player's fuel constantly depletes.
/// </summary>
public class FuelCan : GameEntity
{
    /// <summary>
    /// The road lane index (0, 1, or 2) where the fuel canister is located.
    /// </summary>
    public int Lane { get; set; }

    /// <summary>
    /// The amount of fuel units restored to the player upon collection.
    /// Default value is 20 units.
    /// </summary>
    public float FuelAmount { get; set; } = 20f;

    /// <summary>
    /// Initializes a new instance of a fuel canister.
    /// </summary>
    /// <param name="width">The width of the canister hitbox.</param>
    /// <param name="height">The height of the canister hitbox.</param>
    /// <param name="lane">The target lane for placement.</param>
    public FuelCan(float width, float height, int lane)
    {
        Width = width;
        Height = height;
        Lane = lane;

        // Starts the object off-screen at the top for a smooth scrolling appearance
        Y = -height;
    }

    /// <summary>
    /// Computes the horizontal screen coordinate based on the assigned lane.
    /// Ensures the fuel can is perfectly centered within the lane boundaries.
    /// </summary>
    /// <param name="screenWidth">The total width of the current game viewport.</param>
    /// <returns>The calculated X-coordinate for rendering and collision detection.</returns>
    public float CalculateX(float screenWidth)
    {
        // Guard clause to prevent math errors if screen dimensions aren't yet available
        if (screenWidth <= 0) return 0;

        // Logic for 3-lane distribution
        float laneWidth = screenWidth / 3f;

        // Identify the exact middle point of the selected lane
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        // Adjust for the entity's width to ensure its center aligns with the lane's center
        return centerOfLane - (Width / 2f);
    }
}