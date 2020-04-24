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
			// Configure the host
			var hostBuilder = new HostBuilder();
			var startup = new Startup();
			hostBuilder.ConfigureServices(startup.ConfigureServices);
			using var host = hostBuilder.Build();
			startup.Configure(host);

			await host.StartAsync();

			// Demo some code
			CreateUsersWithDependencyInjection(host.Services.GetRequiredService<UserFactory>());
			CreateUsersWithAmbientContext();

			await host.StopAsync();

			Console.ReadKey(intercept: true);
		}

		private static void CreateUsersWithDependencyInjection(UserFactory userFactory)
		{
			var user1 = userFactory.CreateUser("JohnDoe", "John Doe");
			var user2 = userFactory.CreateUser("JaneDoe", "Jane Doe");
			Console.WriteLine();
			Console.WriteLine(user1);
			Console.WriteLine(user2);
		}

		private static void CreateUsersWithAmbientContext()
		{
			var user1 = new UserEntityWithAmbientContext("JohnDoe", "John Doe");
			var user2 = new UserEntityWithAmbientContext("JaneDoe", "Jane Doe");
			Console.WriteLine();
			Console.WriteLine(user1);
			Console.WriteLine(user2);
		}
	}
}
