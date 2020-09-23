using System;
using System.Linq;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class ApplicationInstanceIdSourceExtensions
	{
		/// <summary>
		/// Registers an <see cref="IApplicationInstanceIdRenter"/> that provides an ID for this application instance.
		/// Use the options to specify an implementation.
		/// </summary>
		public static IServiceCollection AddApplicationInstanceIdSource(this IServiceCollection services, Action<Options> sourceOptions)
		{
			var optionsObject = new Options(services);

			// The source MUST be registered first, since extensions on the options may overwrite the registration
			// The source itself MUST be singleton, to establish one value for the entire application
			// Everything else should be transient, to allow any lifetime of the dependencies for renting and returning the ID
			services.AddSingleton<IApplicationInstanceIdSource, DefaultApplicationInstanceIdSource>();

			sourceOptions(optionsObject);

			if (!services.Any(service => service.ServiceType == typeof(IApplicationInstanceIdRenter)))
				throw new ArgumentException($"Use the options to specify an implementation.");

			var exceptionHandlerFactory = optionsObject.ExceptionHandlerFactory;
			services.AddTransient(CreateExceptionHandler);

			return services;

			// Local function that returns a new IExceptionHandler instance
			IExceptionHandler CreateExceptionHandler(IServiceProvider serviceProvider)
			{
				var handleExceptionAction = exceptionHandlerFactory?.Invoke(serviceProvider);
				var exceptionHandler = new OptionalExceptionHandler(handleExceptionAction);
				return exceptionHandler;
			}
		}

		/// <summary>
		/// Used by extension methods to register a specific <see cref="IApplicationInstanceIdRenter"/> implementation.
		/// </summary>
		public class Options
		{
			public IServiceCollection Services { get; }

			internal Func<IServiceProvider, Action<Exception>?>? ExceptionHandlerFactory { get; set; }

			internal Options(IServiceCollection services)
			{
				this.Services = services ?? throw new ArgumentNullException(nameof(services));
			}
		}

		/// <summary>
		/// <para>
		/// Optional.
		/// Registers a function to invoke whenever an exception occurs in the component.
		/// </para>
		/// <para>
		/// Note that after invocation of the handler, exceptions are rethrown, except during application shutdown.
		/// </para>
		/// <para>
		/// The primary purpose of the exception handler is alerting.
		/// </para>
		/// </summary>
		/// <param name="exceptionHandlerFactory">Takes the service provider and returns an action that handles an exception.</param>
		public static Options UseExceptionHandler(this Options options, Func<IServiceProvider, Action<Exception>> exceptionHandlerFactory)
		{
			options.ExceptionHandlerFactory = exceptionHandlerFactory ?? throw new ArgumentNullException(nameof(exceptionHandlerFactory));
			return options;
		}

		#region Configure

		/// <summary>
		/// Provides the registered <see cref="IApplicationInstanceIdSource"/> with an application instance ID, and registers its returning on application shutdown.
		/// </summary>
		public static IServiceProvider UseApplicationInstanceIdSource(this IHost host)
		{
			return UseApplicationInstanceIdSource(host.Services);
		}

		/// <summary>
		/// Provides the registered <see cref="IApplicationInstanceIdSource"/> with an application instance ID, and registers its returning on application shutdown.
		/// </summary>
		public static IServiceProvider UseApplicationInstanceIdSource(this IApplicationBuilder applicationBuilder)
		{
			return UseApplicationInstanceIdSource(applicationBuilder.ApplicationServices);
		}

		// #TODO: Update documentation to include this
		/// <summary>
		/// Provides the registered <see cref="IApplicationInstanceIdSource"/> with an application instance ID, and registers the returning of that ID on application shutdown.
		/// </summary>
		public static IServiceProvider UseApplicationInstanceIdSource(this IServiceProvider serviceProvider)
		{
			if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));

			var applicationInstanceIdSource = serviceProvider.GetRequiredService<IApplicationInstanceIdSource>();

			if (!(applicationInstanceIdSource is DefaultApplicationInstanceIdSource defaultSource))
				return serviceProvider;

			var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

			var applicationInstanceId = RentApplicationInstanceId();
			applicationLifetime.ApplicationStopped.Register(ReturnApplicationInstanceId);
			defaultSource.SetApplicationInstanceId(applicationInstanceId);

			return serviceProvider;

			// Local function that returns an application instance ID
			ushort RentApplicationInstanceId()
			{
				// Use a service scope to prevent scoped services from behaving as singletons
				using var scope = serviceProvider.CreateScope();

				var renter = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();
				return renter.RentId();
			}

			// Local function that returns the rented application instance ID
			void ReturnApplicationInstanceId()
			{
				// Use a service scope to prevent scoped services from behaving as singletons
				using var scope = serviceProvider.CreateScope();

				var renter = scope.ServiceProvider.GetRequiredService<IApplicationInstanceIdRenter>();
				renter.ReturnId(applicationInstanceId);
			}
		}

		#endregion
	}
}
