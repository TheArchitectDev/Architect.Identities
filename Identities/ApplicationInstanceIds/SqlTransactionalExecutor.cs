using System;
using System.Data;
using System.Data.Common;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Helps SQL-based <see cref="IApplicationInstanceIdRenter"/> implementations perform work transactionally.
	/// </summary>
	internal sealed class SqlTransactionalExecutor : IApplicationInstanceIdSourceTransactionalExecutor
	{
		private IApplicationInstanceIdSourceDbConnectionFactory ConnectionFactory { get; }

		public SqlTransactionalExecutor(IApplicationInstanceIdSourceDbConnectionFactory connectionFactory)
		{
			this.ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
		}

		public object? ExecuteTransactionally(Func<DbConnection, object?> action)
		{
			if (System.Transactions.Transaction.Current is not null)
				throw new Exception($"An an unexpected database transaction was present while attempting to create a new transaction.");

			var connection = this.ConnectionFactory.CreateDbConnection() ?? throw new Exception("The factory produced a null connection object.");

			var isConnectionClosed = connection.State == ConnectionState.Closed;

			// We own the connection only if we opened it
			using (isConnectionClosed ? connection : null)
			{
				if (isConnectionClosed) connection.Open();

				using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

				var result = action(connection);

				transaction.Commit();

				return result;
			}
		}
	}
}
