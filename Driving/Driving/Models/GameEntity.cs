namespace Driving.Models;

public class GameEntity
{
    // Position on canvas
    public float X { get; set; }
    public float Y { get; set; }

    // Object dimensions
    public float Width { get; set; }
    public float Height { get; set; }

    // Speed (for enemies and bonuses)
    public float Speed { get; set; }
}