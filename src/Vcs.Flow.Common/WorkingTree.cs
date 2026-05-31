namespace Vcs.Flow.Common;

/// <summary>One changed path in the working tree, as reported by <c>git status --porcelain</c>.</summary>
public readonly record struct ChangedFile(string StatusCode, string Path);

/// <summary>
/// Pure helpers for interpreting VCS porcelain output. Kept free of process I/O so the
/// parsing logic is unit-testable without spawning <c>git</c> — the command projects feed
/// it the stdout captured through <see cref="VcsRunner"/>.
/// </summary>
public static class WorkingTree
{
	/// <summary>
	/// Parses the machine-readable output of <c>git status --porcelain</c> into one
	/// <see cref="ChangedFile"/> per line. The two-character status code is preserved
	/// verbatim (e.g. <c>" M"</c>, <c>"??"</c>, <c>"A "</c>); blank lines are skipped.
	/// </summary>
	public static IReadOnlyList<ChangedFile> ParsePorcelain(string porcelainOutput)
	{
		ArgumentNullException.ThrowIfNull(porcelainOutput);

		var result = new List<ChangedFile>();
		foreach (var rawLine in porcelainOutput.Split('\n'))
		{
			var line = rawLine.TrimEnd('\r');
			if (line.Length < 4)
			{
				continue;
			}

			var statusCode = line[..2];
			var path = line[3..];
			result.Add(new ChangedFile(statusCode, path));
		}

		return result;
	}
}
