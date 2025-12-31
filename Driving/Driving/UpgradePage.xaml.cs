namespace Driving;

public partial class UpgradePage : ContentPage
{
    private const string TotalCoinsKey = "TotalCoins";
    private const string DurabilityLevelKey = "DurabilityLevel";
    private const string SpeedLevelKey = "SpeedLevel";

    private int _durabilityLevel = 1;
    private int _speedLevel = 1;
    private const int MaxUpgradeLevel = 10;

    private List<BoxView> _durabilitySegments = new List<BoxView>();
    private List<BoxView> _speedSegments = new List<BoxView>();

    public UpgradePage()
    {
        InitializeComponent();
        InitializeProgressSegments();
        LoadUpgradeLevels();
    }

    private void InitializeProgressSegments()
    {
        _durabilitySegments = new List<BoxView>
        {
            DurabilitySegment1, DurabilitySegment2, DurabilitySegment3,
            DurabilitySegment4, DurabilitySegment5, DurabilitySegment6,
            DurabilitySegment7, DurabilitySegment8, DurabilitySegment9,
            DurabilitySegment10
        };

        _speedSegments = new List<BoxView>
        {
            SpeedSegment1, SpeedSegment2, SpeedSegment3,
            SpeedSegment4, SpeedSegment5, SpeedSegment6,
            SpeedSegment7, SpeedSegment8, SpeedSegment9,
            SpeedSegment10
        };
    }

    private void LoadUpgradeLevels()
    {
        _durabilityLevel = Preferences.Get(DurabilityLevelKey, 1);
        _speedLevel = Preferences.Get(SpeedLevelKey, 1);

        if (_durabilityLevel < 1) _durabilityLevel = 1;
        if (_durabilityLevel > MaxUpgradeLevel) _durabilityLevel = MaxUpgradeLevel;
        if (_speedLevel < 1) _speedLevel = 1;
        if (_speedLevel > MaxUpgradeLevel) _speedLevel = MaxUpgradeLevel;

        UpdateUpgradeBars();
        UpdateUpgradeButtons();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUpgradeLevels();
    }

    private void UpdateUpgradeBars()
    {
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _durabilitySegments[i].Color = i < _durabilityLevel ?
                Color.FromArgb("#22c55e") :
                Color.FromArgb("#4b5563");
        }

        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _speedSegments[i].Color = i < _speedLevel ?
                Color.FromArgb("#3b82f6") :
                Color.FromArgb("#4b5563");
        }

        DurabilityLevelLabel.Text = $"{_durabilityLevel}/{MaxUpgradeLevel}";
        SpeedLevelLabel.Text = $"{_speedLevel}/{MaxUpgradeLevel}";

        DurabilityPriceLabel.Text = _durabilityLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetUpgradePrice(_durabilityLevel)} 🪙";
        SpeedPriceLabel.Text = _speedLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetUpgradePrice(_speedLevel)} 🪙";
    }

    private void UpdateUpgradeButtons()
    {
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        if (_durabilityLevel >= MaxUpgradeLevel)
        {
            UpgradeDurabilityButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            UpgradeDurabilityButton.BackgroundColor = Colors.Gray;
            UpgradeDurabilityButton.IsEnabled = false;
        }
        else
        {
            int durabilityPrice = GetUpgradePrice(_durabilityLevel);
            UpgradeDurabilityButton.Text = "УЛУЧШИТЬ";
            UpgradeDurabilityButton.BackgroundColor = playerCoins >= durabilityPrice ?
                Color.FromArgb("#10b981") :
                Color.FromArgb("#6b7280");
            UpgradeDurabilityButton.IsEnabled = playerCoins >= durabilityPrice;
        }

        if (_speedLevel >= MaxUpgradeLevel)
        {
            UpgradeSpeedButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            UpgradeSpeedButton.BackgroundColor = Colors.Gray;
            UpgradeSpeedButton.IsEnabled = false;
        }
        else
        {
            int speedPrice = GetUpgradePrice(_speedLevel);
            UpgradeSpeedButton.Text = "УЛУЧШИТЬ";
            UpgradeSpeedButton.BackgroundColor = playerCoins >= speedPrice ?
                Color.FromArgb("#10b981") :
                Color.FromArgb("#6b7280");
            UpgradeSpeedButton.IsEnabled = playerCoins >= speedPrice;
        }
    }

    private int GetUpgradePrice(int currentLevel)
    {
        return 100 * currentLevel;
    }

    private async void OnUpgradeDurabilityClicked(object sender, EventArgs e)
    {
        if (_durabilityLevel >= MaxUpgradeLevel) return;

        int upgradePrice = GetUpgradePrice(_durabilityLevel);
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        if (playerCoins >= upgradePrice)
        {
            int newCoins = playerCoins - upgradePrice;
            Preferences.Set(TotalCoinsKey, newCoins);

            _durabilityLevel++;
            Preferences.Set(DurabilityLevelKey, _durabilityLevel);

            UpdateUpgradeBars();
            UpdateUpgradeButtons();

            var button = (Button)sender;
            await button.ScaleTo(0.9, 50, Easing.CubicInOut);
            await button.ScaleTo(1.0, 50, Easing.CubicInOut);

            await DisplayAlert("Успех!",
                $"Прочность улучшена до уровня {_durabilityLevel}!\n\nПотрачено: {upgradePrice} 🪙\nОсталось: {newCoins} 🪙",
                "ОК");
        }
        else
        {
            await DisplayAlert("Недостаточно монет",
                $"Вам нужно {upgradePrice} 🪙 для улучшения.\n\nУ вас есть: {playerCoins} 🪙",
                "ОК");
        }
    }

    private async void OnUpgradeSpeedClicked(object sender, EventArgs e)
    {
        if (_speedLevel >= MaxUpgradeLevel) return;

        int upgradePrice = GetUpgradePrice(_speedLevel);
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        if (playerCoins >= upgradePrice)
        {
            int newCoins = playerCoins - upgradePrice;
            Preferences.Set(TotalCoinsKey, newCoins);

            _speedLevel++;
            Preferences.Set(SpeedLevelKey, _speedLevel);

            UpdateUpgradeBars();
            UpdateUpgradeButtons();

            var button = (Button)sender;
            await button.ScaleTo(0.9, 50, Easing.CubicInOut);
            await button.ScaleTo(1.0, 50, Easing.CubicInOut);

            await DisplayAlert("Успех!",
                $"Скорость улучшена до уровня {_speedLevel}!\n\nПотрачено: {upgradePrice} 🪙\nОсталось: {newCoins} 🪙",
                "ОК");
        }
        else
        {
            await DisplayAlert("Недостаточно монет",
                $"Вам нужно {upgradePrice} 🪙 для улучшения.\n\nУ вас есть: {playerCoins} 🪙",
                "ОК");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}