using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Provides extensions for mapping decimal ID properties using EntityFramework.
	/// </summary>
	public static class DecimalIdMappingExtensions
	{
		private const string DefaultColumnType = "DECIMAL(28,0)";

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
		public static ModelBuilder StoreDecimalIdsWithCorrectPrecision(this ModelBuilder modelBuilder, DbContext dbContext, string columnType = DefaultColumnType)
		{
			if (modelBuilder is null) throw new ArgumentNullException(nameof(modelBuilder));
			if (dbContext is null) throw new ArgumentNullException(nameof(dbContext));
			if (columnType is null) throw new ArgumentNullException(nameof(columnType));

			var isSqlite = dbContext.IsSqlite();

			var propertyMappings = modelBuilder.Model.GetEntityTypes()
				.Where(entityType => GetClrType(entityType) is not null)
				.SelectMany(entityType => entityType.GetProperties())
				.Where(property => GetClrType(property) == typeof(decimal) || GetClrType(property) == typeof(decimal?))
				.Where(property => GetName(property).EndsWith("Id") || GetName(property).EndsWith("ID"))
				.Select(property => (Property: property, Entity: modelBuilder.Entity(GetClrType(property.DeclaringEntityType))))
				.Select(pair => (PropertyBuilder: pair.Entity.Property(GetName(pair.Property)), IsNullable: GetClrType(pair.Property) == typeof(decimal?)));

			foreach (var (propertyBuilder, isNullable) in propertyMappings)
			{
				StoreWithDecimalIdPrecision(propertyBuilder, isNullable, dbContext, columnType, isSqlite);
			}

			return modelBuilder;

			// Local function that gets the ClrType property value from a given entity or property type
			static Type? GetClrType(object entityOrPropertyType)
			{
				// This workaround is needed because the library otherwise breaks if EF 6+ is used by the host application, due to breaking changes in EF
				return (Type?)entityOrPropertyType.GetType().GetProperty(nameof(IMutableProperty.ClrType))!.GetValue(entityOrPropertyType);
			}

			// Local function that gets the Name property value from a given entity or property type
			static string GetName(object entityOrPropertyType)
			{
				// This workaround is needed because the library otherwise breaks if EF 6+ is used by the host application, due to breaking changes in EF
				return (string)entityOrPropertyType.GetType().GetProperty(nameof(IMutableProperty.Name))!.GetValue(entityOrPropertyType)!;
			}
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
		public static PropertyBuilder<decimal> StoreWithDecimalIdPrecision(this PropertyBuilder<decimal> propertyBuilder, DbContext dbContext, string columnType = DefaultColumnType)
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
		public static PropertyBuilder<decimal?> StoreWithDecimalIdPrecision(this PropertyBuilder<decimal?> propertyBuilder, DbContext dbContext, string columnType = DefaultColumnType)
		{
			StoreWithDecimalIdPrecision(propertyBuilder, isNullable: true, dbContext, columnType);
			return propertyBuilder;
		}

		private static void StoreWithDecimalIdPrecision(PropertyBuilder decimalPropertyBuilder, bool isNullable, DbContext dbContext,
			string columnType = DefaultColumnType,
			bool? isSqlite = null)
		{
			if (decimalPropertyBuilder is null) throw new ArgumentNullException(nameof(decimalPropertyBuilder));

			System.Diagnostics.Debug.Assert(GetPropertyInfo(decimalPropertyBuilder.Metadata)?.PropertyType == typeof(decimal) ||
				GetPropertyInfo(decimalPropertyBuilder.Metadata)?.PropertyType == typeof(decimal?));

			if (isSqlite ?? dbContext.IsSqlite())
			{
				decimalPropertyBuilder.HasColumnType("TEXT");
				decimalPropertyBuilder.HasConversion(isNullable ? NullableDecimalConverter : DecimalConverter);
			}
			else if (columnType == DefaultColumnType && PropertyBuilderPrecisionSetter.Value is not null)
			{
				// We prefer to use the dynamic setter, since it is not tied to an explicit string representation, allowing broader support by providers
				PropertyBuilderPrecisionSetter.Value.Invoke(decimalPropertyBuilder, 28, 0);
			}
			else
			{
				decimalPropertyBuilder.HasColumnType(columnType);
			}

			// Local function that gets the PropertyInfo property value from a given property
			static PropertyInfo? GetPropertyInfo(object property)
			{
				// This workaround is needed because the library otherwise breaks if EF 6+ is used by the host application, due to breaking changes in EF
				return (PropertyInfo?)property.GetType().GetProperty(nameof(IMutableProperty.PropertyInfo))!.GetValue(property);
			}
		}
	}
}
