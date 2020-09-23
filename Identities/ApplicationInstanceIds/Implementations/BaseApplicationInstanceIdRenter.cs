using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// <para>
	/// Third-party library authors are strongly advised to use this base implementation.
	/// </para>
	/// </summary>
	public abstract class BaseApplicationInstanceIdRenter : IApplicationInstanceIdRenter
	{
		private IExceptionHandler ExceptionHandler { get; }

		/// <summary>
		/// This constructor may call virtual methods.
		/// </summary>
		protected BaseApplicationInstanceIdRenter(IServiceProvider serviceProvider)
		{
			// We use the service locator anti-pattern here, to hide the internal dependencies (registered by our own extension methods), enabling subclasses from third-party implementations
			this.ExceptionHandler = serviceProvider.GetRequiredService<IExceptionHandler>();
		}

		protected virtual void TryHandleException(Exception exception)
		{
			try
			{
				this.ExceptionHandler.HandleException(exception);
			}
			catch
			{
				// We tried our best
				// The outer code determines whether to rethrow the original exception or not, so we must not interfere by throwing our own (less important) exception
			}
		}

		/// <summary>
		/// Rents a context-unique application instance ID.
		/// </summary>
		public ushort RentId()
		{
			var applicationInstanceId = this.TryGetContextUniqueApplicationInstanceId();
			return applicationInstanceId;
		}

		/// <summary>
		/// <para>
		/// Returns the rented application instance ID, making it available to others again.
		/// </para>
		/// <para>
		/// This should only be invoked with an ID that was currently rented and by an instance of the same type.
		/// </para>
		/// </summary>
		public void ReturnId(ushort id)
		{
			this.TryReleaseContextUniqueApplicationInstanceId(id);
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
				var wrappingException = new Exception($"{this.GetType().Name} failed to rent an ApplicationInstanceId.", e);
				Console.WriteLine(wrappingException.Message);
				this.TryHandleException(wrappingException);
				throw wrappingException;
			}
		}

		/// <summary>
		/// Writes to the console and calls the core overload.
		/// Throws if the result was 0.
		/// </summary>
		protected virtual ushort GetContextUniqueApplicationInstanceId()
		{
			Console.WriteLine($"Renting ApplicationInstanceId using {this.GetType().Name}...");

			var applicationInstanceId = this.GetContextUniqueApplicationInstanceIdCore();

			if (applicationInstanceId == 0) throw new Exception($"{this.GetType().Name} provided an invalid ApplicationInstanceId of 0.");

			Console.WriteLine($"Rented ApplicationInstanceId {applicationInstanceId} using {this.GetType().Name}.");

			return applicationInstanceId;
		}

		/// <summary>
		/// Provides the specific implementation to acquire a context-unique application instance ID.
		/// </summary>
		protected abstract ushort GetContextUniqueApplicationInstanceIdCore();

		/// <summary>
		/// <para>
		/// Calls the non-try overload, catching and logging any exceptions, without rethrowing them.
		/// </para>
		/// <para>
		/// If no value was ever rented, this method returns without doing anything.
		/// </para>
		/// </summary>
		private void TryReleaseContextUniqueApplicationInstanceId(ushort id)
		{
			try
			{
				this.ReturnContextUniqueApplicationInstanceId(id);
			}
			catch (Exception e)
			{
				var wrappingException = new Exception(
					$"{this.GetType().Name}.{nameof(this.ReturnContextUniqueApplicationInstanceId)} failed to return the ApplicationInstanceId.", e);
				Console.WriteLine(wrappingException.Message);
				this.TryHandleException(wrappingException);
				// Called on shutdown, so swallow the exception
			}
		}

		/// <summary>
		/// Writes to the console and calls the core overload.
		/// May throw if no value was ever rented.
		/// </summary>
		protected virtual void ReturnContextUniqueApplicationInstanceId(ushort id)
		{
			Console.WriteLine($"Returning ApplicationInstanceId {id} using {this.GetType().Name}...");

			this.ReturnContextUniqueApplicationInstanceIdCore(id);

			Console.WriteLine($"Returned ApplicationInstanceId {id} using {this.GetType().Name}.");
		}

		/// <summary>
		/// Provides the specific implementation to return a previously acquired ID.
		/// </summary>
		protected abstract void ReturnContextUniqueApplicationInstanceIdCore(ushort id);

		/// <summary>
		/// The default implementation returns the entry assembly's name, or a fixed value representing a test host if no entry assembly is available.
		/// </summary>
		protected virtual string GetApplicationName()
		{
			return Assembly.GetEntryAssembly()?.GetName().Name ?? "testhost"; // Stay predictable in unit tests (not a nice name, but it matches the value in IHostEnvironment)
		}

		/// <summary>
		/// The default implmeentation returns the machine name.
		/// </summary>
		protected virtual string GetServerName()
		{
			return Environment.MachineName;
		}
	}
}
