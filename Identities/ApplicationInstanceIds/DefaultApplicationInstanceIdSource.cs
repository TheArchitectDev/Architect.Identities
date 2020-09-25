using System;

namespace Architect.Identities.ApplicationInstanceIds
{
	/// <summary>
	/// The default implementation.
	/// Only when this is used, <see cref="ApplicationInstanceIdSourceExtensions.UseApplicationInstanceIdSource(IServiceProvider)"/> sets the <see cref="ApplicationInstanceId"/> value.
	/// </summary>
	internal sealed class DefaultApplicationInstanceIdSource : IApplicationInstanceIdSource
	{
		public ushort ApplicationInstanceId => this._applicationInstanceId ?? ThrowNotAcquired();
		internal ushort? _applicationInstanceId;

		public static ushort ThrowNotAcquired() => throw new Exception(
			$"No application instance ID was acquired. Call {nameof(ApplicationInstanceIdSourceExtensions.UseApplicationInstanceIdSource)}() on startup.");

		internal void SetApplicationInstanceId(ushort applicationInstanceId)
		{
			if (this._applicationInstanceId != null)
				throw new InvalidOperationException("An application instance ID was already configured.");

			this._applicationInstanceId = applicationInstanceId;
		}
	}
}
