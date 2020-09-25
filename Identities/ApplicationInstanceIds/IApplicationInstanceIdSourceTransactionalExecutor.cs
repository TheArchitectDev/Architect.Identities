using System;
using System.Data.Common;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Provides an abstraction to help <see cref="IApplicationInstanceIdRenter"/> implementations perform work transactionally.
	/// </summary>
	public interface IApplicationInstanceIdSourceTransactionalExecutor
	{
		/// <summary>
		/// Executes the given action transactionally.
		/// </summary>
		object? ExecuteTransactionally(Func<DbConnection, object?> action);
	}
}
