using System;

namespace Architect.Identities.ApplicationInstanceIds
{
	internal sealed class OptionalExceptionHandler : IExceptionHandler
	{
		private Action<Exception>? Handler { get; }

		public OptionalExceptionHandler(Action<Exception>? handler)
		{
			this.Handler = handler;
		}

		public void HandleException(Exception exception)
		{
			this.Handler?.Invoke(exception);
		}
	}
}
