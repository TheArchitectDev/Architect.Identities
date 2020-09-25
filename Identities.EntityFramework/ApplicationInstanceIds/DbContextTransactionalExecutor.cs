using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Helps <see cref="DbContext"/>-based <see cref="IApplicationInstanceIdRenter"/> implementations perform work transactionally.
	/// </summary>
	internal sealed class DbContextTransactionalExecutor : IApplicationInstanceIdSourceTransactionalExecutor
	{
		private Func<DbContext> GetDbContext { get; }
		private bool ShouldDisposeDbContext { get; }

		public DbContextTransactionalExecutor(Func<DbContext> getDbContext, bool shouldDisposeDbContext)
		{
			this.GetDbContext = getDbContext ?? throw new ArgumentNullException(nameof(getDbContext));
			this.ShouldDisposeDbContext = shouldDisposeDbContext;
		}

		public object? ExecuteTransactionally(Func<DbConnection, object?> action)
		{
			var dbContext = this.GetDbContext();

			using (this.ShouldDisposeDbContext ? dbContext : null)
			{
				var executionStrategy = dbContext.Database.CreateExecutionStrategy();

				var result = executionStrategy.Execute(() =>
				{
					dbContext.Database.OpenConnection();
					var connection = dbContext.Database.GetDbConnection() ?? throw new Exception($"{nameof(DbContext)} produced null connection object.");

					using var transaction = dbContext.Database.BeginTransaction();

					var result = action(connection);

					transaction.Commit();

					return result;
				});

				return result;
			}
		}
	}
}
