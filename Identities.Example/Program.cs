using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.Identities.Example
{
	/// <summary>
	/// Demonstrates the use of the Identities package and the Flexible, Locally-Unique ID (Fluid) generator.
	/// </summary>
	internal static class Program
	{
		private static async Task Main()
		{
			// Demo some code (without having any registrations yet)
			CreateCompanyUniqueIds();

			// Configure the host
			Console.WriteLine();
			Console.WriteLine("Configuring the host and performing DI registrations.");
			var hostBuilder = new HostBuilder();
			var startup = new Startup();
			hostBuilder.ConfigureServices(startup.ConfigureServices);
			using var host = hostBuilder.Build();
			startup.Configure(host);

			await host.StartAsync();
			Console.WriteLine("The host has been started.");

			// Demo some code
			CreateUsersWithDependencyInjection(host.Services.GetRequiredService<UserFactory>());
			CreateUsersWithAmbientContext();

			await host.StopAsync();

			Console.ReadKey(intercept: true);
		}

		private static void CreateCompanyUniqueIds()
		{
			// Like GUIDs, these IDs can be generated from anywhere, without any registrations whatsoever
			var id1 = CompanyUniqueId.CreateId();
			var id2 = CompanyUniqueId.CreateId();
			var id1ShortString = CompanyUniqueId.ToShortString(id1);
			var id2ShortString = CompanyUniqueId.ToShortString(id2);
			Console.WriteLine();
			Console.WriteLine($"Here is company-unique ID generated like a GUID: {id1} (short form: {id1ShortString})");
			Console.WriteLine($"Here is company-unique ID generated like a GUID: {id2} (short form: {id2ShortString})");
		}

		private static void CreateUsersWithDependencyInjection(UserFactory userFactory)
		{
			// These users get their ID from the factory, which takes an IIdGenerator as a dependency
			var user1 = userFactory.CreateUser("JohnDoe", "John Doe");
			var user2 = userFactory.CreateUser("JaneDoe", "Jane Doe");
			Console.WriteLine();
			Console.WriteLine(user1);
			Console.WriteLine(user2);
		}

		private static void CreateUsersWithAmbientContext()
		{
			// These users access the IIdGenerator "out of thin air", through the ambient context pattern
			var user1 = new UserEntityWithAmbientContext("JohnDoe", "John Doe");
			var user2 = new UserEntityWithAmbientContext("JaneDoe", "Jane Doe");
			Console.WriteLine();
			Console.WriteLine(user1);
			Console.WriteLine(user2);
		}
	}
}
