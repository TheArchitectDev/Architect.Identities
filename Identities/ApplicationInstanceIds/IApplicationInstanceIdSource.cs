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
	/// </summary>
	public interface IApplicationInstanceIdSource
	{
		/// <summary>
		/// <para>
		/// An ID for the currently running application instance.
		/// </para>
		/// <para>
		/// The implementation determines the uniqueness of the ID.
		/// Generally an ID is unique within the application's bounded context: no other running instance of any application in that context has the same ID.
		/// </para>
		/// <para>
		/// Different runs of an application may result in different IDs, depending on the implementation.
		/// </para>
		/// </summary>
		public abstract ushort ApplicationInstanceId { get; }
	}
}
