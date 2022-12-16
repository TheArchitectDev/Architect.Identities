using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

// #TODO: Reconsider this project

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
			= new ValueConverter<decimal?, string?>(d => d == null ? null : d.ToString(), s => s == null ? null : Decimal.Parse(s));

		/// <summary>
		/// <para>
		/// Sets column type "DECIMAL(28,0)" for all currently mapped decimal or decimal-convertible properties whose name ends with "Id" or "ID".
		/// </para>
		/// <para>
		/// Invoke this <em>after</em> all properties have been configured.
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
				.Where(property => GetName(property).EndsWith("Id") || GetName(property).EndsWith("ID"))
				.Where(property => GetClrType(property) is Type propertyType && (propertyType == typeof(decimal) || propertyType == typeof(decimal?) || IsDecimalConvertible(propertyType)))
				.Select(property => modelBuilder.Entity(GetClrType(property.DeclaringEntityType)).Property(GetName(property)));

			foreach (var propertyBuilder in propertyMappings)
				StoreWithDecimalIdPrecisionCore(propertyBuilder, dbContext, columnType, isSqlite);

			return modelBuilder;
		}

		/// <summary>
		/// <para>
		/// Sets column type "DECIMAL(28,0)" for the decimal or decimal-convertible property.
		/// </para>
		/// <para>
		/// If the database type is SQLite, this method configures a different type and a custom conversion.
		/// Doing so avoids truncation of decimals to 8 bytes, and ensures that they are reconstituted with their original precision.
		/// </para>
		/// <para>
		/// To do this without repetition for each decimal ID property, call <see cref="StoreDecimalIdsWithCorrectPrecision"/> on the <see cref="ModelBuilder"/>.
		/// </para>
		/// </summary>
		/// <param name="propertyBuilder">The property builder whose configuration to update.</param>
		/// <param name="dbContext">The <see cref="DbContext"/> whose <see cref="ModelBuilder"/> is being configured. Accessed if a SQLite assembly is loaded.</param>
		/// <param name="columnType">The column type to configure for the properties. Can be changed for databases that require a different type.</param>
		public static PropertyBuilder<T> StoreWithDecimalIdPrecision<T>(this PropertyBuilder<T> propertyBuilder, DbContext dbContext, string columnType = DefaultColumnType)
		{
			StoreWithDecimalIdPrecisionCore(propertyBuilder, dbContext, columnType);
			return propertyBuilder;
		}

		private static void StoreWithDecimalIdPrecisionCore(PropertyBuilder decimalPropertyBuilder, DbContext dbContext,
			string columnType = DefaultColumnType,
			bool? isSqlite = null)
		{
			if (decimalPropertyBuilder is null) throw new ArgumentNullException(nameof(decimalPropertyBuilder));

			var propertyType = GetPropertyInfo(decimalPropertyBuilder.Metadata)?.PropertyType;

			if (propertyType is null)
				throw new ArgumentException($"{GetName(decimalPropertyBuilder.Metadata.DeclaringEntityType)}.{GetName(decimalPropertyBuilder.Metadata)} is not a property or its type could not be determined.");

			if (propertyType != typeof(decimal) && // Not decimal
				propertyType != typeof(decimal?) && // Not nullable decimal
				!IsDecimalConvertible(propertyType)) // Not interchangeable with decimal
				throw new ArgumentException($"{GetName(decimalPropertyBuilder.Metadata.DeclaringEntityType)}.{GetName(decimalPropertyBuilder.Metadata)} is not a decimal, nullable decimal, or decimal-convertible type.");

			if (isSqlite ?? dbContext.IsSqlite())
			{
				ValueConverter converter;

				if (propertyType == typeof(decimal))
					converter = DecimalConverter;
				else if (propertyType == typeof(decimal?))
					converter = NullableDecimalConverter;
				else
				{
					var toProviderDelegateType = typeof(Func<,>).MakeGenericType(propertyType, typeof(string));
					var fromProviderDelegateType = typeof(Func<,>).MakeGenericType(typeof(string), propertyType);
					var codeValueParam = Expression.Parameter(propertyType, "codeValue");
					var dbValueParam = Expression.Parameter(typeof(string), "dbValue");

					converter = (ValueConverter)Activator.CreateInstance(typeof(ValueConverter<,>).MakeGenericType(propertyType, typeof(string)),
						Expression.Lambda(toProviderDelegateType, Expression.Call(Expression.Convert(codeValueParam, typeof(decimal)), typeof(decimal).GetMethod("ToString", Array.Empty<Type>())), codeValueParam),
						Expression.Lambda(fromProviderDelegateType, Expression.Convert(Expression.Call(typeof(decimal).GetMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) }), dbValueParam, Expression.Constant(CultureInfo.InvariantCulture)), propertyType), dbValueParam),
						null)!;
				}

				decimalPropertyBuilder.HasColumnType("TEXT");
				decimalPropertyBuilder.HasConversion(converter);
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
				// This workaround is needed because the library otherwise breaks if EF Core 6+ is used by the host application, due to breaking changes in EF
				return (PropertyInfo?)property.GetType().GetProperty(nameof(IMutableProperty.PropertyInfo))!.GetValue(property);
			}
		}

		/// <summary>
		/// Gets the ClrType property value from a given entity or property type, in a way that works across the EF Core 6+ breaking change.
		/// </summary>
		private static Type? GetClrType(object entityOrPropertyType)
		{
			// This workaround is needed because the library otherwise breaks if EF Core 6+ is used by the host application, due to breaking changes in EF
			return (Type?)entityOrPropertyType.GetType().GetProperty(nameof(IMutableProperty.ClrType))!.GetValue(entityOrPropertyType);
		}

		/// <summary>
		/// Gets the Name property value from a given entity or property type, in a way that works across the EF Core 6+ breaking change.
		/// </summary>
		private static string GetName(object entityOrPropertyType)
		{
			// This workaround is needed because the library otherwise breaks if EF Core 6+ is used by the host application, due to breaking changes in EF
			return (string)entityOrPropertyType.GetType().GetProperty(nameof(IMutableProperty.Name))!.GetValue(entityOrPropertyType)!;
		}

		/// <summary>
		/// Determines whether the given type is convertible to and from decimal.
		/// </summary>
		private static bool IsDecimalConvertible(Type type)
		{
			// Must have implicit conversion to decimal
			if (type.GetMethod("op_Implicit", 0, new[] { type })?.ReturnType != typeof(decimal))
				return false;

			// Must have explicit OR implicit conversion to decimal
			if (type.GetMethod("op_Explicit", 0, new[] { typeof(decimal) })?.ReturnType != type &&
				type.GetMethod("op_Implicit", 0, new[] { typeof(decimal) })?.ReturnType != type)
				return false;

			return true;
		}
	}
}
