using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Tries to provide a method to set the precision of a property builder, if one is available.
	/// </summary>
	internal static class PropertyBuilderPrecisionSetter
	{
		public static Func<PropertyBuilder, int, int, PropertyBuilder>? Value { get; } = (Func<PropertyBuilder, int, int, PropertyBuilder>?)
			typeof(PropertyBuilder).GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.SingleOrDefault(method => method.Name == "HasPrecision" &&
					method.GetParameters().Length == 2 && method.GetParameters()[0].ParameterType == typeof(int) && method.GetParameters()[1].ParameterType == typeof(int))
				?.CreateDelegate(typeof(Func<PropertyBuilder, int, int, PropertyBuilder>));
	}
}
