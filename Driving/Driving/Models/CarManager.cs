using Microsoft.Maui.Graphics;

namespace Driving.Models
{
    public static class CarManager
    {
        public static List<CarInfo> Cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" }
        };
    }

    public class CarInfo
    {
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.LimeGreen;
        public string Emoji { get; set; } = "🚗";
    }
}