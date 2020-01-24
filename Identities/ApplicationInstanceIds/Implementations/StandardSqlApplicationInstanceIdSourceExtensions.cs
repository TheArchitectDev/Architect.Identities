using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class StandardSqlApplicationInstanceIdSourceExtensions
	{
		/// <summary>
		/// <para>
		/// Registers an implementation based on Standard SQL, which should work with most SQL implementations.
		/// Use the options to specify the database connection.
		/// </para>
		/// <para>
		/// A vendor-specific implementation is preferred over this.
		/// Unlike this one, such an implementation may be able to create the table if it does not exist, and it may be optimized for that specific database.
		/// </para>
		/// <para>
		/// This overload takes a generic dependency to inject, and a function to get a DbConnection from that dependency.
		/// The dependency must be registered separately.
		/// </para>
		/// <para>
		/// The type parameter determines the database connection factory to get from the service provider, which should be registered separately.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist, or if the table does not exist.
		/// </para>
		/// <para>
		/// All database interaction except for cleanup is performed on startup, ensuring availability of the dependency before the application starts.
		/// </para>
		/// </summary>
		/// <param name="getConnectionFromFactory">A function that gets a new DbConnection from the registered connection factory.</param>
		/// <param name="connectionString">Written onto produced DbConnection objects, if given. Required only if a DbConnection is produced without a connection string.</param>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseStandardSql<TDatabaseConnectionFactory>(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<TDatabaseConnectionFactory, DbConnection> getConnectionFromFactory, string? connectionString = null,
			string? databaseName = null)
		{
			options.Services.AddSingleton(CreateInstance);
			return options;

			// Local function used to create an instance
			IApplicationInstanceIdSource CreateInstance(IServiceProvider serviceProvider)
			{
				var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
				var databaseConnectionFactory = serviceProvider.GetRequiredService<TDatabaseConnectionFactory>();
				var exceptionHandler = options.ExceptionHandlerFactory?.Invoke(serviceProvider);

				var instance = ApplicationInstanceIdSourceFactory.Create(applicationLifetime, () => new StandardSqlApplicationInstanceIdSource(
					GetConnectionFromFactory,
					databaseName,
					applicationLifetime,
					exceptionHandler));
				return instance;

				// Local function that returns a new DbConnection based on the registered connection factory, the function that gets a connection from it, and the optional connection string
				DbConnection GetConnectionFromFactory()
				{
					var connection = getConnectionFromFactory(databaseConnectionFactory) ?? throw new Exception("The factory produced a null connection object.");
					if (connectionString != null) connection.ConnectionString = connectionString;
					return connection;
				}
			}
		}

		/// <summary>
		/// <para>
		/// Registers an implementation based on Standard SQL, which should work with most SQL implementations.
		/// Use the options to specify the database connection.
		/// </para>
		/// <para>
		/// A vendor-specific implementation is preferred over this.
		/// Unlike this one, such an implementation may be able to create the table if it does not exist, and it may be optimized for that specific database.
		/// </para>
		/// <para>
		/// This overload takes a custom factory of DbConnection objects.
		/// </para>
		/// <para>
		/// The implementation will throw if the database does not exist, or if the table does not exist.
		/// </para>
		/// <para>
		/// All database interaction except for cleanup is performed on startup, ensuring availability of the dependency before the application starts.
		/// </para>
		/// </summary>
		/// <param name="connectionFactory">A function that provides new DbConnection objects.</param>
		/// <param name="connectionString">Written onto produced DbConnection objects, if given. Required only if a DbConnection is produced without a connection string.</param>
		/// <param name="databaseName">If the connection factory's connection string does not specify the database name, specify it here instead.</param>
		public static ApplicationInstanceIdSourceExtensions.Options UseStandardSql(this ApplicationInstanceIdSourceExtensions.Options options,
			Func<DbConnection> connectionFactory, string? connectionString = null,
			string? databaseName = null)
		{
			// Piggyback on the other overload, using a meaningless dependency that is always available
			return UseStandardSql<IHostApplicationLifetime>(options, _ => connectionFactory(), connectionString, databaseName);
		}
	}
}
