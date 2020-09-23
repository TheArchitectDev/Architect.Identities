using System;
using System.Data.Common;
using Architect.Identities.ApplicationInstanceIds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class MySqlApplicationInstanceIdRenterExtensions
	{
		/// <summary>
		/// <para>
		/// Registers a MySql-based implementation. Use the options to specify the database connection.
		/// </para>
		/// <para>
		/// This overload takes a generic dependency to inject, and a function to get a <see cref="DbConnection"/> from that dependency.
		/// The dependency must be registered separately.
		/// </para>
		/// <para>
		/// The type parameter determines the database connection factory to get from the service provider, which should be registered separately.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist.
		/// The table, however, is created automatically, because different instances of the table may be used in various databases or bounded contexts.
		/// </para>
		/// </summary>
		/// <param name="getConnectionFromFactory">A function that gets a new <see cref="DbConnection"/> from the registered connection factory.</param>
		/// <param name="connectionString">Written onto produced <see cref="DbConnection"/> objects, if given. Required only if a <see cref="DbConnection"/> is produced without a connection string.</param>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseMySql<TDatabaseConnectionFactory>(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<TDatabaseConnectionFactory, DbConnection> getConnectionFromFactory, string? connectionString = null,
			string? databaseName = null)
			where TDatabaseConnectionFactory : class
		{
			// Register an IDbConnectionFactory
			ApplicationInstanceIdSourceDbConnectionFactory.Register(options.Services, getConnectionFromFactory, connectionString);

			// Register an IApplicationInstanceIdSourceTransactionalExecutor that uses the IDbConnectionFactory
			options.Services.AddTransient<IApplicationInstanceIdSourceTransactionalExecutor, SqlTransactionalExecutor>();

			// Register the IApplicationInstanceIdSource that uses all of the above
			options.Services.AddTransient(CreateInstance);

			return options;

			// Local function that creates a new instance
			IApplicationInstanceIdRenter CreateInstance(IServiceProvider serviceProvider)
			{
				var instance = new MySqlApplicationInstanceIdRenter(serviceProvider, databaseName);
				return instance;
			}
		}

		/// <summary>
		/// <para>
		/// Registers a MySql-based implementation. Use the options to specify the database connection.
		/// </para>
		/// <para>
		/// This overload takes a custom factory of <see cref="DbConnection"/> objects.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist.
		/// The table, however, is created automatically, because different instances of the table may be used in various databases or bounded contexts.
		/// </para>
		/// </summary>
		/// <param name="connectionFactory">A function that provides new DbConnection objects.</param>
		/// <param name="connectionString">Written onto produced <see cref="DbConnection"/> objects, if given. Required only if a <see cref="DbConnection"/> is produced without a connection string.</param>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseMySql(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<DbConnection> connectionFactory, string? connectionString = null,
			string? databaseName = null)
		{
			// Piggyback on the other overload, using a meaningless dependency that is always available
			return UseMySql<IHostApplicationLifetime>(options, _ => connectionFactory(), connectionString, databaseName);
		}
	}
}
