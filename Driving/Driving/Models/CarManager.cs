using Microsoft.Maui.Graphics;
using System.Collections.Generic;

namespace Driving.Models
{
    /// <summary>
    /// Central registry for all selectable vehicle profiles available in the game.
    /// This static manager serves as the "garage" where car presets are defined.
    /// </summary>
    public static class CarManager
    {
        /// <summary>
        /// A collection of predefined vehicle presets, each with unique visual properties.
        /// These are used to populate the car selection menu and determine player appearance.
        /// </summary>
        public static List<CarInfo> Cars = new List<CarInfo>
        {
            // BASIC: The default starter vehicle
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            
            // SPORTS: A high-performance red aesthetic
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            
            // POLICE: Emergency vehicle skin
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            
            // TAXI: Urban transport themed skin
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            
            // RACING: Professional track-ready appearance
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            
            // VIP: Premium high-status skin (usually for high scorers)
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" }
        };
    }

    /// <summary>
    /// Data structure representing the metadata and visual identity of a specific car.
    /// </summary>
    public class CarInfo
    {
        /// <summary>
        /// Unique identifier for the car type (e.g., "BASIC", "SPORTS").
        /// Defaults to an empty string to prevent null reference issues.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The primary color used for geometry rendering or fallback sprites.
        /// </summary>
        public Color Color { get; set; } = Colors.LimeGreen;

        /// <summary>
        /// Character icon used in simplified UI menus or labels.
        /// </summary>
        public string Emoji { get; set; } = "🚗";
    }
}