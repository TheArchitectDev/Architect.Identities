// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public interface IIdGenerator
	{
		/// <summary>
		/// <para>
		/// Returns a new signed ID value.
		/// </para>
		/// <para>
		/// Note that ID values are generally unsigned, but various components (such as Azure SQL) lack support for unsigned types.
		/// </para>
		/// </summary>
		long CreateId();

		/// <summary>
		/// <para>
		/// Returns a new unsigned ID value.
		/// </para>
		/// </summary>
		ulong CreateUnsignedId();
	}
}
