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

        private CommandViewModel _selectedCommand;
        public CommandViewModel SelectedCommand
        {
            get => _selectedCommand;
            set { _selectedCommand = value; OnPropertyChanged("SelectedCommand"); }
        }

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
            // EditableParameter is set as a semicolon-separated string.
            // ParameterHelper is joined from the ParameterDescription list.
            // CommandBase is saved so that we can simply pass it through when generating the config.
            Commands = new ObservableCollection<CommandViewModel>(
                defaults.Commands.Select(c => new CommandViewModel
                {
                    Name = c.Name,
                    CommandBase = c.CommandBase,
                    EditableParameter = c.Parameters != null && c.Parameters.Count > 0 ? string.Join(";", c.Parameters) : "",
                    IsEnabled = true,
                    ParameterHelper = c.ParameterDescription != null ? string.Join("; ", c.ParameterDescription) : ""
                })
            );

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // Create a GeneratedConfig from the view models.
            var genConfig = new GeneratedConfig();
            // Map key bindings (we assume Action is the command value)
            genConfig.KeyBindings = KeyBindings
                .Select(k => new Core.Models.KeyBinding { Key = k.Key, Value = k.Action })
                .ToList();

            // Map commands using the default CommandBase from JSON.
            // EditableParameter (entered by the user) is split by semicolons into a list.
            genConfig.Commands = Commands
                .Where(c => c.IsEnabled)
                .Select(c =>
                {
                    var parameters = c.EditableParameter
                        .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();

                    return new Command
                    {
                        Name = c.Name,
                        CommandBase = c.CommandBase,
                        Parameters = parameters
                    };
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

            // Generate configuration text.
            string newConfig = configGenerator.GenerateConfig(genConfig);
            UpdatePreview(newConfig);
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Use SaveFileDialog to let the user select a location to save the file.
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Config Files (*.cfg)|*.cfg|All Files (*.*)|*.*",
                FileName = "autoexec.cfg"
            };

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
            var oldLines = string.IsNullOrEmpty(_previousConfig)
                ? new string[0]
                : _previousConfig.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

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
        // Added CommandBase property so we can access the default template.
        private string _commandBase;
        public string CommandBase
        {
            get => _commandBase;
            set { _commandBase = value; OnPropertyChanged("CommandBase"); }
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
        private string _parameterHelper;
        public string ParameterHelper
        {
            get => _parameterHelper;
            set { _parameterHelper = value; OnPropertyChanged("ParameterHelper"); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
