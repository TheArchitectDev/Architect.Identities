using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A source of application instance IDs, used to provide a source of uniqueness among different applications and among instances of an application.
	/// </para>
	/// <para>
	/// Different runs (i.e. restarts) of an application instance may result in different IDs, depending on the implementation.
	/// </para>
	/// <para>
	/// Registration as a singleton is strongly recommended.
	/// </para>
	/// </summary>
	public interface IApplicationInstanceIdSource
	{
		/// <summary>
		/// <para>
		/// Contains an ID for this application instance.
		/// </para>
		/// <para>
		/// The implementation determines the uniqueness of the ID.
		/// Generally an ID is unique with the application's bounded context: no other running instance of any application in that context has the same ID.
		/// </para>
		/// <para>
		/// Depending on the implementation, IDs may change between runs of an application instance, i.e. across restarts.
		/// </para>
		/// <para>
		/// Once the component has been properly registered, this property is never null.
		/// </para>
		/// </summary>
		Lazy<ushort> ContextUniqueApplicationInstanceId { get; }
	}
}
