using Architect.Identities.Helpers;
using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	/// <summary>
	/// The static subject under test merely acts as a wrapper, rather than implementing the functionality by itself.
	/// This test class merely confirms that its methods succeed, covering them. All other assertions are done in tests on the implementing types.
	/// </summary>
	public sealed class DistributedIdTests
	{
		/// <summary>
		/// Combined in a single test, since it is often hard to simulate NOT running in a unit test after simulating that we ARE running in one.
		/// </summary>
		[Fact]
		public void CreateId_WithNoCustomScopeRegardlessOfUnitTest_ShouldSucceed()
		{
			using (new TestDetector(isTestRun: false))
				_ = DistributedId.CreateId();

			using (new TestDetector(isTestRun: true))
				_ = DistributedId.CreateId();

			_ = DistributedId.CreateId();
		}

		[Fact]
		public void CreateId_WithScope_ShouldUseGeneratorFromScope()
		{
			decimal id;

			using (new DistributedIdGeneratorScope(new TenIdGenerator()))
				id = DistributedId.CreateId();

			Assert.Equal(10m, id);
		}

		private sealed class TenIdGenerator : IDistributedIdGenerator
		{
			/// <summary>
			/// Generates value 10.
			/// </summary>
			public decimal CreateId() => 10m;
		}
	}
}
