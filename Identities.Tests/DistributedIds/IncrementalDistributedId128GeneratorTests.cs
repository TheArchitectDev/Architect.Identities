using System.Collections.Concurrent;
using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class IncrementalDistributedId128GeneratorTests
	{
		private IncrementalDistributedId128Generator Generator { get; } = new IncrementalDistributedId128Generator();

		[Fact]
		public void CreateId_Initially_ShouldReturnExpectedResult()
		{
			var id = this.Generator.CreateId();

			Assert.Equal((UInt128)1, id);
		}

		[Fact]
		public void CreateGuid_Initially_ShouldReturnExpectedResult()
		{
			var id = this.Generator.CreateGuid();

			Assert.Equal(((UInt128)1).ToGuid(), id);
		}

		[Fact]
		public void CreateId_Repeatedly_ShouldReturnExpectedResult()
		{
			var idOne = this.Generator.CreateId();
			var idTwo = this.Generator.CreateId();
			var idThree = this.Generator.CreateId();

			Assert.Equal((UInt128)1, idOne);
			Assert.Equal((UInt128)2, idTwo);
			Assert.Equal((UInt128)3, idThree);
		}

		/// <summary>
		/// The results should be the expected ones, confirming that the type is thread-safe, although the order can be anything.
		/// </summary>
		[Fact]
		public void CreateId_InParallel_ShouldReturnExpectedResult()
		{
			var expectedResults = Enumerable.Range(1, 10).Select(i => (UInt128)i);
			var results = new ConcurrentQueue<UInt128>();

			Parallel.For(0, 10, _ => results.Enqueue(this.Generator.CreateId()));

			Assert.Equal(expectedResults, results.OrderBy(id => id));
		}

		/// <summary>
		/// The generators should be independent and each keep their own count.
		/// </summary>
		[Fact]
		public void CreateId_InParallelWithDifferentGenerators_ShouldReturnExpectedResult()
		{
			var results = new ConcurrentQueue<UInt128>();

			Parallel.For(0, 10, _ => results.Enqueue(new IncrementalDistributedId128Generator().CreateId()));

			Assert.All(results, result => Assert.Equal((UInt128)1, result));
		}

		[Fact]
		public void CreateId_RepeatedlyWithCustomUInt128StartingValue_ShouldReturnExpectedResult()
		{
			var generator = new IncrementalDistributedId128Generator(firstId: 0UL);

			var result1 = generator.CreateId();
			var result2 = generator.CreateId();

			Assert.Equal((UInt128)0, result1);
			Assert.Equal((UInt128)1, result2);

			generator = new IncrementalDistributedId128Generator(firstId: 0UL);

			var resultA = generator.CreateGuid();
			var resultB = generator.CreateGuid();

			Assert.Equal(result1, resultA.ToUInt128());
			Assert.Equal(result2, resultB.ToUInt128());

			generator = new IncrementalDistributedId128Generator(firstId: UInt64.MaxValue);

			result1 = generator.CreateId();
			result2 = generator.CreateId();

			Assert.Equal((UInt128)UInt64.MaxValue, result1);
			Assert.Equal((UInt128)UInt64.MaxValue + 1, result2);

			generator = new IncrementalDistributedId128Generator(firstId: UInt64.MaxValue);

			resultA = generator.CreateGuid();
			resultB = generator.CreateGuid();

			Assert.Equal(result1, resultA.ToUInt128());
			Assert.Equal(result2, resultB.ToUInt128());
		}

		[Fact]
		public void CreateId_RepeatedlyWithCustomGuidStartingValue_ShouldReturnExpectedResult()
		{
			var guidZero = ((UInt128)0UL).ToGuid();
			var guidUlongMax = ((UInt128)UInt64.MaxValue).ToGuid();

			var generator = new IncrementalDistributedId128Generator(firstId: guidZero);

			var result1 = generator.CreateId();
			var result2 = generator.CreateId();

			Assert.Equal((UInt128)0, result1);
			Assert.Equal((UInt128)1, result2);

			generator = new IncrementalDistributedId128Generator(firstId: guidZero);

			var resultA = generator.CreateGuid();
			var resultB = generator.CreateGuid();

			Assert.Equal(result1, resultA.ToUInt128());
			Assert.Equal(result2, resultB.ToUInt128());
			Assert.Equal(guidZero, resultA);

			generator = new IncrementalDistributedId128Generator(firstId: guidUlongMax);

			result1 = generator.CreateId();
			result2 = generator.CreateId();

			Assert.Equal((UInt128)UInt64.MaxValue, result1);
			Assert.Equal((UInt128)UInt64.MaxValue + 1, result2);

			generator = new IncrementalDistributedId128Generator(firstId: guidUlongMax);

			resultA = generator.CreateGuid();
			resultB = generator.CreateGuid();

			Assert.Equal(result1, resultA.ToUInt128());
			Assert.Equal(result2, resultB.ToUInt128());
			Assert.Equal(guidUlongMax, resultA);
		}
	}
}
