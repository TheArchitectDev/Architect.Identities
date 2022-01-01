using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Helps detect if SQLite is being used.
	/// </summary>
	public static class SqliteDetector
	{
		/// <summary>
		/// Determines through reflection if the given <see cref="DbContext.Database" /> is SQLite, without using a hard dependency on Microsoft.EntityFrameworkCore.Sqlite.
		/// </summary>
		public static bool IsSqlite(this DbContext dbContext)
		{
			return IsSqlite(dbContext?.Database!); // Should not be null, but allows it unless the SQLite assembly is detected, so be as forgiving as possible
		}

		/// <summary>
		/// Determines through reflection if the given <see cref="DatabaseFacade" /> is SQLite, without using a hard dependency on Microsoft.EntityFrameworkCore.Sqlite.
		/// </summary>
		public static bool IsSqlite(this DatabaseFacade database)
		{
			var sqliteAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Sqlite,") == true);
			var databaseFacadeExtensionsType = sqliteAssembly?.GetTypes()
				.SingleOrDefault(type => type.Name == "SqliteDatabaseFacadeExtensions" && type.Namespace == "Microsoft.EntityFrameworkCore");
			var isSqliteMethod = databaseFacadeExtensionsType?.GetMethod("IsSqlite");

			if (isSqliteMethod is not null && isSqliteMethod.ReturnType == typeof(bool) &&
				isSqliteMethod.GetParameters().Length == 1 && isSqliteMethod.GetParameters().Single().ParameterType == typeof(DatabaseFacade))
			{
				if (database is null) throw new ArgumentNullException(nameof(database));

				var result = isSqliteMethod.Invoke(obj: null, parameters: new[] { database });
				return (bool)result!;
			}

			return false;
		}
	}
}
