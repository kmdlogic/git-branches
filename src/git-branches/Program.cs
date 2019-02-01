using CommandLine;
using System;

namespace GitBranches
{
    partial class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                       .WithParsed<Options>(o =>
                       {
                           new GitBranchAnalyzer(o).Analyze();
                       });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }
}
