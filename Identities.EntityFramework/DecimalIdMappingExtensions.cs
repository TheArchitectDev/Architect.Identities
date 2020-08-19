using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Provides extensions for mapping decimal ID properties using EntityFramework.
	/// </summary>
	public static class DecimalIdMappingExtensions
	{
		private static ValueConverter DecimalConverter { get; }
			= new ValueConverter<decimal, string>(d => d.ToString(), s => Decimal.Parse(s));
		private static ValueConverter NullableDecimalConverter { get; }
			= new ValueConverter<decimal?, string?>(d => d == null ? null : d.ToString(), s => s == null ? (decimal?)null : Decimal.Parse(s));

		/// <summary>
		/// <para>
		/// Sets column type "DECIMAL(28,0)" for all currently mapped decimal properties (including nullable ones) whose name ends with "Id" or "ID".
		/// </para>
		/// <para>
		/// Invoke this after all properties have been configured.
		/// </para>
		/// <para>
		/// If the database type is SQLite, this method configures a different type and a custom conversion.
		/// Doing so avoids truncation of decimals to 8 bytes, and ensures that they are reconstituted with their original precision.
		/// </para>
		/// </summary>
		/// <param name="modelBuilder">The model builder whose configuration to update.</param>
		/// <param name="dbContext">The <see cref="DbContext"/> whose <see cref="ModelBuilder"/> is being configured. Accessed if a SQLite assembly is loaded.</param>
		/// <param name="columnType">The column type to configure for the properties. Can be changed for databases that require a different type.</param>
		public static ModelBuilder StoreDecimalIdsWithCorrectPrecision(this ModelBuilder modelBuilder, DbContext dbContext, string columnType = "DECIMAL(28,0)")
		{
			if (modelBuilder is null) throw new ArgumentNullException(nameof(modelBuilder));
			if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
			if (columnType is null) throw new ArgumentNullException(nameof(columnType));

			var isSqlite = IsSqlite(dbContext);

			var propertyMappings = modelBuilder.Model.GetEntityTypes()
				.Where(entityType => entityType.ClrType != null)
				.SelectMany(entityType => entityType.GetProperties())
				.Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
				.Where(property => property.Name.EndsWith("Id") || property.Name.EndsWith("ID"))
				.Select(property => (Property: property, Entity: modelBuilder.Entity(property.DeclaringEntityType.ClrType)))
				.Select(pair => (PropertyBuilder: pair.Entity.Property(pair.Property.Name), IsNullable: pair.Property.ClrType == typeof(decimal?)));

			foreach (var (propertyBuilder, isNullable) in propertyMappings)
			{
				StoreWithDecimalIdPrecision(propertyBuilder, isNullable, dbContext, columnType, isSqlite);
			}

			return modelBuilder;
		}

		/// <summary>
		/// <para>
		/// Sets column type "DECIMAL(28,0)" for the decimal property.
		/// </para>
		/// <para>
		/// If the database type is SQLite, this method configures a different type and a custom conversion.
		/// Doing so avoids truncation of decimals to 8 bytes, and ensures that they are reconstituted with their original precision.
		/// </para>
		/// <para>
		/// To do this without repetition for each decimal ID property, call <see cref="StoreDecimalIdsWithCorrectPrecision(ModelBuilder, DbContext, string)"/> on the <see cref="ModelBuilder"/>.
		/// </para>
		/// </summary>
		/// <param name="propertyBuilder">The property builder whose configuration to update.</param>
		/// <param name="dbContext">The <see cref="DbContext"/> whose <see cref="ModelBuilder"/> is being configured. Accessed if a SQLite assembly is loaded.</param>
		/// <param name="columnType">The column type to configure for the properties. Can be changed for databases that require a different type.</param>
		public static PropertyBuilder<decimal> StoreWithDecimalIdPrecision(this PropertyBuilder<decimal> propertyBuilder, DbContext dbContext, string columnType = "DECIMAL(28,0)")
		{
			StoreWithDecimalIdPrecision(propertyBuilder, isNullable: false, dbContext, columnType);
			return propertyBuilder;
		}

		/// <summary>
		/// <para>
		/// Sets column type "DECIMAL(28,0)" for the nullable decimal property.
		/// </para>
		/// <para>
		/// If the database type is SQLite, this method configures a different type and a custom conversion.
		/// Doing so avoids truncation of decimals to 8 bytes, and ensures that they are reconstituted with their original precision.
		/// </para>
		/// <para>
		/// To do this without repetition for each decimal ID property, call <see cref="StoreDecimalIdsWithCorrectPrecision(ModelBuilder, DbContext, string)"/> on the <see cref="ModelBuilder"/>.
		/// </para>
		/// </summary>
		/// <param name="propertyBuilder">The property builder whose configuration to update.</param>
		/// <param name="dbContext">The <see cref="DbContext"/> whose <see cref="ModelBuilder"/> is being configured. Accessed if a SQLite assembly is loaded.</param>
		/// <param name="columnType">The column type to configure for the properties. Can be changed for databases that require a different type.</param>
		public static PropertyBuilder<decimal?> StoreWithDecimalIdPrecision(this PropertyBuilder<decimal?> propertyBuilder, DbContext dbContext, string columnType = "DECIMAL(28,0)")
		{
			StoreWithDecimalIdPrecision(propertyBuilder, isNullable: true, dbContext, columnType);
			return propertyBuilder;
		}

		private static void StoreWithDecimalIdPrecision(PropertyBuilder decimalPropertyBuilder, bool isNullable, DbContext dbContext,
			string columnType = "DECIMAL(28,0)",
			bool? isSqlite = null)
		{
			if (decimalPropertyBuilder is null) throw new ArgumentNullException(nameof(decimalPropertyBuilder));

			System.Diagnostics.Debug.Assert(decimalPropertyBuilder.Metadata.PropertyInfo.PropertyType == typeof(decimal) ||
				decimalPropertyBuilder.Metadata.PropertyInfo.PropertyType == typeof(decimal?));

			if (isSqlite ?? IsSqlite(dbContext))
			{
				decimalPropertyBuilder.HasColumnType("TEXT");
				decimalPropertyBuilder.HasConversion(isNullable ? NullableDecimalConverter : DecimalConverter);
			}
			else
			{
				decimalPropertyBuilder.HasColumnType(columnType);
			}
		}

		/// <summary>
		/// Determines if the given <see cref="DbContext.Database" /> is SQLite through reflection, without using a hard dependency on Microsoft.EntityFrameworkCore.Sqlite.
		/// </summary>
		private static bool IsSqlite(DbContext dbContext)
		{
			var sqliteAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Sqlite,") == true);
			var databaseFacadeExtensionsType = sqliteAssembly?.GetTypes()
				.SingleOrDefault(type => type.Name == "SqliteDatabaseFacadeExtensions" && type.Namespace == "Microsoft.EntityFrameworkCore");
			var isSqliteMethod = databaseFacadeExtensionsType?.GetMethod("IsSqlite");

			if (isSqliteMethod != null && isSqliteMethod.ReturnType == typeof(bool) &&
				isSqliteMethod.GetParameters().Count() == 1 && isSqliteMethod.GetParameters().Single().ParameterType == typeof(DatabaseFacade))
			{
				if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));

				var result = isSqliteMethod.Invoke(obj: null, parameters: new[] { dbContext.Database });
				return (bool)result!;
			}

			return false;
		}
	}
}
