using System.Data.Common;
using System.Data.SQLite;
using Architect.Identities.ApplicationInstanceIds;
using Architect.Identities.IntegrationTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.IntegrationTests.ApplicationInstanceIds.Implementations
{
	public sealed class StandardSqlApplicationInstanceIdRenterTests : IDisposable
	{
		private static string TableName => SqlApplicationInstanceIdRenter.DefaultTableName;

		/// <summary>
		/// Not started by default, for performance.
		/// </summary>
		private IHost Host { get; }

		private UndisposableDbConnection Connection { get; }
		private DbConnection CreateDbConnection() => this.Connection;
		private StandardSqlApplicationInstanceIdRenter Renter { get; }

		public StandardSqlApplicationInstanceIdRenterTests()
		{
			var hostBuilder = new HostBuilder();
			hostBuilder.ConfigureServices(services => services.AddSingleton<IExceptionHandler>(new OptionalExceptionHandler(null)));
			hostBuilder.ConfigureServices(services => services.AddSingleton(sp => new ApplicationInstanceIdSourceDbConnectionFactory(sp, _ => this.CreateDbConnection())));
			hostBuilder.ConfigureServices(services => services.AddSingleton<IApplicationInstanceIdSourceTransactionalExecutor>(
				sp => new SqlTransactionalExecutor(sp.GetRequiredService<ApplicationInstanceIdSourceDbConnectionFactory>())));
			this.Host = hostBuilder.Build();

			this.Connection = new UndisposableDbConnection(new SQLiteConnection("DataSource=:memory:"));
			using var command = this.Connection.CreateCommand();
			this.Connection.Open();
			command.CommandText = $@"
CREATE TABLE {TableName} (
	id BIGINT PRIMARY KEY,
	application_name CHAR(50),
	server_name CHAR(50),
	creation_datetime DATETIME(3) NOT NULL
)
;
";
			command.ExecuteNonQuery();
			this.Renter = new StandardSqlApplicationInstanceIdRenter(this.Host.Services, databaseName: null);
		}

		public void Dispose()
		{
			this.Connection?.TrulyDispose();
			this.Host?.Dispose();
		}

		[Fact]
		public void RentId_WithNoPriorIds_ShouldReturnId1()
		{
			var id = this.Renter.RentId();

			Assert.Equal(1, id);
		}

		[Fact]
		public void RentId_WithNoPriorIds_ShouldAddRow()
		{
			var rowCountBefore = Convert.ToInt32(this.ExecuteScalar($"SELECT COUNT(*) FROM {TableName};"));

			_ = this.Renter.RentId();

			var rowCountAfter = Convert.ToInt32(this.ExecuteScalar($"SELECT COUNT(*) FROM {TableName};"));

			Assert.Equal(0, rowCountBefore);
			Assert.Equal(1, rowCountAfter);
		}

		[Fact]
		public void RentId_WithNoPriorIds_ShouldAddId1()
		{
			_ = this.Renter.RentId();

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));

			Assert.Equal(1, id);
		}

		[Fact]
		public void RentId_WithOnlyPriorId1_ShouldReturnId2()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			var id = this.Renter.RentId();

			Assert.Equal(2, id);
		}

		[Fact]
		public void RentId_WithOnlyPriorId1_ShouldAddId2()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			_ = this.Renter.RentId();

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));

			Assert.Equal(2, id);
		}

		[Fact]
		public void RentId_WithOnlyPriorId100_ShouldReturnId1()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (100, '', '', 0);");

			var id = this.Renter.RentId();

			Assert.Equal(1, id);
		}

		[Fact]
		public void RentId_WithOnePriorInvocation_ShouldReturnId2()
		{
			var exceptionHandler = new OptionalExceptionHandler(null);
			var connectionFactory = new ApplicationInstanceIdSourceDbConnectionFactory(this.Host.Services, _ => this.CreateDbConnection());
			var transactionalExecutor = new SqlTransactionalExecutor(connectionFactory);
			var otherSource = new StandardSqlApplicationInstanceIdRenter(this.Host.Services, databaseName: null);
			otherSource.RentId();

			var id = this.Renter.RentId();

			Assert.Equal(2, id);
		}

		[Fact]
		public void ReturnId_Regularly_ShouldDeleteId()
		{
			this.Renter.RentId();

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));
			Assert.Equal(1, id);

			this.Renter.ReturnId((ushort)id);

			var nullId = this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};");
			Assert.IsType<DBNull>(nullId);
		}

		[Fact]
		public void ReturnId_WithMultipleIdsPresent_ShouldOnlyTouchOwnId()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			var id = this.Renter.RentId();
			Assert.Equal(2, id);

			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (3, '', '', 0);");

			this.Renter.ReturnId((ushort)id);

			var idCount = Convert.ToInt32(this.ExecuteScalar($"SELECT COUNT(*) FROM {TableName};"));
			var minId = Convert.ToInt32(this.ExecuteScalar($"SELECT MIN(id) FROM {TableName};"));
			var maxId = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));
			Assert.Equal(2, idCount);
			Assert.Equal(1, minId);
			Assert.Equal(3, maxId);
		}

		private object ExecuteScalar(string queryString)
		{
			using var connection = this.CreateDbConnection();
			using var command = connection.CreateCommand();
			command.CommandText = queryString;
			connection.Open();
			return command.ExecuteScalar();
		}
	}
}
