using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	/// <summary>
	/// The static subject under test merely acts as a wrapper, rather than implementing the functionality by itself.
	/// This test class merely confirms that its methods succeed, covering them. All other assertions are done in tests on the implementing types.
	/// </summary>
	public sealed class DistributedIdTests
	{
		[Fact]
		public void CreateId_WithNoCustomScope_ShouldSucceed()
		{
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
