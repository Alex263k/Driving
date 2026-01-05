namespace Driving.Models;

public class FuelCan : GameEntity
{
    public int Lane { get; set; }
    public float FuelAmount { get; set; } = 20f; // How much fuel it adds

    public FuelCan(float width, float height, int lane)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Y = -height;
    }

    public float CalculateX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        return centerOfLane - (Width / 2f);
    }
}