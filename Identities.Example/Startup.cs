using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Architect.Identities.Example
{
	internal sealed class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			// Obtain access to a unique ID for our application instance
			ushort applicationInstanceIdConfigValue = 5;
			services.AddApplicationInstanceIdSource(source => source.UseFixedSource(applicationInstanceIdConfigValue));

			// Or rather use one of the self-orchestrating implementations, using SQL server, Azure blob storage, or Standard SQL
			// These handle the distribution among applications for us
			//services.AddApplicationInstanceIdSource(source => source.UseSqlServer<IServiceProvider>(serviceProvider => new SqlConnection("ConnectionString")));
			//services.AddApplicationInstanceIdSource(source => source.UseAzureBlobStorageContainer(serviceProvider => new BlobContainerClient("ConnectionString", "ContainerName")));

			// Register the Flexible, Locally-Unique ID (Fluid) as the ID generation mechanism
			services.AddIdGenerator(generator => generator.UseFluid());

			// Optional:
			// Allow user entities to be created through a factory
			services.AddSingleton<UserFactory>();
		}

		public void Configure(IHost host) // Or IApplicationBuilder, for web hosts
		{
			// Optional:
			// Allow injection-free access to the registered IIdGenerator, through the Ambient Context IoC pattern
			host.UseIdGenerator();
		}
	}
}
