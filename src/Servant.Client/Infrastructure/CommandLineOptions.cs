using CommandLine;
using CommandLine.Text;

namespace Servant.Client.Infrastructure
{
    class CommandLineOptions
    {
        [Option('i', "install", HelpText = "Install the Servant Client as a Windows service")]
        public bool Install { get; set; }

        [Option('u', "uninstall", HelpText = "Uninstall the Servant Client Windows service")]
        public bool Uninstall { get; set; }

        [Option('k', "key", DefaultValue = null, HelpText = "Change your Servant.io key by using this argument: --key <key-goes-here>")]
        public string Key { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
