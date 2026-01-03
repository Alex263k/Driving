namespace Driving;

public partial class UpgradePage : ContentPage
{
    private const string TotalCoinsKey = "TotalCoins";
    private const string DurabilityLevelKey = "DurabilityLevel";
    private const string SpeedLevelKey = "SpeedLevel";
    private const string FuelTankLevelKey = "FuelTankLevel";
    private const string EngineEfficiencyLevelKey = "EngineEfficiencyLevel";

    private const int MaxUpgradeLevel = 10;

    private int _durabilityLevel = 1;
    private int _speedLevel = 1;
    private int _fuelTankLevel = 1;
    private int _engineEfficiencyLevel = 1;

    private List<BoxView> _durabilitySegments = new List<BoxView>();
    private List<BoxView> _speedSegments = new List<BoxView>();
    private List<BoxView> _fuelTankSegments = new List<BoxView>();
    private List<BoxView> _engineSegments = new List<BoxView>();

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

        _fuelTankSegments = new List<BoxView>
        {
            FuelTankSegment1, FuelTankSegment2, FuelTankSegment3,
            FuelTankSegment4, FuelTankSegment5, FuelTankSegment6,
            FuelTankSegment7, FuelTankSegment8, FuelTankSegment9,
            FuelTankSegment10
        };

