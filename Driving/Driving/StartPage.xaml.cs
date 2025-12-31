namespace Driving;

public partial class StartPage : ContentPage
{
    // Keys for storing game data
    private const string HighScoreKey = "HighScore";
    private const string TotalCoinsKey = "TotalCoins";
    private const string GamesPlayedKey = "GamesPlayed";
    private const string SelectedCarKey = "SelectedCar";
    private const string DurabilityLevelKey = "DurabilityLevel";
    private const string SpeedLevelKey = "SpeedLevel";

    // Car class for storing car data
    public class CarInfo
    {
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.LimeGreen;
        public string Emoji { get; set; } = "🚗";
    }

    private List<CarInfo> _cars = new List<CarInfo>();
    private int _currentCarIndex = 0;

    public StartPage()
    {
        InitializeComponent();
        InitializeCars();
    }

    private void InitializeCars()
    {
        // Initialize car collection (all cars unlocked)
        _cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" }
        };

        // Load selected car
        _currentCarIndex = Preferences.Get(SelectedCarKey, 0);
        if (_currentCarIndex >= _cars.Count)
        {
            _currentCarIndex = 0;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAndDisplayStats();
        UpdateCarDisplay();
    }

    private void LoadAndDisplayStats()
    {
        // Load all statistics from preferences
        int highScore = Preferences.Get(HighScoreKey, 0);
        int totalCoins = Preferences.Get(TotalCoinsKey, 0);
        int gamesPlayed = Preferences.Get(GamesPlayedKey, 0);

        // Update labels with loaded data
        HighScoreLabel.Text = highScore.ToString();
        CoinsLabel.Text = totalCoins.ToString();
        GamesPlayedLabel.Text = gamesPlayed.ToString();
    }

    private void UpdateCarDisplay()
    {
        if (_cars.Count == 0 || _currentCarIndex >= _cars.Count) return;

        var currentCar = _cars[_currentCarIndex];

        // Update car display
        CarBody.Color = currentCar.Color;
        CarNameLabel.Text = currentCar.Name;

        // Update navigation buttons
        PrevCarButton.IsEnabled = _currentCarIndex > 0;
        NextCarButton.IsEnabled = _currentCarIndex < _cars.Count - 1;

        PrevCarButton.Opacity = PrevCarButton.IsEnabled ? 1.0 : 0.5;
        NextCarButton.Opacity = NextCarButton.IsEnabled ? 1.0 : 0.5;
    }

    private async void OnPrevCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex > 0)
        {
            _currentCarIndex--;
            UpdateCarDisplay();

            // Animate button
            var button = (Button)sender;
            await button.ScaleTo(0.8, 100, Easing.CubicInOut);
            await button.ScaleTo(1.0, 100, Easing.CubicInOut);

            // Animate car change
            await CarDisplayGrid.TranslateTo(-30, 0, 100);
            CarDisplayGrid.TranslationX = 30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnNextCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex < _cars.Count - 1)
        {
            _currentCarIndex++;
            UpdateCarDisplay();

            // Animate button
            var button = (Button)sender;
            await button.ScaleTo(0.8, 100, Easing.CubicInOut);
            await button.ScaleTo(1.0, 100, Easing.CubicInOut);

            // Animate car change
            await CarDisplayGrid.TranslateTo(30, 0, 100);
            CarDisplayGrid.TranslationX = -30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnUpgradesClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        await button.ScaleTo(0.95, 100, Easing.CubicInOut);
        await button.ScaleTo(1.0, 100, Easing.CubicInOut);

        await Navigation.PushAsync(new UpgradePage());
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        await button.ScaleTo(0.95, 100, Easing.CubicInOut);
        await button.ScaleTo(1.0, 100, Easing.CubicInOut);

        // Increment games played counter
        int gamesPlayed = Preferences.Get(GamesPlayedKey, 0);
        Preferences.Set(GamesPlayedKey, gamesPlayed + 1);

        // Save selected car
        Preferences.Set(SelectedCarKey, _currentCarIndex);

        // Navigate to the Game scene with selected car info
        if (_currentCarIndex < _cars.Count)
        {
            var selectedCar = _cars[_currentCarIndex];
            await Navigation.PushAsync(new GamePage(selectedCar));
        }
        else
        {
            // Fallback to default car
            var defaultCar = new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" };
            await Navigation.PushAsync(new GamePage(defaultCar));
        }
    }

    // Static method to get selected car for other pages
    public static CarInfo GetSelectedCar()
    {
        int selectedIndex = Preferences.Get(SelectedCarKey, 0);
        var cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" }
        };

        if (selectedIndex >= 0 && selectedIndex < cars.Count)
        {
            return cars[selectedIndex];
        }

        return cars[0]; // Default to BASIC car
    }
}