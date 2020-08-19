using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Test
{
	/// <summary>
	/// This program is used to empirically test for collisions.
	/// </summary>
	internal static class Program
	{
		/// <summary>
		/// The number of simultaneously working application instances.
		/// Both are simulated to repeatedly generate the IDs at the maximum rate on the same millisecond.
		/// </summary>
		private const ushort Parallelism = 2;
		/// <summary>
		/// The rate used to be fixed, but is now dynamic. This should be greater, to provide enough array space for the generated IDs.
		/// </summary>
		private const ushort RateLimit = 256;
		/// <summary>
		/// The interval at which to log to the console.
		/// </summary>
		private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(10);

		private static List<DistributedIdGenerator> Generators { get; }
		private static List<int> GenerationCounts { get; }
		private static List<decimal[]> Arrays { get; }
		private static int IterationCount = 0;
		private static long IdCount = 0;

		private static HashSet<decimal> DistinctValues { get; } = new HashSet<decimal>(capacity: 1571);
		private static List<decimal> Collisions { get; } = new List<decimal>();

		static Program()
		{
			GenerationCounts = new List<int>(Parallelism);
			for (var i = 0; i < Parallelism; i++) GenerationCounts.Add(0);
			Generators = new List<DistributedIdGenerator>(capacity: Parallelism);
			Arrays = Enumerable.Range(0, Parallelism).Select(_ => new decimal[RateLimit]).ToList();

			for (var i = 0; i < Parallelism; i++)
			{
				var iCopy = i;
				Generators.Add(new DistributedIdGenerator(() => DateTime.UnixEpoch.AddMilliseconds(IterationCount + (GenerationCounts[iCopy] < 0 ? -1 : 0)), sleepAction: _ => GenerationCounts[iCopy] *= -1));
			}
		}

		private static void Main()
		{
			/*
			// Attempt to calculate probabilities
			{
				const int bits = 42;
				const int servers = 100;
				const int rate = 64;

				// Probability on one (rate-exhausted) timestamp for one server to have NO collisions with one other server
				var prob = 1.0;
				for (var i = 0UL; i < rate; i++)
				{
					var probForI = ((1UL << bits) - rate - i) / (double)((1UL << bits) - i);
					prob *= probForI;
				}

				// To the power of the number of distinct server pairs
				// Gives us the probability that there are no collisions on that timestamp among all of the servers
				prob = Math.Pow(prob, servers * (servers - 1) / 2);

				// Probability of one or more collisions on one (rate-exhausted) timestamp
				// We will pretend this is the probability of just one collision, although it is technically one OR MORE
				var collisionProb = 1 - prob;

				var collisionsPerId = collisionProb / (servers * Rate);

				Console.WriteLine(collisionsPerId);
				var idsPerCollision = 1 / collisionsPerId;

				Console.WriteLine($"Calculated 1 collision in {(ulong)idsPerCollision:#,##0} IDs.");
			}*/

			// Calculate average maximum generation rate
			{
				var tempResults = new List<int>();
				for (var i = 0; i < 100; i++)
				{
					var rate = 1;
					var previousValue = RandomSequence6.Create();
					while (previousValue.TryAddRandomBits(RandomSequence6.Create(), out previousValue))
						rate++;

					tempResults.Add(rate);
				}
				var lowRate = tempResults.Min();
				var highRate = tempResults.Max();
				var avgRate = tempResults.Average();
				Console.WriteLine($"Low {lowRate}, high {highRate}, avg {avgRate}");
			}

			var logInterval = TimeSpan.FromSeconds(10);

			var sw = Stopwatch.StartNew();

#pragma warning disable CS4014 // Deliberately unawaited background task
			LogAtIntervals(sw); // Unawaited task
#pragma warning restore CS4014

			while (true)
			{
				IterationCount++;
				Parallel.For(0, Parallelism, ProcessArray);
				FindDuplicates();
				ResetGenerationCounts();
			}
		}

		private static void ProcessArray(int index)
		{
			var generator = Generators[index];
			var array = Arrays[index];

			var i = 0;
			do
			{
				GenerationCounts[index]++;
				var id = generator.CreateId(); // This makes the generation count negative as soon as it can go no further
				array[i++] = id;
			} while (GenerationCounts[index] >= 0 && i < RateLimit);

			// Make positive again, and subtract the last one, which was a false ID
			if (GenerationCounts[index] < 0)
				GenerationCounts[index] = GenerationCounts[index] * -1 - 1;
		}

		private static void FindDuplicates()
		{
			Debug.Assert(DistinctValues.Count == 0);

			for (var index = 0; index < Arrays.Count; index++)
			{
				var generationCount = GenerationCounts[index];
				IdCount += generationCount;

				var array = Arrays[index];
				for (var i = 0; i < generationCount; i++)
				{
					if (!DistinctValues.Add(array[i]))
						Collisions.Add(array[i]);
				}
			}

			DistinctValues.Clear();
		}

		private static void ResetGenerationCounts()
		{
			for (var i = 0; i < GenerationCounts.Count; i++)
				GenerationCounts[i] = 0;
		}

		private static async Task LogAtIntervals(Stopwatch sw)
		{
			while (true)
			{
				await Task.Delay(LogInterval);

				var collisionCount = (ulong)GetCollisionCount();
				var ids = (ulong)IdCount;
				Console.WriteLine($"{(int)sw.Elapsed.TotalMinutes:000}: {IterationCount} iterations, {ids:#,##0} IDs, {collisionCount} collisions: 1 in {ids/Math.Max(1, collisionCount):#,##0}");
			}
		}

		private static int GetCollisionCount()
		{
			return Collisions.Count;
		}
	}
}
