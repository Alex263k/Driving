namespace Driving.Models;

public class Player : GameEntity // Мы создадим GameEntity позже, пока это просто класс
{
    public float LaneWidth { get; set; } = 100f; // Ширина полосы (будет уточнена в GameDrawable)
    public int CurrentLane { get; set; } = 1; // 0: Левая, 1: Центр, 2: Правая

    // Метод для вычисления X-координаты на основе полосы
    public float CalculateX(float screenWidth)
    {
        // Предполагаем 3 полосы, занимающие всю ширину
        float laneSize = screenWidth / 3f;

        // Центр полосы (0, 1 или 2)
        float targetCenterX = laneSize * CurrentLane + (laneSize / 2);

        // Вычисляем X-координату для размещения машины
        return targetCenterX - (Width / 2f);
    }

    public void ChangeLane(int direction) // direction: -1 (влево) или 1 (вправо)
    {
        CurrentLane = Math.Clamp(CurrentLane + direction, 0, 2);
    }

    // Добавьте минимальные свойства для отрисовки
    public float Width { get; set; } = 50;
    public float Height { get; set; } = 80;
}