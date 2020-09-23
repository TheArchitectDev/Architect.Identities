namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// This is an internal API that should not be used by application code.
	/// </summary>
	public interface IApplicationInstanceIdRenter
	{
		// #TODO: Make uint?
		/// <summary>
		/// This is an internal API that should not be used by application code.
		/// </summary>
		ushort RentId();

		/// <summary>
		/// This is an internal API that should not be used by application code.
		/// </summary>
		void ReturnId(ushort id);
	}
}
