using System;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class FixedApplicationInstanceIdRenterExtensions
	{
		/// <summary>
		/// Registers a fixed-value implementation that provides the given value, as IApplicationInstanceIdSource and as FixedApplicationInstanceIdSource.
		/// </summary>
		public static ApplicationInstanceIdSourceExtensions.Options UseFixedSource(this ApplicationInstanceIdSourceExtensions.Options options, ushort applicationInstanceId)
		{
			return UseFixedSource(options, serviceProvider => applicationInstanceId);
		}
		
		/// <summary>
		/// Registers a fixed-value implementation, using the given value factory.
		/// </summary>
		public static ApplicationInstanceIdSourceExtensions.Options UseFixedSource(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<IServiceProvider, ushort> applicationInstanceIdFactory)
		{
			options.Services.AddSingleton<IApplicationInstanceIdRenter, UnusedApplicationInstanceIdRenter>(); // Register an unused renter to satisfy the base registration
			options.Services.AddSingleton(CreateInstance);

			return options;
			
			// Local function that returns a new instance
			IApplicationInstanceIdSource CreateInstance(IServiceProvider serviceProvider)
			{
				ushort applicationInstanceId;

				// Use a scope, in case scoped services are used to resolve the value
				// Then resolve the value once, and store it in our singleton instance
				using (var scope = serviceProvider.CreateScope())
					applicationInstanceId = applicationInstanceIdFactory(scope.ServiceProvider);

				var instance = new FixedApplicationInstanceIdSource(
					serviceProvider.GetRequiredService<IHostEnvironment>(),
					applicationInstanceId);
				return instance;
			}
		}
	}
}
