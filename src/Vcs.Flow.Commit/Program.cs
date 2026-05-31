using Spectre.Console;

using Vcs.Flow.Common;

// `commit` — interactive, combined commit flow:
//   1. show changed files and let the user pick which to stage,
//   2. collect a commit message,
//   3. offer to push afterwards.
// This is a deliberately small seed; flesh out each step against the real VCS.

var workingDirectory = Directory.GetCurrentDirectory();
var vcs = new VcsRunner(workingDirectory);

var status = await vcs.RunAsync("git", ["status", "--porcelain"]);
if (!status.IsSuccess)
{
	AnsiConsole.MarkupLineInterpolated($"[red]git status failed (exit {status.ExitCode}):[/] {status.StdErr.Trim()}");
	return 1;
}

var changedFiles = WorkingTree.ParsePorcelain(status.StdOut);
if (changedFiles.Count == 0)
{
	AnsiConsole.MarkupLine("[green]Nothing to commit — working tree clean.[/]");
	return 0;
}

var selection = new MultiSelectionPrompt<string>()
	.Title("Select files to [green]stage[/]:")
	.NotRequired()
	.InstructionsText("[grey](space to toggle, enter to confirm)[/]");
foreach (var file in changedFiles)
{
	selection.AddChoice($"{file.StatusCode} {file.Path}");
}

var chosen = AnsiConsole.Prompt(selection);
if (chosen.Count == 0)
{
	AnsiConsole.MarkupLine("[yellow]No files selected — nothing to do.[/]");
	return 0;
}

var message = AnsiConsole.Prompt(
	new TextPrompt<string>("Commit [green]message[/]:")
		.Validate(static text => string.IsNullOrWhiteSpace(text)
			? ValidationResult.Error("[red]Message cannot be empty[/]")
			: ValidationResult.Success()));

AnsiConsole.MarkupLineInterpolated($"[grey]Would stage {chosen.Count} file(s) and commit:[/] {message}");

if (AnsiConsole.Confirm("Push after committing?", defaultValue: false))
{
	AnsiConsole.MarkupLine("[grey]Would push to the upstream of the active bookmark/branch.[/]");
}

return 0;