        _engineSegments = new List<BoxView>
        {
            EngineSegment1, EngineSegment2, EngineSegment3,
            EngineSegment4, EngineSegment5, EngineSegment6,
            EngineSegment7, EngineSegment8, EngineSegment9,
            EngineSegment10
        };
    }

    private void LoadUpgradeLevels()
    {
        _durabilityLevel = Preferences.Get(DurabilityLevelKey, 1);
        _speedLevel = Preferences.Get(SpeedLevelKey, 1);
        _fuelTankLevel = Preferences.Get(FuelTankLevelKey, 1);
        _engineEfficiencyLevel = Preferences.Get(EngineEfficiencyLevelKey, 1);

        // Ограничиваем уровни
        _durabilityLevel = Math.Clamp(_durabilityLevel, 1, MaxUpgradeLevel);
        _speedLevel = Math.Clamp(_speedLevel, 1, MaxUpgradeLevel);
        _fuelTankLevel = Math.Clamp(_fuelTankLevel, 1, MaxUpgradeLevel);
        _engineEfficiencyLevel = Math.Clamp(_engineEfficiencyLevel, 1, MaxUpgradeLevel);

        UpdateUpgradeBars();
        UpdateUpgradeButtons();
        UpdateFuelUpgradeInfo();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadUpgradeLevels();
    }

    private void UpdateUpgradeBars()
    {
        // Обновляем прочность
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _durabilitySegments[i].Color = i < _durabilityLevel ?
                Color.FromArgb("#22c55e") :
                Color.FromArgb("#4b5563");
        }

        // Обновляем скорость
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _speedSegments[i].Color = i < _speedLevel ?
                Color.FromArgb("#3b82f6") :
                Color.FromArgb("#4b5563");
        }

        // Обновляем топливный бак
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _fuelTankSegments[i].Color = i < _fuelTankLevel ?
                Color.FromArgb("#f59e0b") :
                Color.FromArgb("#4b5563");
        }

        // Обновляем эффективность двигателя
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _engineSegments[i].Color = i < _engineEfficiencyLevel ?
                Color.FromArgb("#10b981") :
                Color.FromArgb("#4b5563");
        }

        // Обновляем текстовые метки
        DurabilityLevelLabel.Text = $"{_durabilityLevel}/{MaxUpgradeLevel}";
        SpeedLevelLabel.Text = $"{_speedLevel}/{MaxUpgradeLevel}";
        FuelTankLevelLabel.Text = $"{_fuelTankLevel}/{MaxUpgradeLevel}";
        EngineLevelLabel.Text = $"{_engineEfficiencyLevel}/{MaxUpgradeLevel}";

        DurabilityPriceLabel.Text = _durabilityLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetDurabilityPrice(_durabilityLevel)} 🪙";
        SpeedPriceLabel.Text = _speedLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetSpeedPrice(_speedLevel)} 🪙";
        FuelTankPriceLabel.Text = _fuelTankLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetFuelTankPrice(_fuelTankLevel)} 🪙";
        EnginePriceLabel.Text = _engineEfficiencyLevel >= MaxUpgradeLevel ? "МАКС." : $"{GetEngineEfficiencyPrice(_engineEfficiencyLevel)} 🪙";
    }

    private void UpdateFuelUpgradeInfo()
    {
        // Расчет эффектов для топливной системы
        float maxFuel = 100f + (_fuelTankLevel - 1) * 25f;
        FuelTankEffectLabel.Text = $"Макс. топливо: {maxFuel}L";

        float consumptionReduction = (_engineEfficiencyLevel - 1) * 10f;
        EngineEffectLabel.Text = $"Расход топлива: -{consumptionReduction}%";
    }

    private void UpdateUpgradeButtons()
    {
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        // Кнопка прочности
        if (_durabilityLevel >= MaxUpgradeLevel)
        {
            UpgradeDurabilityButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            UpgradeDurabilityButton.BackgroundColor = Colors.Gray;
            UpgradeDurabilityButton.IsEnabled = false;
        }
        else
        {
            int durabilityPrice = GetDurabilityPrice(_durabilityLevel);
            UpgradeDurabilityButton.Text = "УЛУЧШИТЬ";
            UpgradeDurabilityButton.BackgroundColor = playerCoins >= durabilityPrice ?
                Color.FromArgb("#10b981") :
                Color.FromArgb("#6b7280");
            UpgradeDurabilityButton.IsEnabled = playerCoins >= durabilityPrice;
        }

        // Кнопка скорости
        if (_speedLevel >= MaxUpgradeLevel)
        {
            UpgradeSpeedButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            UpgradeSpeedButton.BackgroundColor = Colors.Gray;
            UpgradeSpeedButton.IsEnabled = false;
        }
        else
        {
            int speedPrice = GetSpeedPrice(_speedLevel);
            UpgradeSpeedButton.Text = "УЛУЧШИТЬ";
            UpgradeSpeedButton.BackgroundColor = playerCoins >= speedPrice ?
                Color.FromArgb("#10b981") :
                Color.FromArgb("#6b7280");
            UpgradeSpeedButton.IsEnabled = playerCoins >= speedPrice;
        }

        // Кнопка топливного бака
        if (_fuelTankLevel >= MaxUpgradeLevel)
        {
            FuelTankUpgradeButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            FuelTankUpgradeButton.BackgroundColor = Colors.Gray;
            FuelTankUpgradeButton.IsEnabled = false;
        }
        else
        {
            int fuelTankPrice = GetFuelTankPrice(_fuelTankLevel);
            FuelTankUpgradeButton.Text = "УЛУЧШИТЬ";
            FuelTankUpgradeButton.BackgroundColor = playerCoins >= fuelTankPrice ?
                Color.FromArgb("#3b82f6") :
                Color.FromArgb("#6b7280");
            FuelTankUpgradeButton.IsEnabled = playerCoins >= fuelTankPrice;
        }

        // Кнопка эффективности двигателя
        if (_engineEfficiencyLevel >= MaxUpgradeLevel)
        {
            EngineUpgradeButton.Text = "МАКСИМАЛЬНЫЙ УРОВЕНЬ";
            EngineUpgradeButton.BackgroundColor = Colors.Gray;
            EngineUpgradeButton.IsEnabled = false;
        }
        else
        {
            int enginePrice = GetEngineEfficiencyPrice(_engineEfficiencyLevel);
            EngineUpgradeButton.Text = "УЛУЧШИТЬ";
            EngineUpgradeButton.BackgroundColor = playerCoins >= enginePrice ?
                Color.FromArgb("#3b82f6") :
                Color.FromArgb("#6b7280");
            EngineUpgradeButton.IsEnabled = playerCoins >= enginePrice;
        }
    }

    private int GetDurabilityPrice(int currentLevel)
    {
        return 100 * currentLevel;
    }

    private int GetSpeedPrice(int currentLevel)
    {
        return 100 * currentLevel;
    }

    private int GetFuelTankPrice(int currentLevel)
    {
        return 50 * currentLevel;
    }

    private int GetEngineEfficiencyPrice(int currentLevel)
    {
        return 75 * currentLevel;
    }

    private async void OnUpgradeDurabilityClicked(object sender, EventArgs e)
    {
        if (_durabilityLevel >= MaxUpgradeLevel) return;

        int upgradePrice = GetDurabilityPrice(_durabilityLevel);
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

        int upgradePrice = GetSpeedPrice(_speedLevel);
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

    private async void OnFuelTankUpgradeClicked(object sender, EventArgs e)
    {
        if (_fuelTankLevel >= MaxUpgradeLevel) return;

        int upgradePrice = GetFuelTankPrice(_fuelTankLevel);
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        if (playerCoins >= upgradePrice)
        {
            int newCoins = playerCoins - upgradePrice;
            Preferences.Set(TotalCoinsKey, newCoins);

            _fuelTankLevel++;
            Preferences.Set(FuelTankLevelKey, _fuelTankLevel);

            UpdateUpgradeBars();
            UpdateUpgradeButtons();
            UpdateFuelUpgradeInfo();

            var button = (Button)sender;
            await button.ScaleTo(0.9, 50, Easing.CubicInOut);
            await button.ScaleTo(1.0, 50, Easing.CubicInOut);

            await DisplayAlert("Успех!",
                $"Топливный бак улучшен до уровня {_fuelTankLevel}!\n\nПотрачено: {upgradePrice} 🪙\nОсталось: {newCoins} 🪙",
                "ОК");
        }
        else
        {
            await DisplayAlert("Недостаточно монет",
                $"Вам нужно {upgradePrice} 🪙 для улучшения.\n\nУ вас есть: {playerCoins} 🪙",
                "ОК");
        }
    }

    private async void OnEngineUpgradeClicked(object sender, EventArgs e)
    {
        if (_engineEfficiencyLevel >= MaxUpgradeLevel) return;

        int upgradePrice = GetEngineEfficiencyPrice(_engineEfficiencyLevel);
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);

        if (playerCoins >= upgradePrice)
        {
            int newCoins = playerCoins - upgradePrice;
            Preferences.Set(TotalCoinsKey, newCoins);

            _engineEfficiencyLevel++;
            Preferences.Set(EngineEfficiencyLevelKey, _engineEfficiencyLevel);

            UpdateUpgradeBars();
            UpdateUpgradeButtons();
            UpdateFuelUpgradeInfo();

            var button = (Button)sender;
            await button.ScaleTo(0.9, 50, Easing.CubicInOut);
            await button.ScaleTo(1.0, 50, Easing.CubicInOut);

            await DisplayAlert("Успех!",
                $"Эффективность двигателя улучшена до уровня {_engineEfficiencyLevel}!\n\nПотрачено: {upgradePrice} 🪙\nОсталось: {newCoins} 🪙",
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