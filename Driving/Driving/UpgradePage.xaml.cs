using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;

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

    // Плееры для звуков
    private IAudioPlayer _upgradeSuccessSound;
    private IAudioPlayer _errorSound;
    private IAudioPlayer _backButtonClickSound;

    public UpgradePage()
    {
        InitializeComponent();
        InitializeProgressSegments();
        LoadUpgradeLevels();
        LoadSounds();
    }

    private async void LoadSounds()
    {
        try
        {
            // Загружаем основные звуки интерфейса
            _upgradeSuccessSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("upgrade_success.wav"));
            _errorSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("error_sound.wav"));
            _backButtonClickSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("button_click.wav"));

            // Устанавливаем громкость
            if (_upgradeSuccessSound != null) _upgradeSuccessSound.Volume = 0.6;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AUDIO ERROR: {ex.Message}");
        }
    }

    private void PlaySound(IAudioPlayer player)
    {
        if (player != null)
        {
            if (player.IsPlaying) player.Stop();
            player.Play();
        }
    }

    private void InitializeProgressSegments()
    {
        _durabilitySegments = new List<BoxView> { DurabilitySegment1, DurabilitySegment2, DurabilitySegment3, DurabilitySegment4, DurabilitySegment5, DurabilitySegment6, DurabilitySegment7, DurabilitySegment8, DurabilitySegment9, DurabilitySegment10 };
        _speedSegments = new List<BoxView> { SpeedSegment1, SpeedSegment2, SpeedSegment3, SpeedSegment4, SpeedSegment5, SpeedSegment6, SpeedSegment7, SpeedSegment8, SpeedSegment9, SpeedSegment10 };
        _fuelTankSegments = new List<BoxView> { FuelTankSegment1, FuelTankSegment2, FuelTankSegment3, FuelTankSegment4, FuelTankSegment5, FuelTankSegment6, FuelTankSegment7, FuelTankSegment8, FuelTankSegment9, FuelTankSegment10 };
        _engineSegments = new List<BoxView> { EngineSegment1, EngineSegment2, EngineSegment3, EngineSegment4, EngineSegment5, EngineSegment6, EngineSegment7, EngineSegment8, EngineSegment9, EngineSegment10 };
    }

    private void LoadUpgradeLevels()
    {
        _durabilityLevel = Math.Clamp(Preferences.Get(DurabilityLevelKey, 1), 1, MaxUpgradeLevel);
        _speedLevel = Math.Clamp(Preferences.Get(SpeedLevelKey, 1), 1, MaxUpgradeLevel);
        _fuelTankLevel = Math.Clamp(Preferences.Get(FuelTankLevelKey, 1), 1, MaxUpgradeLevel);
        _engineEfficiencyLevel = Math.Clamp(Preferences.Get(EngineEfficiencyLevelKey, 1), 1, MaxUpgradeLevel);

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
        for (int i = 0; i < MaxUpgradeLevel; i++)
        {
            _durabilitySegments[i].Color = i < _durabilityLevel ? Color.FromArgb("#22c55e") : Color.FromArgb("#4b5563");
            _speedSegments[i].Color = i < _speedLevel ? Color.FromArgb("#3b82f6") : Color.FromArgb("#4b5563");
            _fuelTankSegments[i].Color = i < _fuelTankLevel ? Color.FromArgb("#f59e0b") : Color.FromArgb("#4b5563");
            _engineSegments[i].Color = i < _engineEfficiencyLevel ? Color.FromArgb("#10b981") : Color.FromArgb("#4b5563");
        }

        DurabilityLevelLabel.Text = $"{_durabilityLevel}/{MaxUpgradeLevel}";
        SpeedLevelLabel.Text = $"{_speedLevel}/{MaxUpgradeLevel}";
        FuelTankLevelLabel.Text = $"{_fuelTankLevel}/{MaxUpgradeLevel}";
        EngineLevelLabel.Text = $"{_engineEfficiencyLevel}/{MaxUpgradeLevel}";

        DurabilityPriceLabel.Text = _durabilityLevel >= MaxUpgradeLevel ? "MAX" : $"{GetDurabilityPrice(_durabilityLevel)} 🪙";
        SpeedPriceLabel.Text = _speedLevel >= MaxUpgradeLevel ? "MAX" : $"{GetSpeedPrice(_speedLevel)} 🪙";
        FuelTankPriceLabel.Text = _fuelTankLevel >= MaxUpgradeLevel ? "MAX" : $"{GetFuelTankPrice(_fuelTankLevel)} 🪙";
        EnginePriceLabel.Text = _engineEfficiencyLevel >= MaxUpgradeLevel ? "MAX" : $"{GetEngineEfficiencyPrice(_engineEfficiencyLevel)} 🪙";
    }

    private void UpdateFuelUpgradeInfo()
    {
        float maxFuel = 100f + (_fuelTankLevel - 1) * 35f;
        FuelTankEffectLabel.Text = $"Max fuel: {maxFuel:F0}L";

        // БЫЛО: baseInterval = 0.333f, бонус = 0.033f
        // СТАЛО: Базовый интервал меньше (чаще расход), бонус за уровень слабее
        float baseInterval = 0.25f; // Топливо начинает тратиться чаще (каждые 0.25с вместо 0.33с)
        float efficiencyBonus = (_engineEfficiencyLevel - 1) * 0.01f; // Бонус стал в 3 раза слабее (0.01с вместо 0.033с)

        // Минимальный интервал теперь 0.18с. Даже на макс. уровне топливо будет улетать быстро.
        float actualInterval = Math.Max(baseInterval - efficiencyBonus, 0.18f);

        EngineEffectLabel.Text = $"Consumption: every {actualInterval:F2}s";
    }

    private void UpdateUpgradeButtons()
    {
        int playerCoins = Preferences.Get(TotalCoinsKey, 0);
        UpdateBtnState(UpgradeDurabilityButton, _durabilityLevel, GetDurabilityPrice(_durabilityLevel), playerCoins, "#10b981");
        UpdateBtnState(UpgradeSpeedButton, _speedLevel, GetSpeedPrice(_speedLevel), playerCoins, "#10b981");
        UpdateBtnState(FuelTankUpgradeButton, _fuelTankLevel, GetFuelTankPrice(_fuelTankLevel), playerCoins, "#3b82f6");
        UpdateBtnState(EngineUpgradeButton, _engineEfficiencyLevel, GetEngineEfficiencyPrice(_engineEfficiencyLevel), playerCoins, "#3b82f6");
    }

    private void UpdateBtnState(Button btn, int level, int price, int playerCoins, string activeHex)
    {
        if (level >= MaxUpgradeLevel) { btn.Text = "MAX LEVEL"; btn.BackgroundColor = Colors.Gray; btn.IsEnabled = false; }
        else { btn.IsEnabled = true; btn.BackgroundColor = playerCoins >= price ? Color.FromArgb(activeHex) : Color.FromArgb("#6b7280"); }
    }

    private int GetDurabilityPrice(int currentLevel) => 100 * currentLevel;
    private int GetSpeedPrice(int currentLevel) => 100 * currentLevel;
    private int GetFuelTankPrice(int currentLevel) => 50 * currentLevel;
    private int GetEngineEfficiencyPrice(int currentLevel) => 75 * currentLevel;

    // ОБРАБОТЧИКИ НАЖАТИЙ СО ЗВУКОМ ДЛЯ ВСЕХ УЛУЧШЕНИЙ
    private async void OnUpgradeDurabilityClicked(object sender, EventArgs e) => await ProcessUpgrade(DurabilityLevelKey, GetDurabilityPrice(_durabilityLevel), () => _durabilityLevel++, (Button)sender);
    private async void OnUpgradeSpeedClicked(object sender, EventArgs e) => await ProcessUpgrade(SpeedLevelKey, GetSpeedPrice(_speedLevel), () => _speedLevel++, (Button)sender);
    private async void OnFuelTankUpgradeClicked(object sender, EventArgs e) => await ProcessUpgrade(FuelTankLevelKey, GetFuelTankPrice(_fuelTankLevel), () => _fuelTankLevel++, (Button)sender);
    private async void OnEngineUpgradeClicked(object sender, EventArgs e) => await ProcessUpgrade(EngineEfficiencyLevelKey, GetEngineEfficiencyPrice(_engineEfficiencyLevel), () => _engineEfficiencyLevel++, (Button)sender);

    private async Task ProcessUpgrade(string key, int price, Action incrementAction, Button btn)
    {
        int coins = Preferences.Get(TotalCoinsKey, 0);
        if (coins >= price)
        {
            PlaySound(_upgradeSuccessSound); // Звук успеха
            Preferences.Set(TotalCoinsKey, coins - price);
            incrementAction();
            Preferences.Set(key, Preferences.Get(key, 1) + 1); // Здесь аккуратно с логикой инкремента

            // Чтобы гарантировать сохранение правильного значения из переменной:
            if (key == DurabilityLevelKey) Preferences.Set(key, _durabilityLevel);
            if (key == SpeedLevelKey) Preferences.Set(key, _speedLevel);
            if (key == FuelTankLevelKey) Preferences.Set(key, _fuelTankLevel);
            if (key == EngineEfficiencyLevelKey) Preferences.Set(key, _engineEfficiencyLevel);

            LoadUpgradeLevels();
            await btn.ScaleTo(0.9, 50); await btn.ScaleTo(1.0, 50);
        }
        else
        {
            PlaySound(_errorSound); // Звук ошибки (недостаточно монет)
            await btn.TranslateTo(10, 0, 50); await btn.TranslateTo(-10, 0, 50); await btn.TranslateTo(0, 0, 50);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        PlaySound(_backButtonClickSound);
        await Navigation.PopAsync();
    }
}