using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfigCreator.Core.Models
{
    public class Command
    {
        public string Name { get; set; }
        public string CommandBase { get; set; }
        public List<string> Parameters { get; set; }
        public List<string> ParameterDescription { get; set; }  // Helper text for each parameter

        /// <summary>
        /// Replaces placeholders in CommandBase with parameters.
        /// If formatting fails, returns the unformatted CommandBase.
        /// </summary>
        public string GetReplacedCommand()
        {
            if (Parameters == null || Parameters.Count == 0)
                return CommandBase;

            try
            {
                return string.Format(CommandBase, Parameters.ToArray());
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Formatting error: {ex.Message}");
                return CommandBase;
            }
        }
    }

    public static class CommandParser
    {
        /// <summary>
        /// Parses an input string into a Command.
        /// Supports quoted tokens.
        /// </summary>
        public static Command ParseCommand(string input)
        {
            var pattern = @"""([^""]+)""|(\S+)";
            var matches = Regex.Matches(input, pattern);
            var tokens = new List<string>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups[1].Success)
                    tokens.Add(match.Groups[1].Value);
                else if (match.Groups[2].Success)
                    tokens.Add(match.Groups[2].Value);
            }

            if (tokens.Count == 0)
                return null;

            return new Command
            {
                CommandBase = tokens[0],
                Parameters = tokens.Count > 1 ? tokens.GetRange(1, tokens.Count - 1) : new List<string>()
            };
        }
    }
}
