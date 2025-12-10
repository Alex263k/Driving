namespace Driving.Models;

public class Collectible : GameEntity
{
    public int Type { get; set; }
    public int Lane { get; set; }

    public Collectible(float width, float height, int lane)
    {
        Width = width;
        Height = height;
        Lane = lane;

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