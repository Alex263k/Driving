using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace Driving;

/// <summary>
/// Main menu logic. Handles car selection, stats, and background music management.
/// </summary>
public partial class StartPage : ContentPage
{
    // Storage keys for game data
    private const string HighScoreKey = "HighScore";
    private const string TotalCoinsKey = "TotalCoins";
    private const string GamesPlayedKey = "GamesPlayed";
    private const string SelectedCarKey = "SelectedCar";
    private const string CustomSkinPathKey = "CustomSkinPath";
    private const string IsMutedKey = "IsMuted";

    public class CarInfo
    {
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.LimeGreen;
        public string Emoji { get; set; } = "🚗";
        public string? CustomImagePath { get; set; }
    }

    private List<CarInfo> _cars = new List<CarInfo>();
    private int _currentCarIndex = 0;
    private bool _isMuted = false;

    // Audio players
    private IAudioPlayer _buttonClickSound;
    private IAudioPlayer _carSwitchSound;
    private IAudioPlayer _startGameSound;
    private IAudioPlayer _backgroundMusic;

    public StartPage()
    {
        InitializeComponent();
        InitializeCars();

        // Load mute state
        _isMuted = Preferences.Get(IsMutedKey, false);

        LoadSounds();
        UpdateMuteButton();
    }

