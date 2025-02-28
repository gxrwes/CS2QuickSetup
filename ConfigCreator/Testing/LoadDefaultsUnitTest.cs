using ConfigCreator.Core.Models;
using ConfigCreator.Core.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Testing
{
    public class LoadDefaultsTests
    {
        private string tempKeybindsFile;
        private string tempCommandsFile;

        [SetUp]
        public void Setup()
        {
            // Create temporary files with valid JSON content for testing.
            tempKeybindsFile = Path.GetTempFileName();
            tempCommandsFile = Path.GetTempFileName();

            // Write valid keybindings JSON content.
            File.WriteAllText(tempKeybindsFile, @"
            [
                { ""Key"": ""W"", ""Value"": ""+forward"" },
                { ""Key"": ""S"", ""Value"": ""+back"" }
            ]
            ");

            // Write valid commands JSON content.
            File.WriteAllText(tempCommandsFile, @"
            [
                { ""CommandBase"": ""echo {0}"", ""Parameters"": [""Hello World""] },
                { ""CommandBase"": ""alias {0} {1}"", ""Parameters"": [""+radar"", ""+use; cl_radar_always_centered 1; cl_radar_scale 0.15""] }
            ]
            ");
        }

        [TearDown]
        public void TearDown()
        {
            // Delete temporary files after tests.
            if (File.Exists(tempKeybindsFile))
                File.Delete(tempKeybindsFile);
            if (File.Exists(tempCommandsFile))
                File.Delete(tempCommandsFile);
        }

        [Test]
        public void LoadDefaults_WithValidFiles_ShouldLoadBindingsAndCommands()
        {
            // Arrange & Act
            var loader = new LoadDefaults(tempKeybindsFile, tempCommandsFile);

            // Assert keybindings loaded correctly.
            Assert.That(loader.DefaultBindings.Count, Is.EqualTo(2), "KeyBindings count should be 2.");
            Assert.That(loader.DefaultBindings[0].Key, Is.EqualTo("W"), "First key should be 'W'.");
            Assert.That(loader.DefaultBindings[0].Value, Is.EqualTo("+forward"), "First key's command should be '+forward'.");

            // Assert commands loaded correctly.
            Assert.That(loader.Commands.Count, Is.EqualTo(2), "Commands count should be 2.");
            Assert.That(loader.Commands[0].GetReplacedCommand(), Is.EqualTo("echo Hello World"), "Replaced command did not match for echo.");
            Assert.That(loader.Commands[1].GetReplacedCommand(), Is.EqualTo("alias +radar +use; cl_radar_always_centered 1; cl_radar_scale 0.15"), "Replaced command did not match for alias.");
        }

        [Test]
        public void LoadDefaults_WithMissingFiles_ShouldReturnEmptyLists()
        {
            // Arrange: use non-existent file paths.
            string missingKeybinds = Path.Combine(Path.GetTempPath(), "nonexistent_keybinds.json");
            string missingCommands = Path.Combine(Path.GetTempPath(), "nonexistent_commands.json");

            // Act
            var loader = new LoadDefaults(missingKeybinds, missingCommands);

            // Assert that empty lists are returned.
            Assert.That(loader.DefaultBindings, Is.Empty, "DefaultBindings should be empty when file is missing.");
            Assert.That(loader.Commands, Is.Empty, "Commands should be empty when file is missing.");
        }

        [Test]
        public void Command_GetReplacedCommand_ShouldReturnFormattedString()
        {
            // Arrange
            var command = new Command
            {
                CommandBase = "bind {0} {1}",
                Parameters = new List<string> { "i", "+radar" }
            };

            // Act
            string result = command.GetReplacedCommand();

            // Assert
            Assert.That(result, Is.EqualTo("bind i +radar"), "The replaced command did not match the expected output.");
        }

        [Test]
        public void Command_GetReplacedCommand_WithInvalidPlaceholder_ShouldReturnUnformattedCommand()
        {
            // Arrange:
            // CommandBase expects three parameters but only two are provided.
            var command = new Command
            {
                CommandBase = "alias {0} {1} {2}",
                Parameters = new List<string> { "+radar", "+use" }
            };

            // Act
            string result = command.GetReplacedCommand();

            // Assert: When formatting fails, the original CommandBase is returned.
            Assert.That(result, Is.EqualTo("alias {0} {1} {2}"), "When formatting fails, the original CommandBase should be returned.");
        }

        [Test]
        public void CommandParser_ParseCommand_ShouldReturnCorrectCommand()
        {
            // Arrange
            // Input string with quoted tokens.
            string input = "alias \"+radar\" \"+use; cl_radar_always_centered 1; cl_radar_scale 0.15\"";

            // Act
            Command command = CommandParser.ParseCommand(input);

            // Assert: The first token is the command base; the rest are parameters.
            Assert.That(command.CommandBase, Is.EqualTo("alias"), "The command base should be 'alias'.");
            Assert.That(command.Parameters.Count, Is.EqualTo(2), "There should be exactly 2 parameters.");
            Assert.That(command.Parameters[0], Is.EqualTo("+radar"), "The first parameter should be '+radar'.");
            Assert.That(command.Parameters[1], Is.EqualTo("+use; cl_radar_always_centered 1; cl_radar_scale 0.15"), "The second parameter did not match expected value.");
        }
    }
}
