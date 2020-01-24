using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class IdGeneratorExtensions
	{
		/// <summary>
		/// Registers an IIdGenerator implementation.
		/// Use the options to specify the implementation.
		/// </summary>
		public static IServiceCollection AddIdGenerator(this IServiceCollection services, Action<Options> generatorOptions)
		{
			var optionsObject = new Options(services);
			generatorOptions.Invoke(optionsObject);

			if (!services.Any(service => service.ServiceType == typeof(IIdGenerator)))
				throw new ArgumentException($"Use the options to register an {nameof(IIdGenerator)} implementation.");

			return services;
		}

		public sealed class Options
		{
			public IServiceCollection Services { get; }

			internal Options(IServiceCollection services)
			{
				this.Services = services ?? throw new ArgumentNullException(nameof(services));
			}
		}
	}
}
