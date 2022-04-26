namespace Architect.Identities.Example
{
	/// <summary>
	/// Demonstrates some uses of the Identities package: DistributedIds, and the Flexible, Locally-Unique ID (Fluid) generator.
	/// </summary>
	internal static class Program
	{
		private static void Main()
		{
			// Demo some code (without needing any registrations)
			Console.WriteLine("Demonstrating DistributedId as a drop-in replacement for GUID:");
			CreateDistributedIds();

			// Demo some code that uses the registrations
			Console.WriteLine("Demonstrating entities that use DistributedIds:");
			CreateUsersWithAmbientContext();

			// Demo Inversion of Control (IoC)
			using (new DistributedIdGeneratorScope(new IncrementalDistributedIdGenerator()))
			{
				Console.WriteLine("Registered an incremental ID generator for test purposes:");
				CreateUsersWithAmbientContext();

				using (new DistributedIdGeneratorScope(new CustomDistributedIdGenerator(id: 0m)))
				{
					Console.WriteLine("Registered a fixed ID generator for test purposes:");
					CreateUsersWithAmbientContext();
				}
			}

			Console.WriteLine("Once the generators have gone out of scope, the default behavior is restored:");
			CreateUsersWithAmbientContext();

			Console.ReadKey(intercept: true);
		}

		private static void CreateDistributedIds()
		{
			// Like GUIDs, these IDs can be generated from anywhere, without any registrations whatsoever
			var id1 = DistributedId.CreateId();
			var id2 = DistributedId.CreateId();
			var id1Alphanumeric = id1.ToAlphanumeric(); // IdEncoder can decode
			var id2Alphanumeric = id2.ToAlphanumeric(); // IdEncoder can decode
			Console.WriteLine($"Here is a DistributedId generated much like a GUID: {id1} (alphanumeric form: {id1Alphanumeric})");
			Console.WriteLine($"Here is a DistributedId generated much like a GUID: {id2} (alphanumeric form: {id2Alphanumeric})");
			Console.WriteLine();
		}

		private static void CreateUsersWithAmbientContext()
		{
			// These users access the IIdGenerator "out of thin air", through the ambient context pattern
			var user1 = new UserEntityWithAmbientContext("JohnDoe", "John Doe");
			var user2 = new UserEntityWithAmbientContext("JaneDoe", "Jane Doe");
			Console.WriteLine(user1);
			Console.WriteLine(user2);
			Console.WriteLine();
		}
	}
}
