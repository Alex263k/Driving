namespace Driving.Models;

public class Player : GameEntity
{
    // GameEntity provides X, Y, Width, Height
    public int CurrentLane { get; set; } = 1; // 0, 1, or 2

    // Animation properties
    public bool IsAnimating { get; set; } = false;
    public int AnimationFramesTotal { get; set; } = 0;
    public int AnimationFramesRemaining { get; set; } = 0;

    public float StartX { get; set; }
    public float TargetX { get; set; }
    public float VisualX { get; set; } // Actual X position used by GameDrawable
    public float VisualY { get; set; } // Actual Y position used by GameDrawable (for forward lean)

    public Player()
    {
        Width = 60f;
        Height = 100f;
        CurrentLane = 1;
        // VisualX and VisualY will be initialized in MainPage.xaml.cs
    }

    // Calculates the fixed X position based on the final lane
    public float CalculateLaneX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * CurrentLane) + (laneWidth / 2f);

        // Center the car in the lane
        return centerOfLane - (Width / 2f);
    }
}