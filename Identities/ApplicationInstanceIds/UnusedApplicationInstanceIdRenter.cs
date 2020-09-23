using System;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// An implementation that does not expect to be invoked.
	/// Invocations throw.
	/// </summary>
	internal sealed class UnusedApplicationInstanceIdRenter : IApplicationInstanceIdRenter
	{
		public ushort RentId()
		{
			throw new NotSupportedException("This implementation did not expected to be invoked.");
		}

		public void ReturnId(ushort id)
		{
			throw new NotSupportedException("This implementation did not expected to be invoked.");
		}
	}
}
