namespace Driving.Models;

public class GameEntity
{
    // Позиция на холсте
    public float X { get; set; }
    public float Y { get; set; }

    // Размеры объекта
    public float Width { get; set; }
    public float Height { get; set; }

    // Скорость (для врагов и бустов)
    public float Speed { get; set; }
}