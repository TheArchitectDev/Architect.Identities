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
			if (generatorOptions is null) throw new ArgumentNullException(nameof(generatorOptions));

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
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGenerator"/> class.
		/// </summary>
		public static IApplicationBuilder UseIdGenerator(this IApplicationBuilder applicationBuilder)
		{
			return UseIdGenerator<IIdGenerator>(applicationBuilder);
		}
		
		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGenerator"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IApplicationBuilder UseIdGenerator<TIdGenerator>(this IApplicationBuilder applicationBuilder)
			where TIdGenerator : IIdGenerator
		{
			UseIdGenerator<TIdGenerator>(applicationBuilder.ApplicationServices);
			return applicationBuilder;
		}

		/// <summary>
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGenerator"/> class.
		/// </summary>
		public static IHost UseIdGenerator(this IHost host)
		{
			return UseIdGenerator<IIdGenerator>(host);
		}

		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGenerator"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IHost UseIdGenerator<TIdGenerator>(this IHost host)
			where TIdGenerator : IIdGenerator
		{
			UseIdGenerator<TIdGenerator>(host.Services);
			return host;
		}

		/// <summary>
		/// Enables static, injection-free access to the registered <see cref="IIdGenerator"/> through the <see cref="IdGenerator"/> class.
		/// </summary>
		public static IServiceProvider UseIdGenerator(this IServiceProvider serviceProvider)
		{
			return UseIdGenerator<IIdGenerator>(serviceProvider);
		}

		/// <summary>
		/// Enables static, injection-free access to the registered ID generator through the <see cref="IdGenerator"/> class.
		/// </summary>
		/// <typeparam name="TIdGenerator">The type of the ID generator to make available.</typeparam>
		public static IServiceProvider UseIdGenerator<TIdGenerator>(this IServiceProvider serviceProvider)
			where TIdGenerator : IIdGenerator
		{
			var generator = serviceProvider.GetRequiredService<TIdGenerator>();
			IdGeneratorScope.SetDefaultValue(generator);
			return serviceProvider;
		}

		#endregion
	}
}
