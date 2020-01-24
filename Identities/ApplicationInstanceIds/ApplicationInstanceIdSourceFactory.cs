using System;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// IApplicationInstanceIdSource instances should be created through this factory.
	/// It ensures that the ApplicationInstanceId value is resolved at the right time.
	/// </summary>
	internal static class ApplicationInstanceIdSourceFactory
	{
		/// <summary>
		/// <para>
		/// Resolves an instance using the given factory method.
		/// </para>
		/// <para>
		/// If the application has not started yet, this method resolves the ApplicationInstanceId value before returning the instance.
		/// This helps ensure that exceptions prevent application startup.
		/// </para>
		/// </summary>
		public static IApplicationInstanceIdSource Create(IHostApplicationLifetime applicationLifetime, Func<IApplicationInstanceIdSource> factory)
		{
			var instance = factory();

			// The value is likely to be application-critical
			// As such, if the application has not started yet, resolve the value
			// This way, potential exceptions are thrown during startup rather than later
			var applicationHasStarted = applicationLifetime.ApplicationStarted.IsCancellationRequested;
			if (!applicationHasStarted)
			{
				// Resolve the lazy
				_ = instance.ContextUniqueApplicationInstanceId.Value;
			}

			return instance;
		}
	}
}
