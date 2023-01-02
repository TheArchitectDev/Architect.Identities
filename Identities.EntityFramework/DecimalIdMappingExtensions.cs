using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Provides extensions for mapping decimal ID properties using EntityFramework.
	/// </summary>
	public static class DecimalIdMappingExtensions
	{
		private static readonly Type[] ParameterListWithSingleDecimal = new[] { typeof(decimal) };

		/// <summary>
		/// <para>
		/// Configures the mapping of decimal ID types to DECIMAL(28,0).
		/// </para>
		/// <para>
		/// Primarily, this method registers a <see cref="DecimalIdConvention"/>, which configures properties named *Id or *ID, provided that they are of type decimal or have appropriate conversions to and from decimal.
		/// </para>
		/// <para>
		/// Additionaly, for any given assemblies, this method configures the default type mapping for any of their types named *Id or *ID that are convertible to and from decimal.
		/// The default type mapping is used when types occur in queries outside of properties, such as when Entity Framework writes calls to CAST().
		/// </para>
		/// </summary>
		/// <param name="modelAssemblies">Any assemblies containing types mapped to tables. For example, if domain objects are mapped directly, the domain layer's assembly should be passed here.</param>
		public static ModelConfigurationBuilder ConfigureDecimalIdTypes(this ModelConfigurationBuilder modelConfigurationBuilder, params Assembly[] modelAssemblies)
		{
			// Configure decimal-like ID properties
			modelConfigurationBuilder.Conventions.Add(_ => new DecimalIdConvention());

			// Configure decimal-like types outside of properties (e.g. in CAST(), SUM(), AVG(), etc.)
			foreach (var decimalIdType in modelAssemblies.SelectMany(assembly => assembly.GetTypes().Where(type =>
				type.Name.EndsWith("Id") &&
				IsDecimalConvertible(type))))
			{
				modelConfigurationBuilder.DefaultTypeMapping(decimalIdType)
					.HasConversion(typeof(CastingConverter<,>).MakeGenericType(decimalIdType, typeof(decimal)))
					.HasPrecision(28, 0);
			}

			return modelConfigurationBuilder;
		}

		/// <summary>
		/// Determines whether the given type is convertible to and from decimal.
		/// </summary>
		internal static bool IsDecimalConvertible(Type type)
		{
			// Must have explicit OR implicit conversion from decimal
			if (type.GetMethod("op_Explicit", genericParameterCount: 0, ParameterListWithSingleDecimal) is null && // Compiler enforces that return type is the type itself
				type.GetMethod("op_Implicit", genericParameterCount: 0, ParameterListWithSingleDecimal) is null) // Compiler enforces that return type is the type itself
				return false;

			// Must have implicit conversion to decimal
			if (!type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Any(method => method.Name == "op_Implicit" && method.ReturnType == typeof(decimal))) // Compiler enforces single parameter of the type itself
				return false;

			return true;
		}
	}
}
