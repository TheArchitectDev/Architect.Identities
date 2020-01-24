using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class FixedApplicationInstanceIdSourceExtensions
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
			options.Services.AddSingleton<IApplicationInstanceIdSource>(CreateInstance);
			options.Services.AddSingleton(CreateInstance);

			return options;
			
			// Local function that returns a new instance
			FixedApplicationInstanceIdSource CreateInstance(IServiceProvider serviceProvider)
			{
				var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
				var instance = new FixedApplicationInstanceIdSource(hostEnvironment, applicationInstanceIdFactory(serviceProvider));
				return instance;
			}
		}
	}
}
