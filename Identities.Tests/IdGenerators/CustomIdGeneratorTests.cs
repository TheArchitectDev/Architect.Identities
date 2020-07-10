using System;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators
{
	public sealed class CustomIdGeneratorTests
	{
		[Fact]
		public void Construct_WithId_ShouldSucceed()
		{
			_ = new CustomIdGenerator(UInt64.MaxValue);
		}

		[Fact]
		public void Construct_WithFunction_ShouldSucceed()
		{
			_ = new CustomIdGenerator(() => UInt64.MaxValue);
		}

		[Fact]
		public void CreateId_RepeatedlyWithFixedId_ShouldGenerateExpectedIds()
		{
			var generator = new CustomIdGenerator(1UL);

			var first = generator.CreateId();
			var second = generator.CreateId();
			var third = generator.CreateId();

			Assert.Equal(1L, first);
			Assert.Equal(1L, second);
			Assert.Equal(1L, third);
		}

		[Fact]
		public void CreateId_RepeatedlyWithGenerator_ShouldGenerateExpectedIds()
		{
			var i = 0UL;
			var generator = new CustomIdGenerator(() => ++i);

			var first = generator.CreateId();
			var second = generator.CreateId();
			var third = generator.CreateId();

			Assert.Equal(1L, first);
			Assert.Equal(2L, second);
			Assert.Equal(3L, third);
		}

		[Fact]
		public void CreateUInsignedId_RepeatedlyWithFixedId_ShouldMatchCreateId()
		{
			var generator = new CustomIdGenerator(1UL);

			var ul1 = generator.CreateUnsignedId();
			var ul2 = generator.CreateUnsignedId();
			var l1 = generator.CreateId();
			var l2 = generator.CreateId();

			Assert.Equal(l1, (long)ul1);
			Assert.Equal(l2, (long)ul2);
		}

		[Fact]
		public void CreateUInsignedId_RepeatedlyWithGenerator_ShouldMatchCreateId()
		{
			var i = 0UL;
			var j = 0UL;
			var generatorUl = new CustomIdGenerator(() => ++i);
			var generatorL = new CustomIdGenerator(() => ++j);

			var ul1 = generatorUl.CreateUnsignedId();
			var ul2 = generatorUl.CreateUnsignedId();
			var l1 = generatorL.CreateId();
			var l2 = generatorL.CreateId();

			Assert.Equal(l1, (long)ul1);
			Assert.Equal(l2, (long)ul2);
		}
	}
}
