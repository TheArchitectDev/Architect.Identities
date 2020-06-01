using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A <see cref="Guid"/> replacement that provides values that are unique company-wide.
	/// </para>
	/// <para>
	/// Like <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
	/// </para>
	/// <para>
	/// The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist, as DECIMAL(28, 0).
	/// </para>
	/// <para>
	/// Note that the values expose their creation timestamp. This may be sensitive data in some contexts.
	/// </para>
	/// <para>
	/// The incremental property makes values much more efficient for use as primary keys in databases than random <see cref="Guid"/> values.
	/// (There are acceptable minor fluctuations of the incremental property, caused by distributed systems, clock rewinds, and (with extremely low probability) bulk operations.)
	/// </para>
	/// <para>
	/// The values are decimals of up to 28 digits, with 0 decimal places. In many SQL databases, the corresponding type is DECIMAL(28, 0).
	/// In [Azure] SQL Server and MySQL, this takes 13 bytes of storage, making it about 20% more compact than a <see cref="Guid"/>.
	/// </para>
	/// <para>
	/// The decimal type has broad support and predictable implementations, unlike the <see cref="Guid"/> type.
	/// For example, SQL Server sorts <see cref="Guid"/> values in an unfavorable way, treating some of the middle bytes as the most significant.
	/// MySQL has no <see cref="Guid"/> type, making manual queries cumbersome.
	/// Decimals avoid such issues.
	/// </para>
	/// <para>
	/// There is a rate limit of 1000 values generated per millisecond (i.e. 1M per second), with threads sleeping to maintain the limit.
	/// </para>
	/// <para>
	/// Collisions between generated values are extremely unlikely. They may happen worldwide, but they are not expected within a single organization.
	/// If 24K values were generated in a single millisecond, the probability of a collision would be less than 1/1M, or 1 collision per 500 billion values generated.
	/// Realistically, the values will be spread out across many more milliseconds, making the probability drastically lower.
	/// </para>
	/// </summary>
	public static class CompanyUniqueId
	{
		private static CompanyUniqueIdGenerator Generator { get; } = new CompanyUniqueIdGenerator();

		/// <summary>
		/// <para>
		/// Returns a new ID value of up to 28 decimal digits, with no decimal places.
		/// </para>
		/// <para>
		/// Like <see cref="Guid"/> values, created values are hard to guess and extremely unlikely to collide.
		/// </para>
		/// <para>
		/// The values have the added benefit of being incremental, intuitive to display and to sort, and slightly more compact to persist, as DECIMAL(28, 0).
		/// </para>
		/// <para>
		/// Note that the values expose their creation timestamp. This may be sensitive data in some contexts.
		/// </para>
		/// </summary>
		public static decimal CreateId()
		{
			return Generator.CreateId();
		}

		/// <summary>
		/// <para>
		/// Outputs a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value or if the output span is too short.
		/// </para>
		/// </summary>
		/// <param name="id">A value generated using <see cref="CreateId"/>.</param>
		/// <param name="bytes">At least 16 bytes, to write the alphanumeric representation to.</param>
		public static void ToShortString(decimal id, Span<byte> bytes)
		{
			CompanyUniqueIdEncoder.ToShortString(id, bytes);
		}

		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="id">A value generated using <see cref="CreateId"/>.</param>
		public static string ToShortString(decimal id)
		{
			return CompanyUniqueIdEncoder.ToShortString(id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric short string representation.
		/// </para>
		/// <para>
		/// Returns false if the input span is too short, if it does not contain a properly encoded value, or if the encoded value does not represent a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="bytes">Input bytes, the first 16 of which are read.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryFromShortString(ReadOnlySpan<byte> bytes, out decimal id)
		{
			return CompanyUniqueIdEncoder.TryFromShortString(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given alphanumeric short string representation.
		/// </para>
		/// <para>
		/// Returns false if the input span is too short, if it does not contain a properly encoded value, or if the encoded value does not represent a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="chars">Input chars, the first 16 of which are read.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryFromShortString(ReadOnlySpan<char> chars, out decimal id)
		{
			return CompanyUniqueIdEncoder.TryFromShortString(chars, out id);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric short string representation.
		/// </para>
		/// <para>
		/// Returns the default value if the input span is too short, if it does not contain a properly encoded value, or if the encoded value does not represent a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="bytes">Input bytes, the first 16 of which are read.</param>
		public static decimal FromShortStringOrDefault(ReadOnlySpan<byte> bytes)
		{
			return CompanyUniqueIdEncoder.FromShortStringOrDefault(bytes);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given alphanumeric short string representation.
		/// </para>
		/// <para>
		/// Returns the default value if the input span is too short, if it does not contain a properly encoded value, or if the encoded value does not represent a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="chars">Input chars, the first 16 of which are read.</param>
		public static decimal FromShortStringOrDefault(ReadOnlySpan<char> chars)
		{
			return CompanyUniqueIdEncoder.FromShortStringOrDefault(chars);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given string representation.
		/// Both the short form (such as from <see cref="ToShortString(decimal)"/>) and regular decimal strings (from <see cref="Decimal.ToString()"/>) are supported.
		/// </para>
		/// <para>
		/// Returns false if the input is not either of the supported encodings of a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="bytes">Input bytes, the first 16 of which are read.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryFromString(ReadOnlySpan<byte> bytes, out decimal id)
		{
			return CompanyUniqueIdEncoder.TryFromString(bytes, out id);
		}

		/// <summary>
		/// <para>
		/// Outputs an ID decoded from the given string representation.
		/// Both the short form (such as from <see cref="ToShortString(decimal)"/>) and regular decimal strings (from <see cref="Decimal.ToString()"/>) are supported.
		/// </para>
		/// <para>
		/// Returns false if the input is not either of the supported encodings of a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="chars">Input chars, the first 16 of which are read.</param>
		/// <param name="id">On true, this outputs the decoded ID.</param>
		public static bool TryFromString(ReadOnlySpan<char> chars, out decimal id)
		{
			return CompanyUniqueIdEncoder.TryFromString(chars, out id);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given string representation.
		/// Both the short form (such as from <see cref="ToShortString(decimal)"/>) and regular decimal strings (from <see cref="Decimal.ToString()"/>) are supported.
		/// </para>
		/// <para>
		/// Returns the default value if the input is not either of the supported encodings of a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="bytes">Input bytes, the first 16 of which are read.</param>
		public static decimal FromStringOrDefault(ReadOnlySpan<byte> bytes)
		{
			return CompanyUniqueIdEncoder.FromStringOrDefault(bytes);
		}

		/// <summary>
		/// <para>
		/// Returns an ID decoded from the given string representation.
		/// Both the short form (such as from <see cref="ToShortString(decimal)"/>) and regular decimal strings (from <see cref="Decimal.ToString()"/>) are supported.
		/// </para>
		/// <para>
		/// Returns the default value if the input is not either of the supported encodings of a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="chars">Input chars, the first 16 of which are read.</param>
		public static decimal FromStringOrDefault(ReadOnlySpan<char> chars)
		{
			return CompanyUniqueIdEncoder.FromStringOrDefault(chars);
		}
	}
}
