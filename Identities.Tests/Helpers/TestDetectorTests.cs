using Architect.Identities.Helpers;
using Xunit;

namespace Architect.Identities.Tests.Helpers
{
	public sealed class TestDetectorTests
	{
		[Fact]
		public void IsTestRun_InTestRun_ShouldReturnExpectedResult()
		{
			var isTestRun = TestDetector.IsTestRun;
			Assert.True(isTestRun);
		}
	}
}
