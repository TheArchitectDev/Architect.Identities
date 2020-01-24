using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A concrete implementation for application instance ID management in Standard SQL.
	/// </para>
	/// <para>
	/// This implementation throws if the table does not exist, as few vendors properly support standard table creation syntax.
	/// Either the table must be created manually, or a vendor-specific implementation must be used.
	/// </para>
	/// <para>
	/// This implementation registers the smallest available ID by inserting it into a dedicated table.
	/// On application shutdown, it attempts to remove that ID, freeing it up again.
	/// </para>
	/// <para>
	/// Enough possible IDs should be available that an occassional failure to free up an ID is not prohibitive.
	/// </para>
	/// </summary>
	public class StandardSqlApplicationInstanceIdSource : SqlApplicationInstanceIdSource
	{
		public StandardSqlApplicationInstanceIdSource(Func<DbConnection> connectionFactory, string? databaseName,
			IHostApplicationLifetime applicationLifetime, Action<Exception>? exceptionHandler = null)
			: base(connectionFactory, databaseName, applicationLifetime, exceptionHandler)
		{
		}

		/// <summary>
		/// This implementation actually throws if the table does not exist, since we cannot easily create it using Standard SQL.
		/// </summary>
		protected override void CreateTableIfNotExists(DbConnection connection, string? databaseName)
		{
			if (connection.State != System.Data.ConnectionState.Open) throw new ArgumentException("Expected an open connection.");

			databaseName = this.GetDatabaseNamePrefix(databaseName);

			using var command = connection.CreateCommand();

			command.CommandText = $@"
SELECT MAX(1) FROM {databaseName}{DefaultTableName}
;
";

			try
			{
				command.ExecuteNonQuery();
			}
			catch (DbException)
			{
				throw new NotSupportedException($"The table {DefaultTableName} does not exist, but {this.GetType().Name} does not support table creation. Create it manually or use a vendor-specific implementation.");
			}
		}

		protected override void ConfigureCommandForGetContextUniqueApplicationInstanceId(DbCommand command, string? databaseName, string applicationName, string serverName)
		{
			databaseName = this.GetDatabaseNamePrefix(databaseName);

			var parameter = command.CreateParameter();
			parameter.ParameterName = "@ApplicationName";
			parameter.Value = applicationName;
			command.Parameters.Add(parameter);

			parameter = command.CreateParameter();
			parameter.ParameterName = "@ServerName";
			parameter.Value = serverName;
			command.Parameters.Add(parameter);

			parameter = command.CreateParameter();
			parameter.ParameterName = "@CreationDateTime";
			parameter.Value = DateTime.UtcNow;
			command.Parameters.Add(parameter);

			command.CommandText = $@"
-- Acquire exclusive lock on record 0 (regardless of prior existence)
DELETE FROM {databaseName}{DefaultTableName} WHERE id = 0;
INSERT INTO {databaseName}{DefaultTableName} (id, application_name, server_name, creation_datetime) VALUES (0, NULL, NULL, @CreationDateTime);

-- Insert smallest available ID
INSERT INTO {databaseName}{DefaultTableName}
SELECT 1 + MIN(id), @ApplicationName, @ServerName, @CreationDateTime
FROM {databaseName}{DefaultTableName} aii
WHERE NOT EXISTS (SELECT id FROM {databaseName}{DefaultTableName} WHERE id = 1 + aii.id)
;

-- Get the inserted ID
SELECT id
FROM {databaseName}{DefaultTableName}
WHERE application_name = @ApplicationName AND server_name = @ServerName AND id <> 0
AND creation_datetime = (SELECT MAX(creation_datetime) FROM {databaseName}{DefaultTableName} WHERE application_name = @ApplicationName AND server_name = @ServerName)
;

-- Release the lock
DELETE FROM {databaseName}{DefaultTableName} WHERE id = 0;
";
		}
		
		protected override void ConfigureCommandForDeleteContextUniqueApplicationInstanceId(DbCommand command, string? databaseName, ushort applicationInstanceId)
		{
			databaseName = this.GetDatabaseNamePrefix(databaseName);

			var parameter = command.CreateParameter();
			parameter.ParameterName = "@Id";
			parameter.Value = (int)applicationInstanceId; // Signed type to satisfy SqlServer/AzureSql
			command.Parameters.Add(parameter);

			command.CommandText = $@"
DELETE FROM {databaseName}{DefaultTableName} WHERE id = @Id;
";
		}

		/// <summary>
		/// Returns the given database name in a format such that it can be prefixed to the table name.
		/// Throws if the given value is unsupported.
		/// </summary>
		protected virtual string? GetDatabaseNamePrefix(string? databaseName = null)
		{
			if (databaseName is null) return null;

			// Disallow empty string
			if (databaseName.Length == 0) throw new ArgumentException("The database name must be either null or non-empty.");

			// Disallow backticks, control characters, and non-ASCII characters
			if (databaseName.Any(chr => !Char.IsLetterOrDigit(chr) && chr != '-' && chr != '_'))
			{
				throw new ArgumentException($"Parameter {nameof(databaseName)} contains unsupported characters, such as interpunction, symbols, or control characters: {databaseName}.");
			}

			return $"{databaseName}."; // Allow usage as a prefix to the table name
		}
	}
}
