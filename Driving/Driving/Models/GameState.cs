using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Driving.Models;

public class GameState
{
    public bool IsRunning { get; set; } = false;
    public bool IsGameOver { get; set; } = false;

    // Screen dimensions (updated by the view)
    public float ScreenWidth { get; set; }
    public float ScreenHeight { get; set; }

    public float Speed { get; set; } = 10f; // Base speed
    public float Score { get; set; } = 0;

    // Simple timer to track total game time
    public DateTime StartTime { get; set; }

    public Player Player { get; set; } = new Player();
}