using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Used to provide <see cref="DbConnection"/> objects to implementations that use a database.
	/// </summary>
	public sealed class ApplicationInstanceIdSourceDbConnectionFactory : IApplicationInstanceIdSourceDbConnectionFactory
	{
		private IServiceProvider ServiceProvider { get; }
		private Func<IServiceProvider, DbConnection> GetConnection { get; }
		private string? ConnectionString { get; }

		/// <summary>
		/// <para>
		/// Constructs a factory that uses the given <paramref name="getConnection"/> to get a <see cref="DbConnection"/> based on the given <paramref name="serviceProvider"/>.
		/// </para>
		/// <para>
		/// Note that whenever an <strong>open</strong> connection is returned, it will not be disposed. Preopened connections are assumed to be owned by another component.
		/// </para>
		/// </summary>
		public ApplicationInstanceIdSourceDbConnectionFactory(IServiceProvider serviceProvider, Func<IServiceProvider, DbConnection> getConnection, string? connectionString = null)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			this.GetConnection = getConnection ?? throw new ArgumentNullException(nameof(getConnection));
			this.ConnectionString = connectionString;
		}

		public DbConnection CreateDbConnection()
		{
			var connection = this.GetConnection(this.ServiceProvider) ?? throw new Exception("The factory produced a null connection object.");
			if (this.ConnectionString is not null) connection.ConnectionString = this.ConnectionString;
			return connection;
		}

		/// <summary>
		/// <para>
		/// Registers an <see cref="IApplicationInstanceIdSourceDbConnectionFactory"/> in the given <see cref="IServiceCollection"/>.
		/// The implementation resolves a <typeparamref name="TDatabaseConnectionFactory"/> from the container and uses <paramref name="getConnectionFromFactory"/> to obtain a <see cref="DbConnection"/>.
		/// </para>
		/// <para>
		/// Note that whenever an <strong>open</strong> connection is returned, it will not be disposed. Preopened connections are assumed to be owned by another component.
		/// </para>
		/// </summary>
		/// <typeparam name="TDatabaseConnectionFactory">The type of source <see cref="DbConnection"/> factory to obtain <see cref="DbConnection"/>s from.</typeparam>
		/// <param name="services">The collection to add a registration to.</param>
		/// <param name="getConnectionFromFactory">A method that gets a <see cref="DbConnection"/> from the <typeparamref name="TDatabaseConnectionFactory"/>.</param>
		/// <param name="connectionString">If given, the connection string is set on any created <see cref="DbConnection"/>.</param>
		public static void Register<TDatabaseConnectionFactory>(IServiceCollection services, Func<TDatabaseConnectionFactory, DbConnection> getConnectionFromFactory,
			string? connectionString = null)
			where TDatabaseConnectionFactory : class
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			if (getConnectionFromFactory is null) throw new ArgumentNullException(nameof(getConnectionFromFactory));

			services.AddTransient(CreateDbConnectionFactory);

			// Local function that returns a new IDbConnectionFactory instance
			IApplicationInstanceIdSourceDbConnectionFactory CreateDbConnectionFactory(IServiceProvider serviceProvider)
			{
				var factory = new ApplicationInstanceIdSourceDbConnectionFactory(serviceProvider, GetDbConnection, connectionString);
				return factory;
			}

			// Local function that returns a new DbConnection from the service provider
			DbConnection GetDbConnection(IServiceProvider serviceProvider)
			{
				var databaseConnectionFactory = serviceProvider.GetRequiredService<TDatabaseConnectionFactory>();
				var connection = getConnectionFromFactory(databaseConnectionFactory) ?? throw new Exception("The factory produced a null connection object.");
				return connection;
			}
		}
	}
}
