using System.Data.Common;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Provides a <see cref="DbConnection"/> to <see cref="IApplicationInstanceIdRenter"/>-related types.
	/// </summary>
	public interface IApplicationInstanceIdSourceDbConnectionFactory
	{
		/// <summary>
		/// <para>
		/// Returns a <see cref="DbConnection"/>.
		/// </para>
		/// <para>
		/// If an open connection is returned, it is assumed to be owned by another component and should not be disposed by the caller.
		/// If it is not open, the connection becomes owned by the caller and should be disposed.
		/// </para>
		/// </summary>
		DbConnection CreateDbConnection();
	}
}
