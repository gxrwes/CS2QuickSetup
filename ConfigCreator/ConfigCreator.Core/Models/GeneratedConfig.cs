using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigCreator.Core.Models
{
    public class GeneratedConfig
    {
        public List<KeyBinding> KeyBindings { get; set; }
        public List<Command> Commands { get; set; }
        public string CustomBindings { get; set; }

    }
}
