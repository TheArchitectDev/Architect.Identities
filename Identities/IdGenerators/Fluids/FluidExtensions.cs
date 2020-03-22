using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static class FluidExtensions
	{
		#region Registration

		/// <summary>
		/// Registers the FluidIdGenerator, as both IIdGenerator and as itself.
		/// </summary>
		public static IdGeneratorExtensions.Options UseFluid(this IdGeneratorExtensions.Options options,
			Action<Options>? fluidOptions = null)
		{
			var optionsObject = new Options();
			fluidOptions?.Invoke(optionsObject);

			options.Services.AddSingleton(serviceProvider => CreateFluidIdGenerator(serviceProvider, optionsObject));
			options.Services.AddSingleton<IIdGenerator>(serviceProvider => serviceProvider.GetRequiredService<FluidIdGenerator>());

			return options;
		}

		private static FluidIdGenerator CreateFluidIdGenerator(IServiceProvider serviceProvider, Options options)
		{
			var epoch = options.Epoch;
			var bitDistribution = options.BitDistribution;

			var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
			var applicationInstanceIdSource = serviceProvider.GetRequiredService<IApplicationInstanceIdSource>();

			var applicationInstanceId = applicationInstanceIdSource.ContextUniqueApplicationInstanceId.Value; // Might throw if the app instance ID source is unreachable

			// Throw on value 0 in production
			if (applicationInstanceId == 0 && hostEnvironment.IsProduction())
				throw new Exception($"{nameof(IApplicationInstanceIdSource)} {applicationInstanceIdSource} provided invalid production value {applicationInstanceId}.");

			// Throw if the app instance ID is too great for our bit distribution
			if (applicationInstanceId > bitDistribution.MaxApplicationInstanceId)
				throw new Exception($"{nameof(IApplicationInstanceIdSource)} {applicationInstanceIdSource} provided value {applicationInstanceId}, but the current bit distribution allows a maximum of {bitDistribution.MaxApplicationInstanceId}.");

			var instance = new FluidIdGenerator(hostEnvironment.IsProduction(), FluidIdGenerator.GetUtcNow, applicationInstanceId, epoch, bitDistribution);

			return instance;
		}

		#endregion

		#region Options

		public sealed class Options
		{
			internal DateTime Epoch { get; set; } = FluidIdGenerator.DefaultEpoch;
			internal FluidBitDistribution BitDistribution { get; set; } = FluidBitDistribution.Default;

			internal Options()
			{
			}
		}

		/// <summary>
		/// <para>
		/// Configures the epoch from which to offset the timestamp component. The default value is 2020-01-01.
		/// </para>
		/// <para>
		/// A higher setting allows a given number of bits to represent timestamps further into the future.
		/// </para>
		/// <para>
		/// Must be in UTC.
		/// </para>
		/// </summary>
		public static Options Epoch(this Options options,
			DateTime epoch)
		{
			options.Epoch = epoch;
			return options;
		}

		/// <summary>
		/// <para>
		/// Configures how the bits in a Fluid are distributed between timestamp, application instance identifier, and counter.
		/// </para>
		/// <para>
		/// The sum of the values must equal 63 or 64.
		/// For databases that cannot store unsigned 64-bit integers, i.e. that must use the type long instead of ulong, only 63 bits can be used effectively.
		/// </para>
		/// </summary>
		/// <param name="timestampBitCount">Default 43. The number of bits dedicated to the ID's creation timestamp in milliseconds since the configured epoch.</param>
		/// <param name="applicationInstanceIdBitCount">Default 11. The number of bits dedicated to a unique identifier for the application instance, i.e. a particular instance of a particular application.</param>
		/// <param name="counterBitCount">Default 10. The number of bits dedicated to a counter value that increments for IDs created on the same millisecond.</param>
		public static Options BitDistribution(this Options options,
			byte timestampBitCount, byte applicationInstanceIdBitCount, byte counterBitCount)
		{
			options.BitDistribution = new FluidBitDistribution(timestampBitCount, applicationInstanceIdBitCount, counterBitCount);
			return options;
		}

		#endregion
	}
}
