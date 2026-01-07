using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;

namespace Driving;

/// <summary>
/// Logic for the vehicle upgrade system. 
/// Handles persistent storage, visual progress bars, and audio feedback.
/// </summary>
public partial class UpgradePage : ContentPage
{
    // Storage Keys for Preferences
    private const string TotalCoinsKey = "TotalCoins";
    private const string DurabilityLevelKey = "DurabilityLevel";
    private const string SpeedLevelKey = "SpeedLevel";
    private const string FuelTankLevelKey = "FuelTankLevel";
    private const string EngineEfficiencyLevelKey = "EngineEfficiencyLevel";

    // Configuration constants
    private const int MaxUpgradeLevel = 10;

    // Internal state variables
    private int _durabilityLevel = 1;
    private int _speedLevel = 1;
    private int _fuelTankLevel = 1;
    private int _engineEfficiencyLevel = 1;

    // Lists to manage UI progress segments efficiently
    private List<BoxView> _durabilitySegments = new List<BoxView>();
    private List<BoxView> _speedSegments = new List<BoxView>();
    private List<BoxView> _fuelTankSegments = new List<BoxView>();
    private List<BoxView> _engineSegments = new List<BoxView>();

    // Audio Players for UI feedback and Background Music
    private IAudioPlayer _upgradeSuccessSound;
    private IAudioPlayer _errorSound;
    private IAudioPlayer _backButtonClickSound;
    private IAudioPlayer _backgroundMusic;

    public UpgradePage()
    {
        InitializeComponent();
        InitializeProgressSegments();
        LoadUpgradeLevels();
        LoadSounds();
    }

    /// <summary>
    /// Loads all necessary audio files and starts the background theme.
    /// </summary>
    private async void LoadSounds()
    {
        try
        {
            // Initialize sound effects (SFX)
            _upgradeSuccessSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("upgrade_success.wav"));
            _errorSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("error_sound.wav"));
            _backButtonClickSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("car_switch.wav"));

            // Инициализация новой фоновой музыки (Gameplay Theme)
            _backgroundMusic = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("gameplay_theme.mp3"));

            if (_backgroundMusic != null)
            {
                _backgroundMusic.Loop = true;     // Цикличное воспроизведение
                _backgroundMusic.Volume = 0.4;    // Умеренная громкость
                _backgroundMusic.Play();
            }

            // Настройка громкости эффектов
            if (_upgradeSuccessSound != null) _upgradeSuccessSound.Volume = 0.6;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AUDIO INITIALIZATION ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper to play a sound effect from the beginning.
    /// </summary>
    private void PlaySound(IAudioPlayer player)
    {
        if (player != null)
        {
            if (player.IsPlaying) player.Stop();
            player.Play();
        }
    }

    /// <summary>
    /// Groups XAML BoxViews into lists for easier programmatic coloring.
    /// </summary>
    private void InitializeProgressSegments()
    {
        _durabilitySegments = new List<BoxView> { DurabilitySegment1, DurabilitySegment2, DurabilitySegment3, DurabilitySegment4, DurabilitySegment5, DurabilitySegment6, DurabilitySegment7, DurabilitySegment8, DurabilitySegment9, DurabilitySegment10 };
        _speedSegments = new List<BoxView> { SpeedSegment1, SpeedSegment2, SpeedSegment3, SpeedSegment4, SpeedSegment5, SpeedSegment6, SpeedSegment7, SpeedSegment8, SpeedSegment9, SpeedSegment10 };
        _fuelTankSegments = new List<BoxView> { FuelTankSegment1, FuelTankSegment2, FuelTankSegment3, FuelTankSegment4, FuelTankSegment5, FuelTankSegment6, FuelTankSegment7, FuelTankSegment8, FuelTankSegment9, FuelTankSegment10 };
        _engineSegments = new List<BoxView> { EngineSegment1, EngineSegment2, EngineSegment3, EngineSegment4, EngineSegment5, EngineSegment6, EngineSegment7, EngineSegment8, EngineSegment9, EngineSegment10 };
    }

    /// <summary>
    /// Synchronizes local variables with Preferences and updates the UI.
    /// </summary>
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

    /// <summary>
    /// Stops the background music when navigating away to prevent overlap with gameplay audio.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_backgroundMusic != null)
        {
            if (_backgroundMusic.IsPlaying) _backgroundMusic.Stop();
            _backgroundMusic.Dispose();
            _backgroundMusic = null;
        }
    }

    /// <summary>
    /// Sets the colors of the progress segments based on the current level of each upgrade.
    /// </summary>
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

        float baseInterval = 0.25f;
        float efficiencyBonus = (_engineEfficiencyLevel - 1) * 0.01f;
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

    // Price scaling logic
    private int GetDurabilityPrice(int currentLevel) => 100 * currentLevel;
    private int GetSpeedPrice(int currentLevel) => 100 * currentLevel;
    private int GetFuelTankPrice(int currentLevel) => 50 * currentLevel;
    private int GetEngineEfficiencyPrice(int currentLevel) => 75 * currentLevel;

    // Interaction Handlers
    private async void OnUpgradeDurabilityClicked(object sender, EventArgs e) => await ProcessUpgrade(DurabilityLevelKey, GetDurabilityPrice(_durabilityLevel), () => _durabilityLevel++, (Button)sender);
    private async void OnUpgradeSpeedClicked(object sender, EventArgs e) => await ProcessUpgrade(SpeedLevelKey, GetSpeedPrice(_speedLevel), () => _speedLevel++, (Button)sender);
    private async void OnFuelTankUpgradeClicked(object sender, EventArgs e) => await ProcessUpgrade(FuelTankLevelKey, GetFuelTankPrice(_fuelTankLevel), () => _fuelTankLevel++, (Button)sender);
    private async void OnEngineUpgradeClicked(object sender, EventArgs e) => await ProcessUpgrade(EngineEfficiencyLevelKey, GetEngineEfficiencyPrice(_engineEfficiencyLevel), () => _engineEfficiencyLevel++, (Button)sender);

    private async Task ProcessUpgrade(string key, int price, Action incrementAction, Button btn)
    {
        int coins = Preferences.Get(TotalCoinsKey, 0);
        if (coins >= price)
        {
            PlaySound(_upgradeSuccessSound);
            Preferences.Set(TotalCoinsKey, coins - price);
            incrementAction();

            if (key == DurabilityLevelKey) Preferences.Set(key, _durabilityLevel);
            if (key == SpeedLevelKey) Preferences.Set(key, _speedLevel);
            if (key == FuelTankLevelKey) Preferences.Set(key, _fuelTankLevel);
            if (key == EngineEfficiencyLevelKey) Preferences.Set(key, _engineEfficiencyLevel);

            LoadUpgradeLevels();
            await btn.ScaleTo(0.9, 50); await btn.ScaleTo(1.0, 50);
        }
        else
        {
            PlaySound(_errorSound);
            await btn.TranslateTo(10, 0, 50); await btn.TranslateTo(-10, 0, 50); await btn.TranslateTo(0, 0, 50);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        PlaySound(_backButtonClickSound);
        await Navigation.PopAsync();
    }
}