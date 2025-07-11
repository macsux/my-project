// See https://aka.ms/new-console-template for more information

using Spectre.Console;
AnsiConsole.Write(new Markup("[bold]Welcome your brand new project[/][bold green] MyProject[/]\n"));

if (Environment.GetEnvironmentVariable("GitHubClientId1") == null || Environment.GetEnvironmentVariable("GitHubClientSecret") == null)
{
    AnsiConsole.Write(new Markup("[yellow]Can't configure GitHub repository without GitHubClientId and GitHubClientSecret environment variables[/]\n"));
    AnsiConsole.Write(new Markup("[bold]Instructions:[/]\n"));
    AnsiConsole.Write(new Markup("[bold]1.[/] Visit [link]https://github.com/settings/developers[/]\n"));
    AnsiConsole.Write(new Markup("[bold]2.[/] Create OAuth App with callback URL of [link]http://127.0.0.1:55525[/]\n"));
    AnsiConsole.Write(new Markup("[bold]3.[/] Generate client secret\n"));
    AnsiConsole.Write(new Markup("[bold]4.[/] Create environment variables for [green]GitHubClientId[/] and [green]GitHubClientSecret[/]"));
    
    return;
}
AnsiConsole.WriteLine("Let's set things up");

var fruits = AnsiConsole.Prompt(
    new MultiSelectionPrompt<string>()
        .Title("What are your [green]favorite fruits[/]?")
        .NotRequired() // Not required to have a favorite fruit
        .PageSize(10)
        .MoreChoicesText("[grey](Move up and down to reveal more fruits)[/]")
        .InstructionsText(
            "[grey](Press [blue]<space>[/] to toggle a fruit, " + 
            "[green]<enter>[/] to accept)[/]")
        .AddChoices(new[] {
            "Apple", "Apricot", "Avocado",
            "Banana", "Blackcurrant", "Blueberry",
            "Cherry", "Cloudberry", "Coconut",
        }));

var name = AnsiConsole.Prompt(
    new TextPrompt<string>("What's your name?")
        .DefaultValue("test")
    );
    
AnsiConsole.WriteLine($"So you're {name} and you're ");