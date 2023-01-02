using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.Identities.EntityFramework

{
	/// <summary>
	/// <para>
	/// An <see cref="IPropertyAddedConvention"/> that maps decimal-like ID properties using a <see cref="CastingConverter{TModel, TProvider}"/>.
	/// </para>
	/// <para>
	/// For a property to match, its name must be *Id or *ID.
	/// Additionally, it must either be of type decimal, or be implicitly convertible to decimal and (implicitly or explicitly) convertible from decimal.
	/// </para>
	/// <para>
	/// Beware that property mappings alone do not cover scenarios such as where Entity Framework writes calls to CAST().
	/// <see cref="ModelConfigurationBuilder.DefaultTypeMapping(Type)"/> can fix such scenarios.
	/// </para>
	/// </summary>
	public sealed class DecimalIdConvention : IPropertyAddedConvention
	{
		public void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
		{
			// ID properties only
			if (!propertyBuilder.Metadata.Name.EndsWith("Id") && !propertyBuilder.Metadata.Name.EndsWith("ID"))
				return;

			var type = propertyBuilder.Metadata.ClrType;

			// Decimal-like types only
			if (type != typeof(decimal) && !DecimalIdMappingExtensions.IsDecimalConvertible(type))
				return;

			if (type != typeof(decimal))
				propertyBuilder.HasConverter(typeof(CastingConverter<,>).MakeGenericType(type, typeof(decimal)));

			propertyBuilder.HasPrecision(28);
			propertyBuilder.HasScale(0);
		}
	}
}
