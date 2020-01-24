using System;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// Once registered, this type provides static, service-free access to deterministic conversion between local and public IDs.
	/// This allows local IDs to be kept hidden, with public IDs directly based on them, without the bookkeeping that comes with unrelated public IDs.
	/// </summary>
	public static class PublicIdentityConverter
	{
		/// <summary>
		/// A registered default implementation.
		/// Throws if not registered.
		/// </summary>
		public static IPublicIdentityConverter Default => DefaultValue ?? throw new InvalidOperationException(
			$"Register static access to the component using {nameof(PublicIdentityExtensions)}.{nameof(PublicIdentityExtensions.UsePublicIdentities)}.");
		internal static IPublicIdentityConverter? DefaultValue = TestDetector.IsTestRun
			? new AesPublicIdentityConverter(new byte[32]) // Automatically initialized with zero key during test runs
			: null;
	}
}
