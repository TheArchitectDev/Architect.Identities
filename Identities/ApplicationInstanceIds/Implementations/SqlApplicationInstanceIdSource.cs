﻿using System;
using System.Data.Common;
using System.Transactions;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Base implementation for SQL-based application instance ID management.
	/// </para>
	/// <para>
	/// This implementation registers the smallest available ID by inserting it into a dedicated table.
	/// On application shutdown, it attempts to remove that ID, freeing it up again.
	/// </para>
	/// <para>
	/// Enough possible IDs should be available that an occassional failure to free up an ID is not prohibitive.
	/// </para>
	/// </summary>
	public abstract class SqlApplicationInstanceIdSource : BaseApplicationInstanceIdSource
	{
		public const string DefaultTableName = "application_instance_id";

		/// <summary>
		/// <para>
		/// By setting this to another method, a custom implementation can be provided for acquiring a <see cref="DbConnection"/> and performing one or more commands transactionally.
		/// </para>
		/// <para>
		/// This should be implemented very carefully. The current class' implementation and the DbContext-based one provide good examples.
		/// </para>
		/// <para>
		/// Note that by overwriting this, it is possible to render <see cref="ConnectionFactory"/> unused.
		/// </para>
		/// </summary>
		public Func<Func<DbConnection, object?>, object?> ExecuteTransactionallyFunc { get; set; }
		private Func<DbConnection> ConnectionFactory { get; }
		private string? DatabaseName { get; }

		protected SqlApplicationInstanceIdSource(Func<DbConnection> connectionFactory, string? databaseName,
			IHostApplicationLifetime applicationLifetime, Action<Exception>? exceptionHandler = null)
			: base(applicationLifetime, exceptionHandler)
		{
			this.ExecuteTransactionallyFunc = this.ExecuteTransactionally;
			this.ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
			if (Transaction.Current != null)
				throw new Exception($"Unexpected database transaction during {this.GetType().Name}.{nameof(this.GetContextUniqueApplicationInstanceId)}.");

			using var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted });

			using var connection = this.ConnectionFactory() ?? throw new Exception("The database connection factory produced a null connection.");

			if (connection.State == System.Data.ConnectionState.Closed)
				connection.Open();

			var result = action(connection);

			transactionScope.Complete();

			return result;
		}

		protected override ushort GetContextUniqueApplicationInstanceIdCore()
		{
			if (Transaction.Current != null)
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

		protected override void DeleteContextUniqueApplicationInstanceIdCore()
		{
			using (new TransactionScope(TransactionScopeOption.Suppress)) // Ignore ambient transactions
			{
				this.ExecuteTransactionally(connection =>
				{
					using var command = connection.CreateCommand();

					command.CommandTimeout = 3; // Seconds

					this.ConfigureCommandForDeleteContextUniqueApplicationInstanceId(command, this.DatabaseName, this.ContextUniqueApplicationInstanceId.Value);

					if (command.CommandText is null)
						throw new Exception($"{this.GetType().Name}.{nameof(this.ConfigureCommandForDeleteContextUniqueApplicationInstanceId)} failed to set a command text.");

					var affectedRowCount = this.ExecuteCommandForDeleteContextUniqueApplicationInstanceId(command);

					if (affectedRowCount != 1)
						throw new Exception($"{this.GetType().Name}.{nameof(this.DeleteContextUniqueApplicationInstanceId)} affected {affectedRowCount} rows instead of the expected 1.");
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
