using CommandLine;

namespace GitBranches
{
    public enum Verbosity
    {
        CSV,
        Compact,
        Normal,
        Contributors,
        Logs
    }

    public class Options
    {
        [Option('p', "path", Required = false, HelpText = "The path to the local git repository")]
        public string Path { get; set; }

        [Option('m', "main", Required = false, Default = "origin/master", HelpText = "The main branch of the git repository")]
        public string MainBranch { get; set; }

        [Option('b', "branch", Required = false, HelpText = "Only include branches which contain this (e.g. -b 123456)")]
        public string Branch { get; set; }

        [Option('c', "contributor", Required = false, HelpText = "Only include branches which contain this contributor (e.g. -c udv)")]
        public string Contributor { get; set; }

        [Option('v', "verbosity", Required = false, Default = Verbosity.Normal, HelpText = "Level of details in the output (Compact, Normal, Contributors, Logs)")]
        public Verbosity Verbosity { get; set; }
    }
}
