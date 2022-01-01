using System;
using System.Data.Common;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// <para>
	/// Base implementation for SQL-based application instance ID management.
	/// </para>
	/// <para>
	/// This implementation rents the smallest available ID by inserting it into a dedicated table.
	/// On returning, it attempts to remove that ID, freeing it up again.
	/// </para>
	/// </summary>
	public abstract class SqlApplicationInstanceIdRenter : BaseApplicationInstanceIdRenter
	{
		public const string DefaultTableName = "application_instance_id";

		protected string TableName { get; } = DefaultTableName;

		private IApplicationInstanceIdSourceTransactionalExecutor TransactionalExecutor { get; }

		private string? DatabaseName { get; }

		protected SqlApplicationInstanceIdRenter(IServiceProvider serviceProvider, string? databaseName)
			: base(serviceProvider)
		{
			// We use the service locator anti-pattern here, to reduce constructor parameter explosion, since we tend to register the appropriate type anyway

			this.TransactionalExecutor = serviceProvider.GetRequiredService<IApplicationInstanceIdSourceTransactionalExecutor>();

			var customTableName = serviceProvider.GetService<ApplicationInstanceIdCustomTableName>();
			if (customTableName is not null) this.TableName = customTableName.TableName;

			this.DatabaseName = databaseName;

			if (this.DatabaseName?.Length == 0) throw new ArgumentException("The database name must be either null or non-empty.");
		}

		protected object? ExecuteTransactionally(Action<DbConnection> action)
		{
			return this.ExecuteTransactionally(connection =>
			{
				action(connection);
				return null;
			});
		}

		protected virtual object? ExecuteTransactionally(Func<DbConnection, object?> action)
		{
			return this.TransactionalExecutor.ExecuteTransactionally(action);
		}

		protected override ushort GetContextUniqueApplicationInstanceIdCore()
		{
			if (Transaction.Current is not null)
				throw new Exception($"Unexpected database transaction during {this.GetType().Name}.{nameof(this.GetContextUniqueApplicationInstanceId)}.");

			this.ExecuteTransactionally(connection => this.CreateTableIfNotExists(connection, this.DatabaseName));

			var result = this.ExecuteTransactionally(connection =>
			{
				using var command = connection.CreateCommand();

				command.CommandTimeout = 3; // Seconds

				this.ConfigureCommandForGetContextUniqueApplicationInstanceId(command, this.DatabaseName, this.GetApplicationName(), this.GetServerName());

				if (command.CommandText is null)
					throw new Exception($"{this.GetType().Name}.{nameof(this.ConfigureCommandForGetContextUniqueApplicationInstanceId)} failed to set a command text.");

				var applicationInstanceId = this.ExecuteCommandForGetContextUniqueApplicationInstanceId(command);

				if (applicationInstanceId == 0)
					throw new Exception($"{this.GetType().Name} provided an invalid application instance identifier of 0.");

				return applicationInstanceId;
			});

			System.Diagnostics.Debug.Assert(result is ushort);

			return (ushort)result;
		}

		protected override void ReturnContextUniqueApplicationInstanceIdCore(ushort id)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress)) // Ignore ambient transactions
			{
				this.ExecuteTransactionally(connection =>
				{
					using var command = connection.CreateCommand();

					command.CommandTimeout = 3; // Seconds

					this.ConfigureCommandForDeleteContextUniqueApplicationInstanceId(command, this.DatabaseName, id);

					if (command.CommandText is null)
						throw new Exception($"{this.GetType().Name}.{nameof(this.ConfigureCommandForDeleteContextUniqueApplicationInstanceId)} failed to set a command text.");

					var affectedRowCount = this.ExecuteCommandForDeleteContextUniqueApplicationInstanceId(command);

					if (affectedRowCount != 1)
						throw new Exception($"{this.GetType().Name}.{nameof(this.ReturnContextUniqueApplicationInstanceId)} affected {affectedRowCount} rows instead of the expected 1.");
				});
			}
		}

		/// <summary>
		/// Allows an implementation to guarantee the existence of the table.
		/// </summary>
		protected abstract void CreateTableIfNotExists(DbConnection openConnection, string? databaseName);

		/// <summary>
		/// Allows an implementation to set the query and parameters to get a context-unique application instance id.
		/// Executed without a transaction scope. The query itself may use a database transaction with any isolation level it deems appropriate.
		/// </summary>
		protected abstract void ConfigureCommandForGetContextUniqueApplicationInstanceId(DbCommand command, string? databaseName, string applicationName, string serverName);

		/// <summary>
		/// Allows an implementation to set the query and parameters to delete its context-unique application instance id.
		/// Executed without a transaction scope. The query itself may use a database transaction with any isolation level it deems appropriate.
		/// </summary>
		protected abstract void ConfigureCommandForDeleteContextUniqueApplicationInstanceId(DbCommand command, string? databaseName, ushort applicationInstanceId);

		/// <summary>
		/// Allows an implementation to change the behavior of how to execute the query and get the result, to get a context-unique application instance id.
		/// The default implementation uses ExecuteScalar() and Convert.ToUInt16().
		/// </summary>
		protected virtual ushort ExecuteCommandForGetContextUniqueApplicationInstanceId(DbCommand command)
		{
			var resultObject = command.ExecuteScalar();
			var applicationInstanceId = Convert.ToUInt16(resultObject);
			return applicationInstanceId;
		}

		/// <summary>
		/// Allows an implementation to change the behavior of how to execute the query and return the affected row count, to delete the context-unique application instance id.
		/// The default implementation uses ExecuteNonQuery().
		/// </summary>
		protected virtual int ExecuteCommandForDeleteContextUniqueApplicationInstanceId(DbCommand command)
		{
			var resultCount = command.ExecuteNonQuery();
			return resultCount;
		}
	}
}
