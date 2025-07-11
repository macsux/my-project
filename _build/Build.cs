using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.Git;
using Nuke.Common.Utilities;

[HandleHelpRequests(Priority = 20)]
[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
partial class Build : NukeBuild
{
    const string ProjectName = "MyProject";
    const string TargetFramework = "net9.0";

    static Build()
    {
        Environment.SetEnvironmentVariable("NUKE_TELEMETRY_OPTOUT", "true");
        Environment.SetEnvironmentVariable("NoLogo", "true");
    }

    public Build()
    {
        RenderBanner(ProjectName);
    }

    protected override void OnBuildInitialized()
    {
        var config = GitRepository.Config;
        GitUsername ??= config.Get<string>("user.name", ConfigurationLevel.Global)?.Value;
        GitEmail ??= config.Get<string>("user.email", ConfigurationLevel.Global)?.Value;
    }

    public static int Main () => Execute<Build>(x => x.Compile);


    GitHubActions GitHubActions => GitHubActions.Instance;

    [Parameter("Nuget.org API key required to push packages")][Secret] readonly string NugetApiKey;
    [Parameter("Type of release to make")] readonly ReleaseType ReleaseType;
    [Parameter("Git username to use for new commits")] string GitUsername;
    [Parameter("Git email to use for new commits")] string GitEmail;

    [Parameter("Nuget feed to which packages are pushed (default: https://api.nuget.org/v3/index.json)")] readonly string NugetFeed = "https://api.nuget.org/v3/index.json";


    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    string NugetArtifactsGlobPattern => $"*.{Version.NuGetPackageVersion}.nupkg";
    IReadOnlyCollection<AbsolutePath> NugetArtifacts => ArtifactsDirectory.GlobFiles(NugetArtifactsGlobPattern);
    AbsolutePath TestResultsDirectory => ArtifactsDirectory /  "test-results";

    [Solution(GenerateProjects = true)] Solution Solution;
    [NerdbankGitVersioning] NerdbankGitVersioning Version;
    [GitRepositoryExt] Repository GitRepository;
    bool IsCurrentBranchCommitted() => !GitRepository.RetrieveStatus().IsDirty;
    static bool IsGitInitialized => Repository.IsValid(RootDirectory);

    Target Clean => _ => _
        .Description("Clean out artifacts and all the bin/obj directories")
        .Before(Restore)
        .Executes(() =>
        {
            IEnumerable<AbsolutePath> GetSubDirectories(params string[] patterns)
            {
                var options = new EnumerationOptions() { RecurseSubdirectories = true };
                return patterns
                    .SelectMany(pattern => Directory.EnumerateDirectories(Solution.Directory!, pattern, options))
                    .Select(AbsolutePath.Create);
            }
            ArtifactsDirectory.CreateOrCleanDirectory();
            var objBin = GetSubDirectories("obj", "bin");
            foreach (var subDirectory in objBin)
            {
                subDirectory.DeleteDirectory();
            }
            
        });

    Target CleanTestResults => _ => _
        .Description("Clean out test results directory")
        .Executes(() =>
        {
            TestResultsDirectory.CreateOrCleanDirectory();
        });
    

    Target Restore => _ => _
        .Description("Restores nuget packages")
        .Executes(() =>
        {

            DotNetRestore(c => c
                .SetProjectFile(Solution.Path)
                .SetVersion(Version.NuGetPackageVersion));
        });

    Target Compile => _ => _
        .Description("Compiles .Net projects")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(c => c
                .SetProjectFile(Solution.Path)
                .SetVersion(Version.NuGetPackageVersion));

        });


    Target Pack => _ => _
        .Description("Creates nuget packages inside artifacts directory")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetPack(x => x
                .EnableNoRestore()
                .SetProject(Solution.Path)
                .SetVersion(Version.NuGetPackageVersion)
                .SetAssemblyVersion(Version.AssemblyVersion)
                .SetOutputDirectory(ArtifactsDirectory));

        });

    [Category("Test")]
    Target Test => _ => _
        .Description("Runs .NET tests")
        .DependsOn(Restore)
        .Executes(() =>
        {
            var testProjects = Solution.AllSolutionFolders
                .Where(x => x.Name == "tests")
                .SelectMany(x => x.Projects)
                .ToList();
            DotNetTest(x => x
                .SetResultsDirectory(TestResultsDirectory)
                .CombineWith(testProjects, (c,v) => c
                    .SetProjectFile(v)
                    .AddProcessAdditionalArguments("--",
                        "--test-parameter RenderLST=false",
                        "--test-parameter NoAnsi=true",
                        "--no-progress",
                        "--no-ansi",
                        "--disable-logo",
                        "--report-trx",
                        "--output Detailed",
                        "--hide-test-output",
                        $"--report-trx-filename {v.Name}.trx",
                        "--results-directory", TestResultsDirectory
                    )
                )
            );
            // InjectLogsIntoTrx();
        });



    void InjectLogsIntoTrx()
    {
        var trxFiles = TestResultsDirectory.GlobFiles("*.trx");
        foreach (var trxFile in trxFiles)
        {
            var logFile = TestResultsDirectory.GlobFiles($"{trxFile.NameWithoutExtension}*.log").FirstOrDefault();
            if (logFile == null)
                continue;
            TrxLogsInjector.InjectLogs(trxFile, logFile);
        }
    }

    Target NugetPush => _ => _
        .Description("Publishes NuGet packages to Nuget.org")
        .Requires(() => NugetApiKey, () => NugetFeed)
        .OnlyWhenStatic(IsCurrentBranchCommitted)
        .DependsOn(Pack)
        .Executes(() =>
        {
            if (NugetArtifacts.IsEmpty())
            {
                Assert.Fail("No packages were found in artifacts directory.");
            }
            DotNetNuGetPush(s => s
                .SetSource(NugetFeed)
                .SetApiKey(NugetApiKey)
                .EnableSkipDuplicate()
                .SetTargetPath(ArtifactsDirectory / NugetArtifactsGlobPattern));
        });

    Target PrepareRelease => _ => _
        .Description("Adjusts version.json to next release based on Major/Minor")
        .Executes(() =>
        {
            if(!IsCurrentBranchCommitted())
                Assert.Fail("Current branch must be commited");
            if (ReleaseType is ReleaseType.Major or ReleaseType.Minor)
            {
                var jsonFilePath = RootDirectory / "version.json";
                var versionJson = jsonFilePath.ReadJson();
                var majorVersion = Version.VersionMajor + ReleaseType is ReleaseType.Major ? 1 : 0;
                var minorVersion = Version.VersionMinor + ReleaseType is ReleaseType.Major ? 1 : 0;
                var nextVersion = $"{majorVersion}.{minorVersion}{Version.PrereleaseVersion}";
                versionJson["version"] = nextVersion;
                jsonFilePath.WriteJson(versionJson);
                CommitUnstaged($"Increment version.json {ReleaseType} version to {nextVersion}");
            }
            
        });

    Target TagVersion => _ => _
        .Description("Tags current commit with version number")
        .Executes(() =>
        {
        });

    void AddTag(string tag) => GitRepository.Tags.Add(tag, GitRepository.Head.Tip);
    void CommitUnstaged(string message)
    {

        var status = GitRepository.RetrieveStatus();
        foreach (var entry in status.Where(e =>
                     e.State.HasFlag(FileStatus.ModifiedInWorkdir) ||
                     e.State.HasFlag(FileStatus.NewInWorkdir)))
        {
            Commands.Stage(GitRepository, entry.FilePath);
        }
        
        if (!GitRepository.RetrieveStatus().IsDirty)
            return;

        var author = new Signature(GitUsername, GitEmail, DateTimeOffset.Now);
        var committer = author;

        GitRepository.Commit(message, author, committer);
    }


    [Category("CI")]
    Target CIBuild => _ => _
        .Description("Builds, tests and produces test reports for regular builds on CI")
        .DependsOn(Pack, Test);

    [Category("CI")]
    Target CIRelease => _ => _
        .Description("Creates and publishes release artifacts nuget")
        .DependsOn(Pack, NugetPush);


}

public enum ReleaseType
{
    Major,
    Minor,
    Regular
}