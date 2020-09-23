using System;
using System.Data.Common;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// A MySQL-specific implementation.
	/// </summary>
	internal sealed class MySqlApplicationInstanceIdRenter : StandardSqlApplicationInstanceIdRenter
	{
		public MySqlApplicationInstanceIdRenter(IServiceProvider serviceProvider, string? databaseName)
			: base(serviceProvider, databaseName)
		{
		}

		protected override void CreateTableIfNotExists(DbConnection connection, string? databaseName)
		{
			if (connection.State != System.Data.ConnectionState.Open) throw new ArgumentException("Expected an open connection.");

			databaseName = this.GetDatabaseNamePrefix(databaseName);

			using var command = connection.CreateCommand();

			command.CommandText = $@"
CREATE TABLE IF NOT EXISTS {databaseName}`{DefaultTableName}`(  
  `id` BIGINT UNSIGNED NOT NULL,
  `application_name` CHAR(50),
  `server_name` CHAR(50),
  `creation_datetime` DATETIME(3) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=INNODB CHARSET=ASCII COLLATE=ascii_general_ci
;
";

			command.ExecuteNonQuery();
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

			command.CommandText = $@"
-- Acquire exclusive lock on record 0 (regardless of prior existence)
REPLACE INTO {databaseName}{DefaultTableName} (id, application_name, server_name, creation_datetime) VALUES (0, NULL, NULL, NOW(3));

-- Insert smallest available ID
INSERT INTO {databaseName}{DefaultTableName}
SELECT 1 + id, @ApplicationName, @ServerName, NOW(3)
FROM {databaseName}{DefaultTableName} aii
WHERE NOT EXISTS (SELECT id FROM {databaseName}{DefaultTableName} WHERE id = 1 + aii.id)
ORDER BY id
LIMIT 1
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

		/// <summary>
		/// Returns the given database name in a format such that it can be prefixed to the table name.
		/// Throws if the given value is unsupported.
		/// </summary>
		protected override string? GetDatabaseNamePrefix(string? databaseName = null)
		{
			if (databaseName is null) return null;

			// Disallow empty string
			if (databaseName.Length == 0) throw new ArgumentException("The database name must be either null or non-empty.");

			// Disallow backticks, control characters, and non-ASCII characters
			if (databaseName.AsSpan().IndexOf('`') >= 0 || databaseName.Any(chr => Char.IsControl(chr) || chr >= 128))
			{
				throw new ArgumentException($"Parameter {nameof(databaseName)} contains unsupported characters, such as backticks, control characters, or non-ASCII characters: {databaseName}.");
			}

			return $"`{databaseName}`."; // Allow usage as a prefix to the table name
		}
	}
}
