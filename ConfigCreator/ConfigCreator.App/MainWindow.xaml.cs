using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using ConfigCreator.Core.Models;
using ConfigCreator.Core.Service;
using Microsoft.Win32;  // For SaveFileDialog

namespace ConfigCreator.App
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Observable collections for binding
        public ObservableCollection<KeyBindingViewModel> KeyBindings { get; set; }
        public ObservableCollection<CommandViewModel> Commands { get; set; }
        public string CustomBindings { get; set; } = "";

        private string _previousConfig = "";
        private ConfigGenerator configGenerator;

        public MainWindow()
        {
            InitializeComponent();
            configGenerator = new ConfigGenerator();

            // Define paths to JSON files (adjust if necessary)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string keybindsFilePath = Path.Combine(baseDir, "Resources", "defaultKeybinds.json");
            string commandsFilePath = Path.Combine(baseDir, "Resources", "defaultCommands.json");

            // Load defaults from JSON files.
            var defaults = new LoadDefaults(keybindsFilePath, commandsFilePath);

            // Map loaded keybindings into the KeyBindings ObservableCollection.
            KeyBindings = new ObservableCollection<KeyBindingViewModel>(
                defaults.DefaultBindings.Select(k => new KeyBindingViewModel
                {
                    Action = k.Value,
                    Key = k.Key
                })
            );

            // Map loaded commands into the Commands ObservableCollection.
            Commands = new ObservableCollection<CommandViewModel>(
                defaults.Commands.Select(c => new CommandViewModel
                {
                    Name = c.Name,
                    EditableParameter = c.Parameters != null && c.Parameters.Count > 0 ? c.Parameters[0] : "",
                    IsEnabled = true
                })
            );

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // Create a GeneratedConfig from the view models.
            var genConfig = new GeneratedConfig();
            // Map key bindings (we assume Action is the command value)
            genConfig.KeyBindings = KeyBindings.Select(k => new Core.Models.KeyBinding { Key = k.Key, Value = k.Action }).ToList();

            // Map commands based on their Name.
            genConfig.Commands = Commands
                .Where(c => c.IsEnabled)
                .Select(c =>
                {
                    if (c.Name == "Loading Message")
                    {
                        return new Command
                        {
                            Name = c.Name,
                            CommandBase = "echo \"{0}\"",
                            Parameters = new System.Collections.Generic.List<string> { c.EditableParameter }
                        };
                    }
                    else if (c.Name == "Minimap Zoom")
                    {
                        // EditableParameter here is assumed to be the scale value.
                        return new Command
                        {
                            Name = c.Name,
                            CommandBase = "echo \"Setting Minimap Zoom\"\nalias \"+radar\" \"+use; cl_radar_always_centered 1; cl_radar_scale {0}\"\nalias \"-radar\" \"-use; cl_radar_always_centered 0; cl_radar_scale {0}\"\nbind \"i\" \"+radar\"",
                            Parameters = new System.Collections.Generic.List<string> { c.EditableParameter }
                        };
                    }
                    else if (c.Name == "Quick Commands")
                    {
                        // Use default parameters for quick commands.
                        return new Command
                        {
                            Name = c.Name,
                            CommandBase = "alias \"{0}\" \"{1}\"\nalias \"{2}\" \"{3}\"",
                            Parameters = new System.Collections.Generic.List<string> { "quit", "exit", "dc", "disconnect" }
                        };
                    }
                    else
                    {
                        return new Command
                        {
                            Name = c.Name,
                            CommandBase = "echo \"{0}\"",
                            Parameters = new System.Collections.Generic.List<string> { c.EditableParameter }
                        };
                    }
                }).ToList();

            // Append custom bindings (if any) directly.
            if (!string.IsNullOrWhiteSpace(txtCustomBindings.Text))
            {
                var customCommand = new Command
                {
                    Name = "Custom Bindings",
                    CommandBase = txtCustomBindings.Text,
                    Parameters = new System.Collections.Generic.List<string>()
                };
                genConfig.Commands.Add(customCommand);
            }

            // Generate configuration text
            string newConfig = configGenerator.GenerateConfig(genConfig);
            UpdatePreview(newConfig);
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Use SaveFileDialog from Microsoft.Win32 to let the user select a location to save the file.
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Config Files (*.cfg)|*.cfg|All Files (*.*)|*.*";
            dlg.FileName = "autoexec.cfg";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, _previousConfig);
                    MessageBox.Show("Configuration downloaded successfully!", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdatePreview(string newConfig)
        {
            // Simple diff: compare new config lines with previous lines.
            rtbPreview.Document.Blocks.Clear();
            var newLines = newConfig.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var oldLines = string.IsNullOrEmpty(_previousConfig) ? new string[0] : _previousConfig.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var paragraph = new Paragraph();
            for (int i = 0; i < newLines.Length; i++)
            {
                string newLine = newLines[i];
                string oldLine = (i < oldLines.Length) ? oldLines[i] : "";

                var run = new Run(newLine + Environment.NewLine);
                run.Foreground = (newLine != oldLine) ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Black;
                paragraph.Inlines.Add(run);
            }
            rtbPreview.Document.Blocks.Add(paragraph);
            _previousConfig = newConfig;
        }

        private bool isRecording = false;
        private KeyBindingViewModel currentRecordingBinding = null;

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            // When the record button is clicked, set the corresponding binding into recording mode.
            var button = sender as Button;
            if (button?.DataContext is KeyBindingViewModel binding)
            {
                if (!isRecording)
                {
                    isRecording = true;
                    currentRecordingBinding = binding;
                    binding.RecordButtonText = "Recording...";
                    // Set focus to the window to capture key input.
                    this.Focus();
                }
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (isRecording && currentRecordingBinding != null)
            {
                // Capture the key press as the new key.
                currentRecordingBinding.Key = e.Key.ToString();
                currentRecordingBinding.RecordButtonText = "Record";
                isRecording = false;
                currentRecordingBinding = null;
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
    }

    // ViewModel for KeyBindings displayed in the DataGrid.
    public class KeyBindingViewModel : INotifyPropertyChanged
    {
        private string _action;
        public string Action
        {
            get => _action;
            set { _action = value; OnPropertyChanged("Action"); }
        }
        private string _key;
        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged("Key"); }
        }
        private string _recordButtonText = "Record";
        public string RecordButtonText
        {
            get => _recordButtonText;
            set { _recordButtonText = value; OnPropertyChanged("RecordButtonText"); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ViewModel for Commands displayed in the DataGrid.
    public class CommandViewModel : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged("Name"); }
        }
        private string _editableParameter;
        public string EditableParameter
        {
            get => _editableParameter;
            set { _editableParameter = value; OnPropertyChanged("EditableParameter"); }
        }
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged("IsEnabled"); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
