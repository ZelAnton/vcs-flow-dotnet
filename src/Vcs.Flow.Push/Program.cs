using Spectre.Console;

using Vcs.Flow.Common;

// `push` — combined push flow: summarise what would be pushed, confirm, and (later)
// offer to open a pull request. A small seed; wire the real fetch/rebase/push here.

var workingDirectory = Directory.GetCurrentDirectory();
var vcs = new VcsRunner(workingDirectory);

var branch = await vcs.RunAsync("git", ["rev-parse", "--abbrev-ref", "HEAD"]);
if (!branch.IsSuccess)
{
	AnsiConsole.MarkupLineInterpolated($"[red]could not determine current branch (exit {branch.ExitCode}):[/] {branch.StdErr.Trim()}");
	return 1;
}

var branchName = branch.StdOut.Trim();
AnsiConsole.MarkupLineInterpolated($"Current branch: [green]{branchName}[/]");

if (!AnsiConsole.Confirm($"Push [green]{branchName}[/] to its upstream?"))
{
	AnsiConsole.MarkupLine("[yellow]Aborted.[/]");
	return 0;
}

AnsiConsole.MarkupLineInterpolated($"[grey]Would push {branchName} and, if requested, open a pull request.[/]");
return 0;
