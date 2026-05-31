using ProcessKit;

namespace Vcs.Flow.Common;

/// <summary>
/// Thin VCS invocation layer shared by every command. Shells out to a tool
/// (<c>git</c>, <c>jj</c>, <c>gh</c>) through ProcessKit's <see cref="IProcessRunner"/>
/// and captures its output, always within a fixed working directory.
/// </summary>
/// <remarks>
/// Per the umbrella conventions, process execution never goes through
/// <c>System.Diagnostics.Process.Start</c> directly, and human-readable CLI output is
/// never parsed — pass <c>--json</c> / <c>jj</c> templates and deserialize the result.
/// </remarks>
public sealed class VcsRunner
{
	readonly IProcessRunner _runner;
	readonly string _workingDirectory;

	public VcsRunner(string workingDirectory, IProcessRunner? runner = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

		_workingDirectory = workingDirectory;
		// ProcessRunner.Default is the library's shared, stateless, thread-safe instance —
		// preferred over allocating one per VcsRunner when no runner is injected.
		_runner = runner ?? ProcessRunner.Default;
	}

	/// <summary>
	/// Runs <paramref name="executable"/> with <paramref name="arguments"/> in the
	/// runner's working directory and returns the captured stdout, stderr and exit code.
	/// </summary>
	public Task<ProcessResult<string>> RunAsync(
		string executable,
		IReadOnlyList<string> arguments,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrEmpty(executable);
		ArgumentNullException.ThrowIfNull(arguments);

		var options = new ProcessRunOptions { WorkingDirectory = _workingDirectory };
		return _runner.GetFullOutputAsync(executable, arguments, options, cancellationToken);
	}
}
