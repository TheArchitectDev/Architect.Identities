using System;
using System.Data.SQLite;
using Architect.Identities.IntegrationTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.IntegrationTests.ApplicationInstanceIds.Implementations
{
	public sealed class SqliteApplicationInstanceIdSourceExtensionsTests : IDisposable
	{
		private IHost Host { get; }
		private UndisposableDbConnection Connection { get; }

		public SqliteApplicationInstanceIdSourceExtensionsTests()
		{
			this.Connection = new UndisposableDbConnection(new SQLiteConnection("DataSource=:memory:"));
			this.Connection.Open();

			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlite(() => this.Connection));
			});

			this.Host = hostBuilder.Build();
		}

		public void Dispose()
		{
			this.Connection?.TrulyDispose();
			this.Host?.Dispose();
		}

		[Fact]
		public void UseSqlite_AfterDependencyResolution_ShouldCreateTable()
		{
			_ = this.Host.Services.GetRequiredService<IApplicationInstanceIdSource>(); // Create the dependency

			this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdSource.DefaultTableName};"); // Should succeed
		}

		[Fact]
		public void UseSqlite_AfterDependencyResolution_ShouldHaveRegisteredOneApplicationInstanceId()
		{
			_ = this.Host.Services.GetRequiredService<IApplicationInstanceIdSource>(); // Create the dependency

			var count = this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdSource.DefaultTableName};");

			Assert.Equal(1L, count);
		}

		private object ExecuteScalar(string queryString)
		{
			using var connection = this.Connection;
			using var command = connection.CreateCommand();
			command.CommandText = queryString;
			connection.Open();
			return command.ExecuteScalar();
		}
	}
}
