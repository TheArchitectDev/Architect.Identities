using System;
using System.Reflection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Implementors are advised to use this base implementation.
	/// </para>
	/// <para>
	/// This implementation registers an ID on startup.
	/// On application shutdown, it attempts to remove it again, freeing it up.
	/// </para>
	/// <para>
	/// Enough possible IDs should be available that an occassional failure to free up an ID is not prohibitive.
	/// </para>
	/// </summary>
	public abstract class BaseApplicationInstanceIdSource : IApplicationInstanceIdSource
	{
		public Lazy<ushort> ContextUniqueApplicationInstanceId { get; }

		protected Action<Exception>? ExceptionHandler { get; }

		/// <summary>
		/// This constructor may call virtual methods.
		/// </summary>
		protected BaseApplicationInstanceIdSource(IHostApplicationLifetime applicationLifetime, Action<Exception>? exceptionHandler = null)
		{
			if (applicationLifetime is null) throw new ArgumentNullException(nameof(applicationLifetime));
			this.ExceptionHandler = exceptionHandler;

			// Register relinquishing of the ID on shutdown
			applicationLifetime.ApplicationStopped.Register(this.TryDeleteContextUniqueApplicationInstanceId);

			this.ContextUniqueApplicationInstanceId = new Lazy<ushort>(this.TryGetContextUniqueApplicationInstanceId);
		}

		/// <summary>
		/// Calls the non-try overload, catching and logging any exceptions before rethrowing them.
		/// </summary>
		protected virtual ushort TryGetContextUniqueApplicationInstanceId()
		{
			try
			{
				var contextUniqueApplicationInstanceId = this.GetContextUniqueApplicationInstanceId();
				return contextUniqueApplicationInstanceId;
			}
			catch (Exception e)
			{
				var wrappingException = new Exception($"{this.GetType().Name} failed to register an ApplicationInstanceId.", e);
				Console.WriteLine(wrappingException.Message);
				this.ExceptionHandler?.Invoke(wrappingException);
				throw wrappingException;
			}
		}

		/// <summary>
		/// Writes to the console and calls the core overload.
		/// Throws if the result was 0.
		/// </summary>
		protected virtual ushort GetContextUniqueApplicationInstanceId()
		{
			Console.WriteLine($"Registering ApplicationInstanceId using {this.GetType().Name}...");

			var applicationInstanceId = this.GetContextUniqueApplicationInstanceIdCore();

			if (applicationInstanceId == 0) throw new Exception($"{this.GetType().Name} provided an invalid ApplicationInstanceId of 0.");

			Console.WriteLine($"Registered ApplicationInstanceId {applicationInstanceId} using {this.GetType().Name}.");

			return applicationInstanceId;
		}

		/// <summary>
		/// Provides the specific implementation.
		/// </summary>
		protected abstract ushort GetContextUniqueApplicationInstanceIdCore();

		/// <summary>
		/// <para>
		/// Calls the non-try overload, catching and logging any exceptions, without rethrowing them.
		/// </para>
		/// <para>
		/// If no value was ever registered, this method returns without doing anything.
		/// </para>
		/// </summary>
		private void TryDeleteContextUniqueApplicationInstanceId()
		{
			// If no value was registered, there is nothing to delete
			if (this.ContextUniqueApplicationInstanceId?.IsValueCreated != true) return;

			try
			{
				this.DeleteContextUniqueApplicationInstanceId();
			}
			catch (Exception e)
			{
				var wrappingException = new Exception(
					$"{this.GetType().Name}.{nameof(this.DeleteContextUniqueApplicationInstanceId)} failed to unregister the ApplicationInstanceId.", e);
				Console.WriteLine(wrappingException.Message);
				this.ExceptionHandler?.Invoke(wrappingException);
				// Called on shutdown, so swallow the exception
			}
		}

		/// <summary>
		/// Writes to the console and calls the core overload.
		/// Throws if no value was ever registered.
		/// </summary>
		protected virtual void DeleteContextUniqueApplicationInstanceId()
		{
			if (this.ContextUniqueApplicationInstanceId?.IsValueCreated != true)
				throw new InvalidOperationException("Trying to unregister ApplicationInstanceId when none was ever registered.");

			Console.WriteLine($"Unregistering ApplicationInstanceId {this.ContextUniqueApplicationInstanceId.Value} using {this.GetType().Name}...");

			this.DeleteContextUniqueApplicationInstanceIdCore();

			Console.WriteLine($"Unregistered ApplicationInstanceId {this.ContextUniqueApplicationInstanceId.Value} using {this.GetType().Name}.");
		}

		/// <summary>
		/// Provides the specific implementation.
		/// </summary>
		protected abstract void DeleteContextUniqueApplicationInstanceIdCore();

		/// <summary>
		/// Returns the entry assembly's name, or a fixed value representing a test host if no entry assembly is available.
		/// </summary>
		protected virtual string GetApplicationName()
		{
			return Assembly.GetEntryAssembly()?.GetName().Name ?? "testhost"; // Stay predictable in unit tests (not a nice name, but it matches the value in IHostEnvironment)
		}

		/// <summary>
		/// Returns the machine name.
		/// </summary>
		protected virtual string GetServerName()
		{
			return Environment.MachineName;
		}
	}
}
