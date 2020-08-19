using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Architect.Identities.IdGenerators;
using Xunit;

namespace Architect.Identities.Tests.IdGenerators
{
	public sealed class IncrementalIdGeneratorTests
	{
		[Fact]
		public void Construct_Regularly_ShouldSucceed()
		{
			_ = new IncrementalIdGenerator();
		}

		[Fact]
		public void CreateId_Initially_ShouldReturnExpectedResult()
		{
			var generator = new IncrementalIdGenerator();

			var id = generator.CreateId();

			Assert.Equal(1L, id);
		}

		[Fact]
		public void CreateUnsignedId_Initially_ShouldReturnExpectedResult()
		{
			var generator = new IncrementalIdGenerator();

			var id = generator.CreateUnsignedId();

			Assert.Equal(1UL, id);
		}

		[Fact]
		public void CreateId_Repeatedly_ShouldReturnExpectedResults()
		{
			var generator = new IncrementalIdGenerator();

			var results = new List<long>();
			for (var i = 0; i < 100; i++)
				results.Add(generator.CreateId());

			for (var i = 0; i < results.Count; i++)
				Assert.Equal(1L + i, results[i]);
		}

		[Fact]
		public void CreateUnsignedId_Repeatedly_ShouldReturnExpectedResults()
		{
			var generator = new IncrementalIdGenerator();

			var results = new List<ulong>();
			for (var i = 0; i < 100; i++)
				results.Add(generator.CreateUnsignedId());

			for (var i = 0; i < results.Count; i++)
				Assert.Equal(1UL + (ulong)i, results[i]);
		}

		[Fact]
		public void CreateId_InParallel_ShouldReturnDistinctResults()
		{
			var generator = new IncrementalIdGenerator();

			var results = new ConcurrentQueue<long>();
			Parallel.For(0, 100, _ => results.Enqueue(generator.CreateId()));

			Assert.Equal(results.Count, results.Distinct().Count());
		}

		[Fact]
		public void CreateUnsignedId_InParallel_ShouldReturnDistinctResults()
		{
			var generator = new IncrementalIdGenerator();

			var results = new ConcurrentQueue<ulong>();
			Parallel.For(0, 100, _ => results.Enqueue(generator.CreateUnsignedId()));

			Assert.Equal(results.Count, results.Distinct().Count());
		}
	}
}
