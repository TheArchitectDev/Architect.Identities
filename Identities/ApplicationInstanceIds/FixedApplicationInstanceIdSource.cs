using System;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// An <see cref="IApplicationInstanceIdSource"/> implementation that uses a fixed value.
	/// </summary>
	public sealed class FixedApplicationInstanceIdSource : IApplicationInstanceIdSource
	{
		public ushort ApplicationInstanceId { get; }

		/// <summary>
		/// Constructs a new instance with the given value.
		/// </summary>
		public FixedApplicationInstanceIdSource(ushort applicationInstanceId)
			: this(hostEnvironment: null, applicationInstanceId)
		{
		}

		/// <summary>
		/// Constructs a new instance with the given value.
		/// </summary>
		/// <param name="hostEnvironment">If given, if the environment is Production and the ID value is 0, this method throws an exception.</param>
		/// <param name="applicationInstanceId">The fixed value to use.</param>
		public FixedApplicationInstanceIdSource(IHostEnvironment? hostEnvironment, ushort applicationInstanceId)
		{
			// Refuse ID 0 in production
			if (applicationInstanceId == 0 && hostEnvironment?.IsProduction() == false)
				throw new Exception($"{nameof(FixedApplicationInstanceIdSource)} was constructed with ID 0, which should be avoided in production.");
			
			this.ApplicationInstanceId = applicationInstanceId;
		}
	}
}
