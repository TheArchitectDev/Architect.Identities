using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.Identities.EntityFramework
{
	/// <summary>
	/// Mostly similar to <see cref="CastingConverter{TModel, TProvider}"/> with the provider type set to <see cref="Decimal"/>.
	/// Additionally, this type also truncates the provider value before casting it to a model value.
	/// This solves an issue where the SQLite provider introduces an undesirable decimal place.
	/// </summary>
	internal sealed class DecimalIdConverter<TId> : ValueConverter<TId, decimal>
	{
		public DecimalIdConverter()
			: base(
				  convertToProviderExpression: CreateConversionExpression<TId, decimal>(),
				  convertFromProviderExpression: CreateConversionExpression<decimal, TId>())
		{
		}

		private static Expression<Func<TIn, TOut>> CreateConversionExpression<TIn, TOut>()
		{
			var param = Expression.Parameter(typeof(TIn), "value");

			// Truncate decimals before converting them
			Expression value = (typeof(TIn) == typeof(decimal))
				? Expression.Call(typeof(DecimalIdConverter<TId>).GetMethod(nameof(TruncateIfLossless), BindingFlags.Static | BindingFlags.NonPublic)!, param)
				: param;

			var result = Expression.Lambda<Func<TIn, TOut>>(
				Expression.Convert(value, typeof(TOut)),
				param);

			return result;
		}

		/// <summary>
		/// <para>
		/// Returns the truncated input value if its value is equal, or the input value otherwise.
		/// </para>
		/// <para>
		/// This method can be used to remove needless decimal places, such as from 123.0.
		/// SQLite typically causes such values.
		/// </para>
		/// </summary>
		private static decimal TruncateIfLossless(decimal value)
		{
			var result = Decimal.Truncate(value);
			return result == value
				? result
				: value;
		}
	}
}
