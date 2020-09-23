using System;
using System.Data;
using System.Linq;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class DbContextApplicationInstanceIdSourceExtensions
	{
		// #TODO: Update documentation to demo this

		/// <summary>
		/// <para>
		/// Registers an implementation based on SQL Server / Azure SQL, using a registered IDbContextFactory or DbContext.
		/// The use of AddPooledDbContextFactory or AddDbContextFactory is strongly recommended.
		/// </para>
		/// <para>
		/// This overload makes use of the registered DbContext, including all of its configuration, such as auto-retrying execution strategies.
		/// </para>
		/// <para>
		/// The implementation will throw if the schema does not exist.
		/// The table, however, is created automatically, because different instances of the table may be used in various databases or bounded contexts.
		/// </para>
		/// </summary>
		/// <param name="databaseAndSchemaName">If the connection factory's connection string does not specify the database and schema name, specify database.schema here.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseSqlServerDbContext<TDbContext>(this ApplicationInstanceIdSourceExtensions.Options options,
			string? databaseAndSchemaName = null)
			where TDbContext : DbContext
		{
			options.UseSqlServer(() => new DummyDbConnection(), connectionString: null, databaseAndSchemaName);
			
			AddDbContextTransactionalExecutor<TDbContext>(options.Services);

			return options;
		}

		/// <summary>
		/// <para>
		/// Registers an implementation based on MySQL, using a registered IDbContextFactory or DbContext.
		/// The use of AddPooledDbContextFactory or AddDbContextFactory is strongly recommended.
		/// </para>
		/// <para>
		/// This overload makes use of the registered DbContext, including all of its configuration, such as auto-retrying execution strategies.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist.
		/// The table, however, is created automatically, because different instances of the table may be used in various databases or bounded contexts.
		/// </para>
		/// </summary>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseMySqlDbContext<TDbContext>(this ApplicationInstanceIdSourceExtensions.Options options,
			string? databaseName = null)
			where TDbContext : DbContext
		{
			options.UseMySql(() => new DummyDbConnection(), connectionString: null, databaseName);

			AddDbContextTransactionalExecutor<TDbContext>(options.Services);

			return options;
		}

		/// <summary>
		/// <para>
		/// Registers an implementation based on SQLite, using a registered IDbContextFactory or DbContext.
		/// The use of AddPooledDbContextFactory or AddDbContextFactory is strongly recommended.
		/// </para>
		/// <para>
		/// This overload makes use of the registered DbContext, including all of its configuration, such as auto-retrying execution strategies.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist.
		/// The table, however, is created automatically, because different instances of the table may be used in various databases or bounded contexts.
		/// </para>
		/// </summary>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseSqliteDbContext<TDbContext>(this ApplicationInstanceIdSourceExtensions.Options options,
			string? databaseName = null)
			where TDbContext : DbContext
		{
			options.UseSqlite(() => new DummyDbConnection(), connectionString: null, databaseName);

			AddDbContextTransactionalExecutor<TDbContext>(options.Services, isSqlite: true);

			return options;
		}

		/// <summary>
		/// <para>
		/// Registers an implementation based on Standard SQL, using a registered IDbContextFactory or DbContext.
		/// The implementation should work with most SQL implementations.
		/// The use of AddPooledDbContextFactory or AddDbContextFactory is strongly recommended.
		/// </para>
		/// <para>
		/// This overload makes use of the registered DbContext, including all of its configuration, such as auto-retrying execution strategies.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist, or if the table does not exist.
		/// </para>
		/// </summary>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseStandardSqlDbContext<TDbContext>(this ApplicationInstanceIdSourceExtensions.Options options,
			string? databaseName = null)
			where TDbContext : DbContext
		{
			options.UseStandardSql(() => new DummyDbConnection(), connectionString: null, databaseName);

			AddDbContextTransactionalExecutor<TDbContext>(options.Services);

			return options;
		}

		public static void AddDbContextTransactionalExecutor<TDbContext>(IServiceCollection services)
			where TDbContext : DbContext
		{
			AddDbContextTransactionalExecutor<TDbContext>(services, isSqlite: false);
		}

		private static void AddDbContextTransactionalExecutor<TDbContext>(IServiceCollection services, bool isSqlite)
			where TDbContext : DbContext
		{
			var iDbContextFactoryType = AppDomain.CurrentDomain.GetAssemblies()
				.Where(assembly => assembly.FullName?.StartsWith("Microsoft.EntityFrameworkCore,") == true)
				.SelectMany(assembly => assembly.GetTypes())
				.Where(type => type.Name.StartsWith("IDbContextFactory`"))
				.SingleOrDefault(type => type.Name.StartsWith("IDbContextFactory") && type.IsGenericType && !type.IsConstructedGenericType && type.GetGenericArguments().Length == 1);

			iDbContextFactoryType = iDbContextFactoryType?.MakeGenericType(typeof(TDbContext));

			var createDbContextMethod = iDbContextFactoryType?.GetMethods().SingleOrDefault(method => method.Name == "CreateDbContext" && method.GetParameters().Count() == 0);

			services.AddTransient(CreateTransactionalExecutor); // DbContext may be registered as anything, which we can support by being transient

			// Local function that creates a new transaction executor
			IApplicationInstanceIdSourceTransactionalExecutor CreateTransactionalExecutor(IServiceProvider serviceProvider)
			{
				Func<IServiceProvider, DbContext> getDbContext;
				bool shouldDisposeDbContext;

				var dbContextFactory = serviceProvider.GetService(iDbContextFactoryType);

				if (dbContextFactory != null && createDbContextMethod != null)
				{
					getDbContext = serviceProvider => (DbContext)(createDbContextMethod.Invoke(dbContextFactory, parameters: Array.Empty<object>())
						?? throw new Exception($"The factory produced a null {nameof(DbContext)}."));
					shouldDisposeDbContext = true;
				}
				else
				{
					getDbContext = serviceProvider => serviceProvider.GetRequiredService<TDbContext>();
					shouldDisposeDbContext = false; // Lifetime is the responsibility of the service provider
				}

				if (isSqlite)
				{
					var originalResolver = getDbContext;
					getDbContext = serviceProvider => GetSqliteDbContext(serviceProvider, originalResolver);
				}

				var transactionalExecutor = new DbContextTransactionalExecutor(() => getDbContext(serviceProvider), shouldDisposeDbContext);
				return transactionalExecutor;

				// Local function that gets the TDbContext from the IServiceProvider for SQLite
				DbContext GetSqliteDbContext(IServiceProvider serviceProvider, Func<IServiceProvider, DbContext> originalResolver)
				{
					var dbContext = originalResolver(serviceProvider);

					if (isSqlite)
					{
						var connection = dbContext.Database.GetDbConnection();
						if (connection.ConnectionString.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
						{
							using var serviceScope = serviceProvider.CreateScope();
							var secondDbContext = originalResolver(serviceScope.ServiceProvider);
							using (shouldDisposeDbContext ? secondDbContext : null)
							{
								var secondConnection = secondDbContext.Database.GetDbConnection();
								if (!ReferenceEquals(connection, secondConnection))
									throw new Exception($"To use an in-memory SQLite database, configure a fixed connection.");
							}
						}
						isSqlite = false;
					}

					return dbContext;
				}
			}
		}
	}
}
