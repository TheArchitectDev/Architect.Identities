using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Generates 128-bit ID values in a distributed way, with no synchronization between generators.
	/// </summary>
	public interface IDistributedId128Generator
	{
#if NET7_0_OR_GREATER
		/// <summary>
		/// <para>
		/// Returns a new ID value, encoded as a <see cref="UInt128"/>.
		/// </para>
		/// <para>
		/// Note that the numeric (.NET7+ only) and <see cref="Guid"/> creation methods return identical values, encoded in different formats.
		/// Both are incremental.
		/// The two can be freely transcoded to one another using the extension methods provided by <see cref="IdEncodingExtensions"/>.
		/// </para>
		/// </summary>
		UInt128 CreateId();
#endif

		/// <summary>
		/// <para>
		/// Returns a new ID value, encoded as a <see cref="Guid"/>.
		/// </para>
		/// <para>
		/// Note that the numeric (.NET7+ only) and <see cref="Guid"/> creation methods return identical values, encoded in different formats.
		/// Both are incremental.
		/// The two can be freely transcoded to one another using the extension methods provided by <see cref="IdEncodingExtensions"/>.
		/// </para>
		/// </summary>
		Guid CreateGuid();
	}
}
