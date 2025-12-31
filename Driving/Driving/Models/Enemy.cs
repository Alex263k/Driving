using System.Collections.Generic;

namespace Driving.Models;

public class Enemy : GameEntity
{
    // Identifier for drawing different models/colors
    public enum EnemyType
    {
        RegularCar,     // Regular car
        Truck,          // Truck (wider, slower)
        Motorcycle,     // Motorcycle (faster, narrower)
        Police          // Police car (chases player)
    }

    public EnemyType Type { get; set; }

    // Which lane the enemy occupies (0, 1, or 2)
    public int Lane { get; set; }

    public Enemy(float width, float height, int lane, float initialSpeed, EnemyType type = EnemyType.RegularCar)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Speed = initialSpeed;
        Type = type;

        // Enemies always spawn above the top edge
        Y = -height;
    }


    /// <summary>
    /// Calculates the X-coordinate of the enemy based on its lane.
    /// </summary>
    public float CalculateX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        return centerOfLane - (Width / 2f);
    }
}