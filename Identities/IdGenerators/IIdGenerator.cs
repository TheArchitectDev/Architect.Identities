// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public interface IIdGenerator
	{
		/// <summary>
		/// Returns a new unsigned ID value.
		/// </summary>
		ulong CreateId();

		/// <summary>
		/// Returns a new signed ID value.
		/// </summary>
		long CreateSignedId();
	}
}
