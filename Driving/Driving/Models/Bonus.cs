namespace Driving.Models;

public class Bonus : GameEntity
{
    public enum BonusType
    {
        Shield,        // Shield (temporary invulnerability)
        Magnet,        // Magnet (auto coin collection)
        SlowMotion,    // Slow motion
        Multiplier,    // x2 score multiplier
        CoinRain       // Coin rain
    }

    public BonusType Type { get; set; }
    public int Lane { get; set; }

    public Bonus(float width, float height, int lane, BonusType type)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Type = type;

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