using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ConfigCreator.Core.Models;

namespace ConfigCreator.Core.Service
{
    public class LoadDefaults
    {
        public List<KeyBinding> DefaultBindings { get; private set; } = new();
        public List<Command> Commands { get; private set; } = new();

        // Constructor takes two file paths: one for key bindings and one for commands.
        public LoadDefaults(string keybindsFilePath, string commandsFilePath)
        {
            LoadBindingsFromJson(keybindsFilePath);
            LoadCommandsFromJson(commandsFilePath);
        }

        private void LoadBindingsFromJson(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    DefaultBindings = JsonSerializer.Deserialize<List<KeyBinding>>(json) ?? new List<KeyBinding>();
                }
                else
                {
                    Console.WriteLine($"Warning: Default keybinds file not found at {filePath}");
                    DefaultBindings = new List<KeyBinding>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading keybinds: {ex.Message}");
                DefaultBindings = new List<KeyBinding>();
            }
        }

        private void LoadCommandsFromJson(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Commands = JsonSerializer.Deserialize<List<Command>>(json) ?? new List<Command>();
                }
                else
                {
                    Console.WriteLine($"Warning: Default commands file not found at {filePath}");
                    Commands = new List<Command>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading commands: {ex.Message}");
                Commands = new List<Command>();
            }
        }
    }
}
