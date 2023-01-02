using System;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
#if NET5_0_OR_GREATER
	[System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
	public static class PublicIdentityExtensions
	{
		/// <summary>
		/// <para>
		/// Registers an <see cref="IPublicIdentityConverter"/> implementation, providing deterministic conversion between local and public IDs.
		/// This allows local IDs to be kept hidden, with public IDs directly based on them, without the bookkeeping that comes with unrelated public IDs.
		/// </para>
		/// <para>
		/// Use the options to specify the key.
		/// </para>
		/// <para>
		/// Public IDs are deterministic under the key, but otherwise indistinguishable from random noise.
		/// </para>
		/// <para>
		/// When decoded, public IDs are validated, with a chance of guessing a valid (and even then likely nonexistent) ID of 1/2^64 at best.
		/// </para>
		/// </summary>
		public static IServiceCollection AddPublicIdentities(this IServiceCollection services, Action<Options> identitiesOptions)
		{
			if (identitiesOptions is null) throw new ArgumentNullException(nameof(identitiesOptions));

			var optionsObject = new Options(services);
			identitiesOptions(optionsObject);

			if (optionsObject.Key is null) throw new ArgumentException("Use the options to specify the key.");

			services.AddSingleton<IPublicIdentityConverter>(new AesPublicIdentityConverter(optionsObject.Key));
			return services;
		}

		public sealed class Options
		{
			internal IServiceCollection Services { get; }

			internal byte[]? Key { get; set; }

			internal Options(IServiceCollection services)
			{
				this.Services = services ?? throw new ArgumentNullException(nameof(services));
			}
		}

		/// <summary>
		/// Sets the key used in the conversion between local and public IDs.
		/// </summary>
		public static Options Key(this Options options, string base64Key)
		{
			options.Key = Convert.FromBase64String(base64Key);
			return options;
		}

		/// <summary>
		/// Sets the key used in the conversion between local and public IDs.
		/// </summary>
		public static Options Key(this Options options, ReadOnlySpan<byte> key)
		{
			options.Key = key.ToArray();
			return options;
		}
	}
}
