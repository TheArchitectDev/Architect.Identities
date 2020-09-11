using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// An implementation specific to [Azure] SQL Server.
	/// </summary>
	internal sealed class SqlServerApplicationInstanceIdSource : StandardSqlApplicationInstanceIdSource
	{
		public SqlServerApplicationInstanceIdSource(Func<DbConnection> connectionFactory, string? databaseAndSchemaName,
			IHostApplicationLifetime applicationLifetime, Action<Exception>? exceptionHandler = null)
			: base(connectionFactory, databaseAndSchemaName, applicationLifetime, exceptionHandler)
		{
			ThrowIfDatabaseNameExcludesSchema(databaseAndSchemaName);
		}

		private static void ThrowIfDatabaseNameExcludesSchema(string? databaseAndSchemaName)
		{
			if (databaseAndSchemaName != null && databaseAndSchemaName.Count(chr => chr == '.') != 1)
			{
				throw new NotSupportedException(
					$"{nameof(SqlServerApplicationInstanceIdSource)} only supports specifying the database name outside of the connection string if the schema name is included, i.e. 'database.schema'.");
			}
		}

		protected override void CreateTableIfNotExists(DbConnection connection, string? databaseName)
		{
			if (connection.State != System.Data.ConnectionState.Open) throw new ArgumentException("Expected an open connection.");

			databaseName = this.GetDatabaseNamePrefix(databaseName);

			using var command = connection.CreateCommand();

			command.CommandText = $@"
IF OBJECT_ID(N'{databaseName}{DefaultTableName}', N'U') IS NULL BEGIN

CREATE TABLE {databaseName}{DefaultTableName} (
  id BIGINT NOT NULL PRIMARY KEY,
  application_name CHAR(50) NULL,
  server_name CHAR(50) NULL,
  creation_datetime DATETIME2(3) NOT NULL
)

END
;
";

			command.ExecuteNonQuery();
		}

		/// <summary>
		/// Returns the given database name in a format such that it can be prefixed to the table name.
		/// Throws if the given value is unsupported.
		/// </summary>
		protected override string? GetDatabaseNamePrefix(string? databaseName = null)
		{
			if (databaseName is null) return null;

			// Disallow empty string
			if (databaseName.Length == 0) throw new ArgumentException("The database name must be either null or non-empty.");

			System.Diagnostics.Debug.Assert(databaseName.Count(chr => chr == '.') == 1, "Format should have already been confirmed to be 'database.schema'.");

			return $"{databaseName}."; // Allow usage as a prefix to the table name
		}
	}
}
