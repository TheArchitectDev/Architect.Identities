using System;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Provides a custom table name to be used by an <see cref="IApplicationInstanceIdRenter"/>.
	/// </summary>
	public sealed class ApplicationInstanceIdCustomTableName
	{
		public string TableName { get; }

		public ApplicationInstanceIdCustomTableName(string tableName)
		{
			this.TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
		}
	}
}
