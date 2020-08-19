using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class CustomDistributedIdGeneratorTests
	{
		[Fact]
		public void CreateId_ConstructedWithFixedDecimal_ShouldReturnExpectedResult()
		{
			var generator = new CustomDistributedIdGenerator(5m);

			var idOne = generator.CreateId();
			var idTwo = generator.CreateId();

			Assert.Equal(5m, idOne);
			Assert.Equal(5m, idTwo);
		}

		[Fact]
		public void CreateId_ConstructedWithFunc_ShouldReturnExpectedResult()
		{
			var result = 0m;
			var generator = new CustomDistributedIdGenerator(() => ++result);

			var idOne = generator.CreateId();
			var idTwo = generator.CreateId();

			Assert.Equal(1m, idOne);
			Assert.Equal(2m, idTwo);
		}
	}
}
