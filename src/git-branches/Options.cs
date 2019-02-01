using CommandLine;

namespace GitBranches
{
    /// <summary>
    /// The level of verbosity in the output
    /// </summary>
    public enum Verbosity
    {
        /// <summary>
        /// Summarise in Comma Separated Value format
        /// </summary>
        CSV,

        /// <summary>
        /// Compact format
        /// </summary>
        Compact,

        /// <summary>
        /// Normal format
        /// </summary>
        Normal,

        /// <summary>
        /// Include the contributors to the branch
        /// </summary>
        Contributors,

        /// <summary>
        /// Include the commit log of the branch
        /// </summary>
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

        [Option('c', "contributor", Required = false, HelpText = "Only include branches which contain this contributor (e.g. -c billy)")]
        public string Contributor { get; set; }

        [Option('v', "verbosity", Required = false, Default = Verbosity.Normal, HelpText = "Level of details in the output (CSV, Compact, Normal, Contributors, Logs)")]
        public Verbosity Verbosity { get; set; }
    }
}
