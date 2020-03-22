using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class IdGeneratorExtensions
	{
		#region Registration

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

		#endregion

		#region Options

		public sealed class Options
		{
			public IServiceCollection Services { get; }

			internal Options(IServiceCollection services)
			{
				this.Services = services ?? throw new ArgumentNullException(nameof(services));
			}
		}

		#endregion

		#region Configuration

		/// <summary>
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		public static IApplicationBuilder UseIdGeneratorScope(this IApplicationBuilder applicationBuilder)
		{
			return UseIdGeneratorScope<IIdGenerator>(applicationBuilder);
		}
		
		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IApplicationBuilder UseIdGeneratorScope<TIdGenerator>(this IApplicationBuilder applicationBuilder)
			where TIdGenerator : IIdGenerator
		{
			UseIdGeneratorScope<TIdGenerator>(applicationBuilder.ApplicationServices);
			return applicationBuilder;
		}

		/// <summary>
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		public static IHost UseIdGeneratorScope(this IHost host)
		{
			return UseIdGeneratorScope<IIdGenerator>(host);
		}

		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IHost UseIdGeneratorScope<TIdGenerator>(this IHost host)
			where TIdGenerator : IIdGenerator
		{
			UseIdGeneratorScope<TIdGenerator>(host.Services);
			return host;
		}

		/// <summary>
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		public static IServiceProvider UseIdGeneratorScope(this IServiceProvider serviceProvider)
		{
			return UseIdGeneratorScope<IIdGenerator>(serviceProvider);
		}

		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGeneratorScope"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IServiceProvider UseIdGeneratorScope<TIdGenerator>(this IServiceProvider serviceProvider)
			where TIdGenerator : IIdGenerator
		{
			var generator = serviceProvider.GetRequiredService<TIdGenerator>();
			IdGeneratorScope.SetDefaultValue(generator);
			return serviceProvider;
		}

		#endregion
	}
}