    /// <summary>
    /// Loads SFX and the background MP3 theme.
    /// </summary>
    private async void LoadSounds()
    {
        try
        {
            // Load SFX from MauiAsset
            _buttonClickSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("button_click.wav"));
            _carSwitchSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("car_switch.wav"));
            _startGameSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("start_game.wav"));

            // Load Background Music
            _backgroundMusic = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("menu_theme.mp3"));

            // Setup volume and loop
            if (_buttonClickSound != null) _buttonClickSound.Volume = _isMuted ? 0 : 0.5;
            if (_carSwitchSound != null) _carSwitchSound.Volume = _isMuted ? 0 : 0.5;
            if (_startGameSound != null) _startGameSound.Volume = _isMuted ? 0 : 0.7;

            if (_backgroundMusic != null)
            {
                _backgroundMusic.Volume = _isMuted ? 0 : 0.4;
                _backgroundMusic.Loop = true;
                _backgroundMusic.Play(); // Start playing on load
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! AUDIO ERROR: {ex.Message}");
        }
    }

    private void PlaySound(IAudioPlayer player)
    {
        if (player != null && !_isMuted)
        {
            if (player.IsPlaying) player.Stop();
            player.Play();
        }
    }

    /// <summary>
    /// Helper to safely stop the background music.
    /// </summary>
    private void StopBackgroundMusic()
    {
        if (_backgroundMusic != null && _backgroundMusic.IsPlaying)
        {
            _backgroundMusic.Stop();
        }
    }

    private void InitializeCars()
    {
        string? customSkinPath = Preferences.Get(CustomSkinPathKey, string.Empty);
        if (!string.IsNullOrEmpty(customSkinPath) && !File.Exists(customSkinPath))
            customSkinPath = null;

        _cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" },
            new CarInfo { Name = "CUSTOM", Color = Colors.Purple, Emoji = "🎨", CustomImagePath = customSkinPath }
        };

        _currentCarIndex = Preferences.Get(SelectedCarKey, 0);
        if (_currentCarIndex >= _cars.Count) _currentCarIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAndDisplayStats();
        UpdateCarDisplay();

        // Resume background music when returning from other pages
        if (_backgroundMusic != null && !_backgroundMusic.IsPlaying && !_isMuted)
        {
            _backgroundMusic.Play();
        }
    }

    private void LoadAndDisplayStats()
    {
        HighScoreLabel.Text = Preferences.Get(HighScoreKey, 0).ToString();
        CoinsLabel.Text = Preferences.Get(TotalCoinsKey, 0).ToString();
        GamesPlayedLabel.Text = Preferences.Get(GamesPlayedKey, 0).ToString();
    }

    private void UpdateCarDisplay()
    {
        if (_cars.Count == 0 || _currentCarIndex >= _cars.Count) return;

        var currentCar = _cars[_currentCarIndex];
        CarBody.Color = currentCar.Color;
        CarNameLabel.Text = currentCar.Name;
        CarEmojiLabel.Text = currentCar.Emoji;

        bool isCustomCar = currentCar.Name == "CUSTOM";
        CustomCarImage.IsVisible = isCustomCar && !string.IsNullOrEmpty(currentCar.CustomImagePath);
        CarBody.IsVisible = !CustomCarImage.IsVisible;

        if (isCustomCar && !string.IsNullOrEmpty(currentCar.CustomImagePath) && File.Exists(currentCar.CustomImagePath))
        {
            try { CustomCarImage.Source = ImageSource.FromFile(currentCar.CustomImagePath); }
            catch { CustomCarImage.IsVisible = false; CarBody.IsVisible = true; }
        }

        UploadCustomButton.IsVisible = isCustomCar;
        PrevCarButton.IsEnabled = _currentCarIndex > 0;
        NextCarButton.IsEnabled = _currentCarIndex < _cars.Count - 1;
        PrevCarButton.Opacity = PrevCarButton.IsEnabled ? 1.0 : 0.5;
        NextCarButton.Opacity = NextCarButton.IsEnabled ? 1.0 : 0.5;
    }

    private void UpdateMuteButton()
    {
        MuteButton.Text = _isMuted ? "🔇" : "🔊";

        // Update all audio players volume
        if (_buttonClickSound != null) _buttonClickSound.Volume = _isMuted ? 0 : 0.5;
        if (_carSwitchSound != null) _carSwitchSound.Volume = _isMuted ? 0 : 0.5;
        if (_startGameSound != null) _startGameSound.Volume = _isMuted ? 0 : 0.7;
        if (_backgroundMusic != null)
        {
            _backgroundMusic.Volume = _isMuted ? 0 : 0.4;
            if (_isMuted && _backgroundMusic.IsPlaying)
            {
                _backgroundMusic.Stop();
            }
            else if (!_isMuted && !_backgroundMusic.IsPlaying)
            {
                _backgroundMusic.Play();
            }
        }
    }

    private async void OnPrevCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex > 0)
        {
            PlaySound(_carSwitchSound);
            _currentCarIndex--;
            UpdateCarDisplay();
            await CarDisplayGrid.TranslateTo(-30, 0, 100);
            CarDisplayGrid.TranslationX = 30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnNextCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex < _cars.Count - 1)
        {
            PlaySound(_carSwitchSound);
            _currentCarIndex++;
            UpdateCarDisplay();
            await CarDisplayGrid.TranslateTo(30, 0, 100);
            CarDisplayGrid.TranslationX = -30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnUploadCustomClicked(object sender, EventArgs e)
    {
        PlaySound(_buttonClickSound);
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select skin", FileTypes = FilePickerFileType.Images });
            if (result == null) return;

            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "custom_car.png");
            using (var sourceStream = await result.OpenReadAsync())
            using (var targetStream = File.Create(targetPath))
            {
                await sourceStream.CopyToAsync(targetStream);
            }

            Preferences.Set(CustomSkinPathKey, targetPath);
            _cars[_currentCarIndex].CustomImagePath = targetPath;
            UpdateCarDisplay();
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnUpgradesClicked(object sender, EventArgs e)
    {
        PlaySound(_buttonClickSound);
        StopBackgroundMusic();
        await Navigation.PushAsync(new UpgradePage());
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        PlaySound(_startGameSound);
        StopBackgroundMusic();

        Preferences.Set(GamesPlayedKey, Preferences.Get(GamesPlayedKey, 0) + 1);
        Preferences.Set(SelectedCarKey, _currentCarIndex);

        var selectedCar = (_currentCarIndex < _cars.Count) ? _cars[_currentCarIndex] : _cars[0];
        await Navigation.PushAsync(new GamePage(selectedCar));
    }

    public static CarInfo GetSelectedCar()
    {
        int selectedIndex = Preferences.Get(SelectedCarKey, 0);
        string customPath = Preferences.Get(CustomSkinPathKey, string.Empty);

        var cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" },
            new CarInfo { Name = "CUSTOM", Color = Colors.Purple, Emoji = "🎨", CustomImagePath = customPath }
        };

        return (selectedIndex >= 0 && selectedIndex < cars.Count) ? cars[selectedIndex] : cars[0];
    }

    private void OnMuteClicked(object sender, EventArgs e)
    {
        _isMuted = !_isMuted;
        Preferences.Set(IsMutedKey, _isMuted);
        UpdateMuteButton();

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch { /* Ignore if haptic feedback is not supported */ }
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Reset Progress",
            "Are you sure you want to reset all progress? This will delete all coins, high score, and custom skins.",
            "Yes, Reset",
            "Cancel"
        );

        if (confirmed)
        {
            Preferences.Clear();

            string customSkinPath = FileSystem.AppDataDirectory + "/custom_car.png";
            if (File.Exists(customSkinPath))
            {
                try { File.Delete(customSkinPath); }
                catch { }
            }

            _isMuted = false;
            UpdateMuteButton();

            _currentCarIndex = 0;

            InitializeCars();
            LoadAndDisplayStats();
            UpdateCarDisplay();

            await DisplayAlert("Reset Complete", "All progress has been reset.", "OK");

            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            }
            catch { }
        }
    }
}