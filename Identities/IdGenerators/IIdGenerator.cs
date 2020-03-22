// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public interface IIdGenerator
	{
		/// <summary>
		/// <para>
		/// Returns a new ID value.
		/// </para>
		/// <para>
		/// Note that ID values are generally unsigned, but various components (such as Azure SQL) lack support for unsigned types.
		/// </para>
		/// </summary>
		long CreateId();

		/// <summary>
		/// <para>
		/// Returns a new ID value, as an unsigned type.
		/// </para>
		/// </summary>
		ulong CreateUnsignedId();
	}
}
