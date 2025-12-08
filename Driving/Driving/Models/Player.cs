namespace Driving.Models;

public class Player : GameEntity
{
    // Текущая полоса: 0 (левая), 1 (центр), 2 (правая)
    public int CurrentLane { get; set; } = 1;

    public Player()
    {
        // Устанавливаем размер машины игрока
        Width = 50;
        Height = 80;
        // Y-позиция будет определяться в GameDrawable
    }

    /// <summary>
    /// Изменяет полосу движения игрока, ограничивая от 0 до 2.
    /// </summary>
    /// <param name="direction">Направление: -1 для влево, 1 для вправо.</param>
    public void ChangeLane(int direction)
    {
        CurrentLane = Math.Clamp(CurrentLane + direction, 0, 2);
    }

    /// <summary>
    /// Вычисляет X-координату игрока, центрируя его в текущей полосе.
    /// </summary>
    public float CalculateX(float screenWidth)
    {
        if (screenWidth <= 0) return 0;

        // Ширина одной полосы
        float laneWidth = screenWidth / 3f;

        // Находим горизонтальный центр текущей полосы
        float centerOfLane = (laneWidth * CurrentLane) + (laneWidth / 2f);

        // Возвращаем X-координату для центрирования
        return centerOfLane - (Width / 2f);
    }
}