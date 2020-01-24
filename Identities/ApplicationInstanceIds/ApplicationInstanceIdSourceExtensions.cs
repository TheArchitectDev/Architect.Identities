using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class ApplicationInstanceIdSourceExtensions
	{
		/// <summary>
		/// Registers a source that provides an id for this application instance.
		/// Use the options to specify an implementation.
		/// </summary>
		public static IServiceCollection AddApplicationInstanceIdSource(this IServiceCollection serviceCollection, Action<Options> sourceOptions)
		{
			var optionsObject = new Options(serviceCollection);
			sourceOptions(optionsObject);
			if (!serviceCollection.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IApplicationInstanceIdSource)))
			{
				throw new Exception($"Use the options to register an {nameof(IApplicationInstanceIdSource)} implementation.");
			}
			return serviceCollection;
		}

		/// <summary>
		/// Used by extension methods to register a specific IApplicationInstanceIdSource implementation.
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
		/// Registers the given implementation type.
		/// </summary>
		public static Options UseImplementation<TApplicationInstanceIdSource>(this Options options)
			where TApplicationInstanceIdSource : IApplicationInstanceIdSource
		{
			options.Services.AddSingleton(typeof(IApplicationInstanceIdSource), typeof(TApplicationInstanceIdSource));
			return options;
		}

		/// <summary>
		/// Registers the given implementation instance.
		/// </summary>
		public static Options UseImplementation(this Options options, IApplicationInstanceIdSource instance)
		{
			options.Services.AddSingleton(typeof(IApplicationInstanceIdSource), instance ?? throw new ArgumentNullException(nameof(instance)));
			return options;
		}
		
		/// <summary>
		/// Registers the given implementation factory.
		/// </summary>
		public static Options UseImplementation(this Options options, Func<IServiceProvider, IApplicationInstanceIdSource> factory)
		{
			options.Services.AddSingleton(typeof(IApplicationInstanceIdSource), implementationFactory: factory ?? throw new ArgumentNullException(nameof(factory)));
			return options;
		}

		/// <summary>
		/// <para>
		/// Optional.
		/// Registers a function to invoke whenever an exception occurs in the component.
		/// </para>
		/// <para>
		/// After the invocation, exceptions are rethrown, except on teardown.
		/// </para>
		/// <para>
		/// The primary intent of the exception handler is alerting.
		/// Generally, this component only performs critical work during startup.
		/// The exception handler can be used to provide custom alerting in case of failure.
		/// </para>
		/// <para>
		/// The simplest approach is to register a method defined in the Startup class.
		/// </para>
		/// </summary>
		/// <param name="exceptionHandlerFactory">Takes the service provider and returns an action that handles an exception.</param>
		public static Options UseExceptionHandler(this Options options, Func<IServiceProvider, Action<Exception>> exceptionHandlerFactory)
		{
			options.ExceptionHandlerFactory = exceptionHandlerFactory ?? throw new ArgumentNullException(nameof(exceptionHandlerFactory));
			return options;
		}
	}
}
