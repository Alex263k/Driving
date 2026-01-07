using System.Collections.Generic;

namespace Driving.Models;

/// <summary>
/// Represents an NPC vehicle on the road that acts as an obstacle for the player.
/// Handles different vehicle behaviors and positioning logic within the lane system.
/// </summary>
public class Enemy : GameEntity
{
    /// <summary>
    /// Defines the specific classification of the enemy vehicle.
    /// This determines rendering assets, physical dimensions, and movement behavior.
    /// </summary>
    public enum EnemyType
    {
        RegularCar,     // Standard vehicle with balanced stats
        Truck,          // Large vehicle: wider hitbox, slower movement speed
        Motorcycle,     // Small vehicle: narrow hitbox, high movement speed
        Police          // Special vehicle: potentially includes pursuit AI logic
    }

    /// <summary>
    /// The specific category assigned to this enemy instance.
    /// </summary>
    public EnemyType Type { get; set; }

    /// <summary>
    /// The current lane assignment (typically 0 for left, 1 for center, 2 for right).
    /// Used to calculate the horizontal positioning on the road.
    /// </summary>
    public int Lane { get; set; }

    /// <summary>
    /// Initializes a new instance of an Enemy vehicle.
    /// </summary>
    /// <param name="width">Collision width of the vehicle.</param>
    /// <param name="height">Collision height of the vehicle.</param>
    /// <param name="lane">Initial lane index (0-2).</param>
    /// <param name="initialSpeed">The downward velocity of the vehicle.</param>
    /// <param name="type">The specific vehicle archetype.</param>
    public Enemy(float width, float height, int lane, float initialSpeed, EnemyType type = EnemyType.RegularCar)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Speed = initialSpeed;
        Type = type;

        // Initialize position just above the viewport to ensure a smooth scrolling entry
        Y = -height;
    }

    /// <summary>
    /// Translates the logical lane index into a physical X-coordinate on the screen.
    /// Ensures the vehicle is perfectly centered within its designated lane.
    /// </summary>
    /// <param name="screenWidth">The current width of the game's drawing surface.</param>
    /// <returns>The pixel coordinate for the left edge of the enemy vehicle.</returns>
    public float CalculateX(float screenWidth)
    {
        // Safety check for unitialized or zero-width screen dimensions
        if (screenWidth <= 0) return 0;

        // Divide the road into three equal segments
        float laneWidth = screenWidth / 3f;

        // Calculate the absolute center point of the target lane
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        // Offset the center by half the vehicle's width to align the hitbox correctly
        return centerOfLane - (Width / 2f);
    }
}