using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using ConfigCreator.Core;
using ConfigCreator.Core.Models;
using ConfigCreator.Core.Services;

namespace ConfigCreator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AutoexecGenerator _generator = new();
        private ConfigProfile _profile = new();
        private Keybind _currentRecordingKeybind = null;

        public ObservableCollection<Keybind> Keybinds { get; } = new();
        public ObservableCollection<Fixture> AvailableFixtures { get; } = new();
        public ObservableCollection<Fixture> SelectedFixtures { get; } = new();

        private string _configPreview;
        public string ConfigPreview
        {
            get => _configPreview;
            set { _configPreview = value; OnPropertyChanged(); }
        }

        private bool _isRecording = false;
        public bool IsRecording
        {
            get => _isRecording;
            set { _isRecording = value; OnPropertyChanged(); }
        }

        public ICommand AddFixtureCommand { get; }
        public ICommand RecordKeybindCommand { get; }
        public ICommand GenerateConfigCommand { get; }

        public MainViewModel()
        {
            LoadData();
            AddFixtureCommand = new RelayCommand<object>(AddFixture);
            RecordKeybindCommand = new RelayCommand<object>(StartRecording);
            GenerateConfigCommand = new RelayCommand(GenerateConfig);
        }

        private void LoadData()
        {
            var keybinds = KeybindService.LoadKeybindsAsync().Result;
            foreach (var bind in keybinds)
            {
                Keybinds.Add(bind);
            }

            var fixtures = FixtureService.LoadFixturesAsync().Result;
            foreach (var fixture in fixtures)
            {
                AvailableFixtures.Add(fixture);
            }

            OnPropertyChanged(nameof(Keybinds));
            OnPropertyChanged(nameof(AvailableFixtures));
        }

        private void StartRecording(object obj)
        {
            if (obj is Keybind keybind)
            {
                _currentRecordingKeybind = keybind;
                IsRecording = true;
                Console.WriteLine($"🎤 Recording keybind for {keybind.Action}");

                // Hook into the next key press
                Application.Current.MainWindow.PreviewKeyDown += OnKeyPressed;
                Application.Current.MainWindow.PreviewMouseDown += OnMousePressed;
            }
        }

        private void OnKeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_currentRecordingKeybind != null)
            {
                _currentRecordingKeybind.Key = e.Key.ToString().ToLower();
                StopRecording();
            }
        }

        private void OnMousePressed(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_currentRecordingKeybind != null)
            {
                _currentRecordingKeybind.Key = e.ChangedButton.ToString().ToLower();
                StopRecording();
            }
        }

        private void StopRecording()
        {
            Console.WriteLine($"✅ Recorded keybind: {_currentRecordingKeybind.Action} = {_currentRecordingKeybind.Key}");

            // Remove event listeners
            Application.Current.MainWindow.PreviewKeyDown -= OnKeyPressed;
            Application.Current.MainWindow.PreviewMouseDown -= OnMousePressed;

            _currentRecordingKeybind = null;
            IsRecording = false;

            // Refresh UI
            OnPropertyChanged(nameof(Keybinds));
            UpdateConfigPreview();
        }

        private void AddFixture(object obj)
        {
            if (obj is Fixture fixture && !SelectedFixtures.Contains(fixture))
            {
                SelectedFixtures.Add(fixture);
                UpdateConfigPreview();
            }
        }

        private void GenerateConfig()
        {
            _profile.ConfigBlocks.Clear();

            var keybindsBlock = new ConfigBlock("Keybinds");
            foreach (var kb in Keybinds)
            {
                keybindsBlock.Commands.Add($"bind \"{kb.Key}\" \"{kb.Action}\"");
            }
            _profile.ConfigBlocks.Add(keybindsBlock);

            foreach (var fixture in SelectedFixtures)
            {
                var fixtureBlock = new ConfigBlock(fixture.Name) { Commands = fixture.Content.Split('\n').ToList() };
                _profile.ConfigBlocks.Add(fixtureBlock);
            }

            ConfigPreview = _profile.GenerateConfig();
            OnPropertyChanged(nameof(ConfigPreview));
        }

        private void UpdateConfigPreview()
        {
            _profile.ConfigBlocks.Clear();

            var keybindsBlock = new ConfigBlock("Keybinds");
            foreach (var kb in Keybinds)
            {
                keybindsBlock.Commands.Add($"bind \"{kb.Key}\" \"{kb.Action}\"");
            }
            _profile.ConfigBlocks.Add(keybindsBlock);

            foreach (var fixture in SelectedFixtures)
            {
                var fixtureBlock = new ConfigBlock(fixture.Name) { Commands = fixture.Content.Split('\n').ToList() };
                _profile.ConfigBlocks.Add(fixtureBlock);
            }

            ConfigPreview = _profile.GenerateConfig();
            OnPropertyChanged(nameof(ConfigPreview));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
