using System.Data.SQLite;
using Architect.Identities.ApplicationInstanceIds;
using Architect.Identities.IntegrationTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.IntegrationTests.ApplicationInstanceIds.Implementations
{
	public sealed class SqliteApplicationInstanceIdRenterExtensionsTests : IDisposable
	{
		private List<Action<IServiceCollection>> RegistrationActions { get; } = new List<Action<IServiceCollection>>();

		private IHostBuilder HostBuilder { get; } = new HostBuilder();
		private IHost Host
		{
			get
			{
				if (this._host is null)
				{
					foreach (var action in this.RegistrationActions)
						this.HostBuilder.ConfigureServices(action);

					this._host = this.HostBuilder.Build();
				}
				return this._host;
			}
		}
		private IHost _host;

		private UndisposableDbConnection Connection { get; }

		public SqliteApplicationInstanceIdRenterExtensionsTests()
		{
			this.Connection = new UndisposableDbConnection(new SQLiteConnection("DataSource=:memory:"));
			this.Connection.Open();

			this.HostBuilder.ConfigureServices(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlite(() => this.Connection));
			});
		}

		public void Dispose()
		{
			this.Connection?.TrulyDispose();
			this.Host?.Dispose();
		}

		[Fact]
		public void UseSqlite_OnUseApplicationInstanceIdSource_ShouldCreateTable()
		{
			Assert.ThrowsAny<Exception>(() => this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};")); // Should throw because of missing table

			this.Host.UseApplicationInstanceIdSource();

			this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};"); // Should succeed
		}

		[Fact]
		public void UseSqlite_OnUseApplicationInstanceIdSource_ShouldRegisterOneApplicationInstanceId()
		{
			Assert.ThrowsAny<Exception>(() => this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};")); // Should throw because of missing table

			this.Host.UseApplicationInstanceIdSource();

			var count = this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};");

			Assert.Equal(1L, count);
		}

		[Fact]
		public async Task UseSqlite_AfterShutdownWithUseApplicationInstanceIdSource_ShouldHaveNoRemainingRegisteredApplicationInstanceIds()
		{
			Assert.ThrowsAny<Exception>(() => this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};")); // Should throw because of missing table

			this.Host.UseApplicationInstanceIdSource();

			this.Host.Start();
			await this.Host.StopAsync();

			var count = this.ExecuteScalar($"SELECT COUNT(*) FROM {SqlApplicationInstanceIdRenter.DefaultTableName};");

			Assert.Equal(0L, count);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqlite_WhenResolvedWithInMemoryNonFixedConnection_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlite(() =>
				{
					var connection = new SQLiteConnection("Data Source=:memory:");
					connection.Open();
					return connection;
				})); // Not preopened
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
		}

		/// <summary>
		/// In-memory connections need to be preopened and reused. The lack of preopening we can detect.
		/// </summary>
		[Fact]
		public void UseSqlite_WhenResolvedWithInMemoryUnopenedConnection_ShouldThrow()
		{
			this.RegistrationActions.Add(services =>
			{
				services.AddApplicationInstanceIdSource(source => source.UseSqlite(() => new SQLiteConnection("Data Source=:memory:"))); // Not preopened
			});

			var exception = Assert.Throws<Exception>(() => this.Host.UseApplicationInstanceIdSource());
			Assert.Contains("failed to rent", exception.Message);
			Assert.NotNull(exception.InnerException);
			Assert.Contains("in-memory", exception.InnerException?.Message);
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
