using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class CustomDistributedId128GeneratorTests
	{
		[Fact]
		public void CreateId_ConstructedWithFixedUInt128_ShouldReturnExpectedResult()
		{
			var generator = new CustomDistributedId128Generator(5);

			var idOne = generator.CreateId();
			var idTwo = generator.CreateId();

			Assert.Equal((UInt128)5, idOne);
			Assert.Equal((UInt128)5, idTwo);
		}

		[Fact]
		public void CreateId_ConstructedWithUInt128Func_ShouldReturnExpectedResult()
		{
			var result = default(UInt128);
			var generator = new CustomDistributedId128Generator(() => ++result);

			var idOne = generator.CreateId();
			var idTwo = generator.CreateId();

			Assert.Equal((UInt128)1, idOne);
			Assert.Equal((UInt128)2, idTwo);
		}

		[Fact]
		public void CreateGuid_ConstructedWithFixedGuid_ShouldReturnExpectedResult()
		{
			var guid = Guid.NewGuid();
			var generator = new CustomDistributedId128Generator(guid);

			var idOne = generator.CreateGuid();
			var idTwo = generator.CreateGuid();

			Assert.Equal(guid, idOne);
			Assert.Equal(guid, idTwo);

			generator = new CustomDistributedId128Generator(guid);

			Assert.Equal(idOne, generator.CreateId().ToGuid());
			Assert.Equal(idTwo, generator.CreateId().ToGuid());
		}

		[Fact]
		public void CreateId_ConstructedWithGuidFunc_ShouldReturnExpectedResult()
		{
			var guid1 = Guid.NewGuid();
			var guid2 = Guid.NewGuid();
			var invocationCount = 0;
			var generator = new CustomDistributedId128Generator(() => invocationCount++ == 0 ? guid1 : guid2);

			var idOne = generator.CreateGuid();
			var idTwo = generator.CreateGuid();

			Assert.Equal(guid1, idOne);
			Assert.Equal(guid2, idTwo);

			invocationCount = 0;

			Assert.Equal(idOne, generator.CreateId().ToGuid());
			Assert.Equal(idTwo, generator.CreateId().ToGuid());
		}
	}
}
