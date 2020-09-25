using System;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// Handles exceptions thrown by <see cref="IApplicationInstanceIdRenter"/>-related types.
	/// </summary>
	internal interface IExceptionHandler
	{
		void HandleException(Exception exception);
	}
}
