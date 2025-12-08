using System.Collections.Generic;

namespace Driving.Models;

public class Enemy : GameEntity
{
    // Идентификатор, чтобы потом рисовать разные модели/цвета
    public int Type { get; set; }

    // Какую полосу занимает враг (0, 1 или 2)
    public int Lane { get; set; }

    public Enemy(float width, float height, int lane, float initialSpeed)
    {
        Width = width;
        Height = height;
        Lane = lane;
        Speed = initialSpeed;

        // Враги всегда появляются за пределами верхнего края
        Y = -height;

        // Позиция X будет рассчитана в Spawner или GameDrawable
    }

    /// <summary>
    /// Вычисляет X-координату врага на основе его полосы.
    /// </summary>
    public float CalculateX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        float laneWidth = screenWidth / 3f;
        float centerOfLane = (laneWidth * Lane) + (laneWidth / 2f);

        return centerOfLane - (Width / 2f);
    }
}