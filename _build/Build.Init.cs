using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ConsoleClientWithBrowser;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.Jwk;
using Duende.IdentityModel.OidcClient;
using LibGit2Sharp;
using Nuke.Common;
using Nuke.Common.Tools.Git;
using Octokit;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Credentials = LibGit2Sharp.Credentials;
using Repository = LibGit2Sharp.Repository;
using Duende.IdentityModel.OidcClient.Infrastructure;
using Humanizer;
using Newtonsoft.Json;
using Nuke.Common.IO;
using ReflectionMagic;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public partial class Build
{
    [Parameter]readonly string GitHubClientId;
    [Parameter]readonly string GitHubClientSecret;
    
    protected override void OnBuildCreated()
    {
        if (!IsGitInitialized)
        {
            InitializeGit();
            DotNetToolRestore(_ => _);
        }
    }
    
    
    void InitializeGit()
    {
        GitTasks.Git("init --initial-branch=main", workingDirectory: RootDirectory);
        GitRepository = new Repository(RootDirectory);
        var signature = GitRepository.Config.BuildSignature(DateTimeOffset.UtcNow);
        // Commands.Stage(GitRepository, ".gitignore");
        Commands.Stage(GitRepository, "version.json");
        // commit all build scripts with execute permission to make sure they are runnable on linux based systems
        foreach (var extension in new[] {"sh", "cmd", "ps1"})
        {
            Commands.Stage(GitRepository, $"build.{extension}");
            GitTasks.Git($"update-index --chmod=+x build.{extension}", workingDirectory: RootDirectory);
        }
    
        GitRepository.Commit("Git versioning file", signature, signature);
        GitRepository.CreateBranch("release");
    }

    Target CreateGithubRepo => _ => _
        .Executes(async () =>
        {
            var accessToken = await RequestGitHubAccessToken();
            var client = new GitHubClient(new ProductHeaderValue("MyProject"));
            client.Credentials = new Octokit.Credentials(accessToken.Token);
            var repoName = ProjectName.Kebaberize();
            
            var newRepo = new NewRepository(repoName);
            string cloneUrl = "";
            try
            {
                var repo = await client.Repository.Create(newRepo);
                if (repo.CloneUrl == null)
                {
                    throw new InvalidOperationException("Failed to create a GitHub repository at ");
                }
                Log.Information("Created new repository at {RepoUrl}", cloneUrl);

                cloneUrl = repo.CloneUrl;
                
            }
            catch (RepositoryExistsException)
            {
                Log.Warning("Repository {RepoUrl} already exists", cloneUrl);
                var user = await client.User.Current();
                cloneUrl = $"https://github.com/{user.Login}/{repoName}.git";
            }
            GitTasks.Git($"remote add origin {cloneUrl}");
            Log.Information("Added {RepoUrl} as remote origin", cloneUrl);
            
            //
            // // 1. Create repo
            // var newRepo = new NewRepository(repoName)
            // {
            //     Private = true
            // };
            // var repo = await client.Repository.Create(newRepo);
        });


    
    async Task<AccessToken> RequestGitHubAccessToken()
    {
        // create a redirect URI using an available port on the loopback address.
        // requires the OP to allow random ports on 127.0.0.1 - otherwise set a static port
        var browser = new SystemBrowser(55525);
        string redirectUri = string.Format($"http://127.0.0.1:{browser.Port}");
        var client = new HttpClient();
        var disco = await client.GetDiscoveryDocumentAsync(
            new DiscoveryDocumentRequest
            {
                Address = "https://github.com/login/oauth/.well-known/openid-configuration",
                Policy = new DiscoveryPolicy
                {
                    ValidateIssuerName = false,
                    ValidateEndpoints = false,
                },
            });
        
        
        var options = new OidcClientOptions
        {
            Authority = "https://github.com/login/oauth",
            ClientId = GitHubClientId,
            ClientSecret = GitHubClientSecret,
            RedirectUri = redirectUri,
            Scope = "openid repo",
            FilterClaims = false,
            Browser = browser,
            LoadProfile = false,
            Policy = new Policy(){ Discovery =
            {
                ValidateIssuerName = false,
                ValidateEndpoints = false,
            }},
            ProviderInformation = new ProviderInformation()
            {
                IssuerName = "https://github.com",
                AuthorizeEndpoint = "https://github.com/login/oauth/authorize",
                TokenEndpoint = "https://github.com/login/oauth/access_token",
                KeySet = disco.KeySet
            }
        };
        
        
        options.LoggerFactory.AddSerilog();
        var oidcClient = new OidcClient(options);
        
        var result = await oidcClient.LoginAsync(new LoginRequest());
        var token = new AccessToken(result.AccessToken, result.AccessTokenExpiration);
        
        return token;
    }
    
    
}