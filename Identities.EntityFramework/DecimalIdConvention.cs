using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// <para>
	/// An <see cref="IPropertyAddedConvention"/> that maps decimal-like ID properties by casting them to/from decimal.
	/// </para>
	/// <para>
	/// For a property to match, its name must be *Id or *ID.
	/// Additionally, it must either be of type decimal, or be implicitly convertible <em>to</em> decimal and (implicitly or explicitly) convertible <em>from</em> decimal.
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
			if (!DecimalIdMappingExtensions.IsDecimalConvertible(type))
				return;

			propertyBuilder.HasConverter(typeof(DecimalIdConverter<>).MakeGenericType(type));
			propertyBuilder.HasPrecision(28);
			propertyBuilder.HasScale(0);
		}
	}
}
