namespace Driving.Models;

public class Collectible : GameEntity
{
    // Type 0: Coin (for now)
    public int Type { get; set; }

    // Which lane the collectible is in (0, 1, or 2)
    public int Lane { get; set; }

    public Collectible(float width, float height, int lane)
    {
        Width = width;
        Height = height;
        Lane = lane;

        // Collectibles always appear beyond the top edge
        Y = -height;
    }

    /// <summary>
    /// Calculates the X-coordinate of the collectible based on its lane.
    /// </summary>
    public float CalculateX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        // Center the coin in the lane
        return centerOfLane - (Width / 2f);
    }
}