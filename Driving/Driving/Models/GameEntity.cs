namespace Driving.Models;

/// <summary>
/// The base class for all interactive objects within the game world.
/// Provides the fundamental physical properties required for movement, 
/// rendering, and collision detection.
/// </summary>
public class GameEntity
{
    /// <summary>
    /// The horizontal coordinate of the entity's top-left corner on the canvas.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The vertical coordinate of the entity's top-left corner on the canvas.
    /// Higher values move the entity downward.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The horizontal span of the entity's bounding box used for rendering and hit-testing.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// The vertical span of the entity's bounding box used for rendering and hit-testing.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// The rate of travel for the entity. 
    /// Typically represents downward velocity for obstacles and power-ups.
    /// </summary>
    public float Speed { get; set; }
}