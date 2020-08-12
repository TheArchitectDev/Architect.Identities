using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.ApplicationInstanceIds.Implementations
{
	public sealed class StandardSqlApplicationInstanceIdSourceTests : IDisposable
	{
		private static string TableName => SqlApplicationInstanceIdSource.DefaultTableName;

		/// <summary>
		/// Not started by default, for performance.
		/// </summary>
		private IHost Host { get; }
		private IHostApplicationLifetime HostApplicationLifetime { get; }

		private UndisposableDbConnection Connection { get; }
		private DbConnection CreateDbConnection() => this.Connection;
		private StandardSqlApplicationInstanceIdSource Source { get; }

		public StandardSqlApplicationInstanceIdSourceTests()
		{
			var hostBuilder = new HostBuilder();
			this.Host = hostBuilder.Build();
			this.HostApplicationLifetime = this.Host.Services.GetRequiredService<IHostApplicationLifetime>();

			this.Connection = new UndisposableDbConnection(new SQLiteConnection("DataSource=:memory:;"));
			using var command = this.Connection.CreateCommand();
			this.Connection.Open();
			command.CommandText = $@"
CREATE TABLE {TableName} (
	id BIGINT UNSIGNED PRIMARY KEY,
	application_name CHAR(50),
	server_name CHAR(50),
	creation_datetime DATETIME(3) NOT NULL
)
;
";
			command.ExecuteNonQuery();
			this.Source = new StandardSqlApplicationInstanceIdSource(this.CreateDbConnection, databaseName: null, this.HostApplicationLifetime);
		}

		public void Dispose()
		{
			this.Connection?.TrulyDispose();
			this.Host?.Dispose();
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithNoPriorIds_ShouldReturnId1()
		{
			var id = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Equal(1, id);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithNoPriorIds_ShouldAddRow()
		{
			var rowCountBefore = Convert.ToInt32(this.ExecuteScalar($"SELECT COUNT(*) FROM {TableName};"));

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			var rowCountAfter = Convert.ToInt32(this.ExecuteScalar($"SELECT COUNT(*) FROM {TableName};"));

			Assert.Equal(0, rowCountBefore);
			Assert.Equal(1, rowCountAfter);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithNoPriorIds_ShouldAddId1()
		{
			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));

			Assert.Equal(1, id);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnlyPriorId1_ShouldReturnId2()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			var id = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Equal(2, id);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnlyPriorId1_ShouldAddId2()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));

			Assert.Equal(2, id);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnlyPriorId100_ShouldReturnId1()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (100, '', '', 0);");

			var id = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Equal(1, id);
		}

		[Fact]
		public void GetContextUniqueApplicationInstanceIdValue_WithOnePriorInvocation_ShouldReturnId2()
		{
			var otherSource = new StandardSqlApplicationInstanceIdSource(this.CreateDbConnection, databaseName: null, this.HostApplicationLifetime);
			_  = otherSource.ContextUniqueApplicationInstanceId.Value;

			var id = this.Source.ContextUniqueApplicationInstanceId.Value;

			Assert.Equal(2, id);
		}

		[Fact]
		public async Task StopHost_Regularly_ShouldDeleteId()
		{
			_ = this.Source.ContextUniqueApplicationInstanceId.Value;

			var id = Convert.ToInt32(this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};"));
			Assert.Equal(1, id);

			await this.Host.StopAsync();

			var nullId = this.ExecuteScalar($"SELECT MAX(id) FROM {TableName};");
			Assert.IsType<DBNull>(nullId);
		}

		[Fact]
		public async Task StopHost_WithMultipleIdsPresent_ShouldOnlyTouchOwnId()
		{
			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (1, '', '', 0);");

			var id = this.Source.ContextUniqueApplicationInstanceId.Value;
			Assert.Equal(2, id);

			this.ExecuteScalar($"INSERT INTO {TableName} VALUES (3, '', '', 0);");

			await this.Host.StopAsync();

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

		/// <summary>
		/// Helps pretend we have pooling, accessing a temporary, in-memory database while pretending to dispose connections in between operations.
		/// </summary>
		private sealed class UndisposableDbConnection : DbConnection
		{
			private DbConnection Connection { get; }

			public override string ConnectionString
			{
				get => this.Connection.ConnectionString;
				set => this.Connection.ConnectionString = value;
			}
			public override string Database => this.Connection.Database;
			public override string DataSource => this.Connection.DataSource;
			public override string ServerVersion => this.Connection.ServerVersion;
			public override ConnectionState State => this.Connection.State;

			private bool MayDispose { get; set; }

			public UndisposableDbConnection(DbConnection connection)
			{
				this.Connection = connection;
			}

			protected override void Dispose(bool disposing)
			{
				if (!this.MayDispose) return;
				base.Dispose(disposing);
				this.Connection.Dispose();
			}

			/// <summary>
			/// Actually disposes this object and the underlying DbConnection object.
			/// </summary>
			public void TrulyDispose()
			{
				this.MayDispose = true;
				this.Dispose();
			}

			public override void ChangeDatabase(string databaseName) => this.Connection.ChangeDatabase(databaseName);
			public override void Close()
			{
				if (!this.MayDispose) return;
				this.Connection.Close();
			}
			public override void Open()
			{
				if (this.Connection.State == ConnectionState.Open) return;
				this.Connection.Open();
			}
			protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => this.Connection.BeginTransaction(isolationLevel);
			protected override DbCommand CreateDbCommand() => this.Connection.CreateCommand();
		}
	}
}
