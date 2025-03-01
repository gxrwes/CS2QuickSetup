using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConfigCreator.Core.Models;
using ConfigCreator.Core.Service;
using ConfigCreator.Core; // For Version
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Testing
{
    [TestFixture]
    internal class ConfigGeneratorUnitTest
    {
        private ConfigGenerator generator;

        [SetUp]
        public void Setup()
        {
            generator = new ConfigGenerator();
        }

        [Test]
        public void GenerateConfig_WithValidGeneratedConfig_ShouldContainExpectedSections()
        {
            // Arrange: Create a GeneratedConfig with dynamic key bindings and commands.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>
                {
                    new KeyBinding { Key = "w", Value = "+forward" },
                    new KeyBinding { Key = "s", Value = "+back" }
                },
                Commands = new List<Command>
                {
                    new Command
                    {
                        Name = "Test Echo",
                        CommandBase = "echo {0}",
                        Parameters = new List<string> { "\"Hello World\"" }
                    },
                    new Command
                    {
                        Name = "Test Alias",
                        CommandBase = "alias {0} {1}",
                        Parameters = new List<string> { "\"+radar\"", "\"+use; cl_radar_always_centered 1; cl_radar_scale 0.15\"" }
                    }
                }
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check for header, static sections, and dynamic parts.
            Assert.That(output, Does.Contain("CS2 Autoexec"), "Output should contain the plain text header 'CS2 Autoexec'.");
            Assert.That(output, Does.Contain("Graphics Settings"), "Output should contain the Graphics Settings section.");
            Assert.That(output, Does.Contain("Sound Settings"), "Output should contain the Sound Settings section.");
            Assert.That(output, Does.Contain("bind \"w\" \"+forward\""), "Output should contain key binding for 'w'.");
            Assert.That(output, Does.Contain("echo \"Hello World\""), "Output should contain the echo command for 'Hello World'.");
            Assert.That(output, Does.Contain("alias \"+radar\" \"+use; cl_radar_always_centered 1; cl_radar_scale 0.15\""),
                "Output should contain the alias command for +radar.");
        }

        [Test]
        public void GenerateConfig_WithEmptyDynamicSections_ShouldShowFallbackMessages()
        {
            // Arrange: Create a GeneratedConfig with empty dynamic sections.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>(),
                Commands = new List<Command>()
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check that fallback messages appear.
            Assert.That(output, Does.Contain($"{ConfigGenerator.CommentChar} No key bindings configured"),
                "Should indicate no key bindings configured.");
            Assert.That(output, Does.Contain($"{ConfigGenerator.CommentChar} No commands configured"),
                "Should indicate no commands configured.");
        }

        [Test]
        public void GenerateConfig_WithNullDynamicSections_ShouldShowFallbackMessages()
        {
            // Arrange: Create a GeneratedConfig with null dynamic sections.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = null,
                Commands = null
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check fallback messages.
            Assert.That(output, Does.Contain($"{ConfigGenerator.CommentChar} No key bindings configured"),
                "Null key bindings should result in a fallback message.");
            Assert.That(output, Does.Contain($"{ConfigGenerator.CommentChar} No commands configured"),
                "Null commands should result in a fallback message.");
        }

        [Test]
        public void GenerateConfig_WithMisconfiguredCommand_ShouldOutputOriginalCommandBase()
        {
            // Arrange: Create a misconfigured command (mismatch between placeholders and parameters).
            var misconfiguredCommand = new Command
            {
                Name = "Misconfigured Command",
                CommandBase = "alias {0} {1} {2}",
                Parameters = new List<string> { "\"+radar\"", "\"+use\"" }
            };

            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>(),
                Commands = new List<Command> { misconfiguredCommand }
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: The GetReplacedCommand should catch the FormatException and return the original CommandBase.
            Assert.That(output, Does.Contain("alias {0} {1} {2}"),
                "For misconfigured command, the original command base should be output.");
        }

        [Test]
        public void GenerateConfig_Header_ShouldContainVersionInfoAndAsciiArt()
        {
            // Arrange: Use an empty GeneratedConfig.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>(),
                Commands = new List<Command>()
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check that the header includes the plain text header, echoed version info, and generation date.
            Assert.That(output, Does.Contain("CS2 Autoexec"), "Header should include plain text 'CS2 Autoexec'.");
            Assert.That(output, Does.Contain("echo \"Generated by"), "Header should include echoed version info.");
            Assert.That(output, Does.Contain("v1.0.2"), "Header should include the version number. Did you perhaps change the Major or Patch Number?");
            Assert.That(output, Does.Contain("echo \"Generated on"), "Header should include echoed generation date.");
        }

        [Test]
        public void GenerateConfig_DynamicMinimapZoomCommand_ShouldBeCombinedIntoSingleBlock()
        {
            // Arrange: Create a GeneratedConfig that includes a Minimap Zoom command.
            var minimapCommand = new Command
            {
                Name = "Minimap Zoom",
                CommandBase = "echo {0}\nalias {1} {2}\nalias {3} {4}\nbind {5} {6}",
                Parameters = new List<string>
                {
                    "\"Setting Minimap Zoom\"",
                    "\"+radar\"",
                    "\"+use; cl_radar_always_centered 1; cl_radar_scale 0.15\"",
                    "\"-radar\"",
                    "\"-use; cl_radar_always_centered 0; cl_radar_scale 0.90\"",
                    "\"i\"",
                    "\"+radar\""
                }
            };

            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>(),
                Commands = new List<Command> { minimapCommand }
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check that the entire minimap block appears as one unit.
            StringAssert.Contains("echo \"Setting Minimap Zoom\"", output, "Minimap block should contain the echo line.");
            StringAssert.Contains("alias \"+radar\" \"+use; cl_radar_always_centered 1; cl_radar_scale 0.15\"", output,
                "Minimap block should contain the first alias line.");
            StringAssert.Contains("alias \"-radar\" \"-use; cl_radar_always_centered 0; cl_radar_scale 0.90\"", output,
                "Minimap block should contain the second alias line.");
            StringAssert.Contains("bind \"i\" \"+radar\"", output, "Minimap block should contain the bind command.");
        }

        [Test]
        public void GenerateConfig_WithPartialDynamicSections_ShouldHandleMissingPartsGracefully()
        {
            // Arrange: Create a config with only key bindings provided and null commands.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>
                {
                    new KeyBinding { Key = "d", Value = "+right" }
                },
                Commands = null
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);

            // Assert: Check that key binding is output and commands fallback message appears.
            Assert.That(output, Does.Contain("bind \"d\" \"+right\""), "Key binding for 'd' should be present.");
            Assert.That(output, Does.Contain($"{ConfigGenerator.CommentChar} No commands configured"),
                "Missing commands should yield a fallback message.");
        }

        [Test]
        public void GenerateConfig_LinesEndWithSemicolon()
        {
            // Arrange: Create a simple GeneratedConfig with at least one key binding and one command.
            var generatedConfig = new GeneratedConfig
            {
                KeyBindings = new List<KeyBinding>
                {
                    new KeyBinding { Key = "x", Value = "+example" }
                },
                Commands = new List<Command>
                {
                    new Command { Name = "Test", CommandBase = "echo \"{0}\"", Parameters = new List<string> { "\"Test Value\"" } }
                }
            };

            // Act
            string output = generator.GenerateConfig(generatedConfig);
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Assert: For every non-empty line that does not start with the comment character,
            // ensure it ends with a semicolon.
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith(ConfigGenerator.CommentChar))
                {
                    Assert.That(trimmed, Does.EndWith(";"), $"Line '{trimmed}' should end with a semicolon.");
                }
            }
        }
    }
}
