namespace GitBranches
{
    using System;
    using CommandLine;

    public static class Program
    {
        public static void Main(string[] args)
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
