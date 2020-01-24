using System;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed class FixedApplicationInstanceIdSource : IApplicationInstanceIdSource
	{
		public Lazy<ushort> ContextUniqueApplicationInstanceId { get; }

		public FixedApplicationInstanceIdSource(IHostEnvironment hostEnvironment, ushort applicationInstanceId)
		{
			if (hostEnvironment is null) throw new ArgumentNullException(nameof(hostEnvironment));

			// Refuse id 0 in production
			if (applicationInstanceId == 0 && hostEnvironment.IsProduction())
				throw new Exception($"{nameof(FixedApplicationInstanceIdSource)} was constructed with id 0, which should be avoided in production.");
			
			this.ContextUniqueApplicationInstanceId = new Lazy<ushort>(() => applicationInstanceId);
		}
	}
}
