using Vcs.Flow.Common;

namespace Vcs.Flow.Common.Tests;

[TestFixture]
public class WorkingTreeTests
{
	[Test]
	public void ParsePorcelain_ReadsStatusCodeAndPath()
	{
		var output = " M src/Program.cs\n?? README.md\nA  new.txt\n";

		var changed = WorkingTree.ParsePorcelain(output);

		Assert.That(changed, Has.Count.EqualTo(3));
		Assert.That(changed[0], Is.EqualTo(new ChangedFile(" M", "src/Program.cs")));
		Assert.That(changed[1], Is.EqualTo(new ChangedFile("??", "README.md")));
		Assert.That(changed[2], Is.EqualTo(new ChangedFile("A ", "new.txt")));
	}

	[Test]
	public void ParsePorcelain_SkipsBlankLines()
	{
		Assert.That(WorkingTree.ParsePorcelain("\n\n"), Is.Empty);
	}
}
