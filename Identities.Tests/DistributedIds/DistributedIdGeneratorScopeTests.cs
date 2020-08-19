using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class DistributedIdGeneratorScopeTests
	{
		[Fact]
		public void CurrentGenerator_Regularly_ShouldReturnDefaultGenerator()
		{
			var result = DistributedIdGeneratorScope.CurrentGenerator;

			Assert.IsType<DistributedIdGenerator>(result);
		}

		[Fact]
		public void CurrentGenerator_WithAmbientGenerator_ShouldReturnThatGenerator()
		{
			var generator = new NineIdGenerator();

			IDistributedIdGenerator result;

			using (new DistributedIdGeneratorScope(generator))
				result = DistributedIdGeneratorScope.CurrentGenerator;

			Assert.Equal(generator, result);
		}

		[Fact]
		public void CurrentGenerator_WithNestedAmbientGenerators_ShouldReturnInnermostGenerator()
		{
			var outerGenerator = new NineIdGenerator();
			var innerGenerator = new EightIdGenerator();

			IDistributedIdGenerator result;

			using (new DistributedIdGeneratorScope(outerGenerator))
			using (new DistributedIdGeneratorScope(innerGenerator))
				result = DistributedIdGeneratorScope.CurrentGenerator;

			Assert.Equal(innerGenerator, result);
		}

		[Fact]
		public void CurrentGenerator_WithAmbientGeneratorAfterDisposingInnerOne_ShouldReturnExpectedGenerator()
		{
			var outerGenerator = new NineIdGenerator();
			var innerGenerator = new EightIdGenerator();

			IDistributedIdGenerator result;

			using (new DistributedIdGeneratorScope(outerGenerator))
			{
				using (new DistributedIdGeneratorScope(innerGenerator))
				{
				}
				result = DistributedIdGeneratorScope.CurrentGenerator;
			}

			Assert.Equal(outerGenerator, result);
		}

		[Fact]
		public void CurrentGenerator_AfterDisposingNestedAmbientGenerators_ShouldReturnDefaultGenerator()
		{
			using (new DistributedIdGeneratorScope(new NineIdGenerator()))
			using (new DistributedIdGeneratorScope(new EightIdGenerator()))
			{
			}

			var result = DistributedIdGeneratorScope.CurrentGenerator;

			Assert.IsType<DistributedIdGenerator>(result);
		}

		private sealed class EightIdGenerator : IDistributedIdGenerator
		{
			/// <summary>
			/// Generates value 8.
			/// </summary>
			public decimal CreateId() => 8m;
		}

		private sealed class NineIdGenerator : IDistributedIdGenerator
		{
			/// <summary>
			/// Generates value 9.
			/// </summary>
			public decimal CreateId() => 9m;
		}
	}
}
