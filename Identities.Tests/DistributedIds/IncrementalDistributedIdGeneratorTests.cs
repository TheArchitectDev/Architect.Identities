using System.Collections.Concurrent;
using Xunit;

namespace Architect.Identities.Tests.DistributedIds
{
	public sealed class IncrementalDistributedIdGeneratorTests
	{
		private IncrementalDistributedIdGenerator Generator { get; } = new IncrementalDistributedIdGenerator();

		[Fact]
		public void CreateId_Initially_ShouldReturnExpectedResult()
		{
			var id = this.Generator.CreateId();

			Assert.Equal(1m, id);
		}

		[Fact]
		public void CreateId_Repeatedly_ShouldReturnExpectedResult()
		{
			var idOne = this.Generator.CreateId();
			var idTwo = this.Generator.CreateId();
			var idThree = this.Generator.CreateId();

			Assert.Equal(1m, idOne);
			Assert.Equal(2m, idTwo);
			Assert.Equal(3m, idThree);
		}

		/// <summary>
		/// The results should be the expected ones, confirming that the type is thread-safe, although the order can be anything.
		/// </summary>
		[Fact]
		public void CreateId_InParallel_ShouldReturnExpectedResult()
		{
			var expectedResults = Enumerable.Range(1, 10).Select(i => (decimal)i);
			var results = new ConcurrentQueue<decimal>();

			Parallel.For(0, 10, _ => results.Enqueue(this.Generator.CreateId()));

			Assert.Equal(expectedResults, results.OrderBy(id => id));
		}

		/// <summary>
		/// The generators should be independent and each keep their own count.
		/// </summary>
		[Fact]
		public void CreateId_InParallelWithDifferentGenerators_ShouldReturnExpectedResult()
		{
			var results = new ConcurrentQueue<decimal>();

			Parallel.For(0, 10, _ => results.Enqueue(new IncrementalDistributedIdGenerator().CreateId()));

			foreach (var result in results)
				Assert.Equal(1m, result);
		}
	}
}
