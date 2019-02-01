using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace GitBranches
{
    class GitBranchAnalyzer
    {
        private readonly Options options;

        private DirectoryInfo repoDir;

        public GitBranchAnalyzer(Options options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public object BranchType { get; private set; }

        private class BranchCommitter
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public DateTimeOffset FirstCommit { get; set; }
            public DateTimeOffset LastCommit { get; set; }
            public List<string> MessageShorts { get; set; }
        }

        private class BranchDetails
        {
            public string BranchName { get; set; }
            public int Commits { get; set; }
            public List<BranchCommitter> Committers { get; set; }
            public DateTimeOffset FirstCommit { get; set; }
            public DateTimeOffset LastCommit { get; set; }
            public string LastSha { get; set; }
        }

        public void Analyze()
        {
            var repoPath = GetRepoPath();

            var branches = new List<BranchDetails>();

            using (var repo = new Repository(repoPath.FullName))
            {
                var mainBranch = repo.Branches[$"refs/remotes/{options.MainBranch}"];
                if (mainBranch == null) throw new Exception($"Unknown main branch {options.MainBranch}");

                var mainCommits = new HashSet<string>(mainBranch.Commits.Select(x => x.Sha));

                foreach (var branch in repo.Branches.Where(x => x.IsRemote && x != mainBranch))
                {
                    var details = new BranchDetails { BranchName = branch.FriendlyName };
                    var committers = new Dictionary<string, BranchCommitter>(StringComparer.OrdinalIgnoreCase);
                    Commit mainCommit = null;

                    branches.Add(details);

                    foreach (var commit in branch.Commits)
                    {
                        var committerName = commit.Committer.ToString();
                        BranchCommitter committer;

                        if (mainCommits.Contains(commit.Sha))
                        {
                            if (mainCommit == null)
                            {
                                mainCommit = commit;
                            }
                            continue;
                        }

                        details.Commits++;

                        if (details.Commits == 1)
                        {
                            details.FirstCommit = commit.Committer.When;
                            details.LastCommit = commit.Committer.When;
                            details.LastSha = commit.Sha;
                        }
                        else
                        {
                            if (details.FirstCommit > commit.Committer.When)
                            {
                                details.FirstCommit = commit.Committer.When;
                            }
                            if (details.LastCommit < commit.Committer.When)
                            {
                                details.LastCommit = commit.Committer.When;
                                details.LastSha = commit.Sha;
                            }
                        }

                        if (committers.TryGetValue(committerName, out committer))
                        {
                            committer.Count++;
                            committer.MessageShorts.Add(commit.MessageShort);
                            if (committer.FirstCommit > commit.Committer.When)
                            {
                                committer.FirstCommit = commit.Committer.When;
                            }
                            if (committer.LastCommit < commit.Committer.When)
                            {
                                committer.LastCommit = commit.Committer.When;
                            }
                        }
                        else
                        { 
                            committer = new BranchCommitter
                            {
                                Name = committerName,
                                Count = 1,
                                MessageShorts = new List<string> { commit.MessageShort },
                                FirstCommit = commit.Committer.When,
                                LastCommit = commit.Committer.When
                            };

                            committers.Add(committerName, committer);
                        }                        
                    }

                    if (committers.Count == 0 && mainCommit != null)
                    {
                        details.FirstCommit = mainCommit.Committer.When;
                        details.LastCommit = mainCommit.Committer.When; 
                        details.LastSha = mainCommit.Sha;

                        var committerName = mainCommit.Committer.ToString();

                        var committer = new BranchCommitter
                        {
                            Name = committerName,
                            Count = 0,
                            MessageShorts = new List<string>(),
                            FirstCommit = mainCommit.Committer.When,
                            LastCommit = mainCommit.Committer.When
                        };

                        committers.Add(committerName, committer);
                    }

                    details.Committers = committers.Values.ToList();

                    details.Committers.Sort((x, y) =>
                    {
                        var r = y.Count - x.Count;
                        if (r != 0) return r;

                        r = y.LastCommit.CompareTo(x.LastCommit);
                        if (r != 0) return r;

                        return string.Compare(x.Name, y.Name, true);
                    });
                }
            }

            if (!string.IsNullOrEmpty(options.Branch))
            {
                branches = branches.Where(x =>
                    x.BranchName.Contains(options.Branch, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (!string.IsNullOrEmpty(options.Contributor))
            {
                branches = branches.Where(x => 
                    x.Committers.Any(y => y.Name.Contains(options.Contributor, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            branches.Sort((x, y) =>
            {
                var r = x.LastCommit.CompareTo(y.LastCommit);
                if (r != 0) return r;

                return string.Compare(x.BranchName, y.BranchName, true);
            });

            if (options.Verbosity == Verbosity.CSV)
            {
                Console.WriteLine("Branch Name,Last Commit,Commit Hash,Commits,Contributor(s)");
            }
            else
            {
                Console.WriteLine($"Found {branches.Count:N0} remote branches");
            }

            foreach (var branch in branches)
            {
                if (options.Verbosity == Verbosity.CSV)
                {
                    Console.WriteLine($"\"{branch.BranchName}\",{branch.LastCommit:dd MMM yyyy HH:mm:ss zzz},\"{branch.LastSha}\",{branch.Commits:N0}," + string.Join(",", branch.Committers.Select(x => "\"" + x.Name + "\"")));
                }
                else if (options.Verbosity == Verbosity.Compact)
                {
                    var contributors = GetContributors(branch);

                    Console.WriteLine($"{branch.BranchName}: {branch.LastCommit:dd MMM yyyy HH:mm:ss zzz} [{branch.Commits:N0} commit(s)] by {contributors} @ {branch.LastSha}");
                }
                else
                {
                    Console.WriteLine($"{branch.BranchName}:");
                    if (options.Verbosity == Verbosity.Normal)
                    {
                        Console.WriteLine($" Contributors: {GetContributors(branch)}");
                        Console.WriteLine($" Unmerged Commits: {branch.Commits:N0}");
                    }
                    switch (branch.Commits)
                    {
                        case 0:
                            Console.WriteLine($" Branch Commit: {branch.LastCommit:dd MMM yyyy HH:mm:ss zzz} [Hash {branch.LastSha}]");
                            break;

                        case 1:
                            Console.WriteLine($" Commit: {branch.LastCommit:dd MMM yyyy HH:mm:ss zzz} [Hash {branch.LastSha}]");
                            break;

                        default:
                            Console.WriteLine($" First Commit: {branch.FirstCommit:dd MMM yyyy HH:mm:ss zzz}");
                            Console.WriteLine($" Last Commit: {branch.LastCommit:dd MMM yyyy HH:mm:ss zzz} [Hash {branch.LastSha}]");
                            break;
                    }

                    if (options.Verbosity >= Verbosity.Contributors)
                    {
                        Console.WriteLine($" Contributors: {branch.Committers.Count:N0}");
                        foreach (var committer in branch.Committers)
                        {
                            var details = $"  * {committer.Name}: {committer.FirstCommit:dd MMM yyyy HH:mm:ss zzz} to {committer.LastCommit:dd MMM yyyy HH:mm:ss zzz}";

                            if (options.Verbosity == Verbosity.Logs)
                            {
                                Console.WriteLine(details);
                                foreach (var msg in committer.MessageShorts)
                                {
                                    Console.WriteLine($"   - {msg}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"{details} [{ committer.Count:N0} commit(s)]");
                            }
                        }
                    }
                }
            }
        }

        private static string GetContributors(BranchDetails branch)
        {
            const int OwnerCount = 3;
            var owners = string.Empty;
            if (branch.Committers.Count > 0)
            {
                owners = string.Join(", ", branch.Committers.Take(OwnerCount).Select(x => x.Name));
                if (branch.Committers.Count > OwnerCount)
                {
                    owners += $" + {branch.Committers.Count - OwnerCount:N0} other(s)";
                }
            }

            return owners;
        }

        private DirectoryInfo GetRepoPath()
        {
            if (string.IsNullOrEmpty(options.Path))
            {
                repoDir = new DirectoryInfo(".");
            }
            else
            {
                var dirInfo = new DirectoryInfo(options.Path);
                if (!dirInfo.Exists)
                {
                    throw new ArgumentException($"Not a valid path {options.Path}");
                }
            }

            while (repoDir.GetDirectories(".git").Length == 0)
            {
                if (repoDir.Parent == null)
                {
                    throw new ArgumentException($"Unable to locate the root directory of the git repository");
                }

                repoDir = repoDir.Parent;
            }

            return repoDir;
        }
    }
}