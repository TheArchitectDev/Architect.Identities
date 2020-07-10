using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Architect.Identities.Tests.PublicIdentities
{
	public sealed class PublicIdentityExtensionsTests
	{
		[Fact]
		public void AddPublicIdentities_WithSameKey_ShouldGenerateSameResults()
		{
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key1.AsSpan().CopyTo(key2);

			var hostBuilder1 = new HostBuilder();
			hostBuilder1.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key1)));
			using var host1 = hostBuilder1.Build();
			var guid1 = host1.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicRepresentation(0UL);

			var hostBuilder2 = new HostBuilder();
			hostBuilder2.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key2)));
			using var host2 = hostBuilder2.Build();
			var guid2 = host2.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicRepresentation(0UL);

			Assert.Equal(guid1, guid2); // A second custom key gives the same result as an identical first custom key
		}

		[Fact]
		public void AddPublicIdentities_WithKey_ShouldGenerateResultBasedOnThatKey()
		{
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key2[31] = 2;

			var guid0 = PublicIdentityScope.Current.Converter.GetPublicRepresentation(0UL);

			var hostBuilder1 = new HostBuilder();
			hostBuilder1.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key1)));
			using var host1 = hostBuilder1.Build();
			var guid1 = host1.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicRepresentation(0UL);

			var hostBuilder2 = new HostBuilder();
			hostBuilder2.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key2)));
			using var host2 = hostBuilder2.Build();
			var guid2 = host2.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicRepresentation(0UL);

			Assert.NotEqual(guid0, guid1); // A custom key gives a different result than the default unit test key
			Assert.NotEqual(guid1, guid2); // A second custom key gives a different result than the first custom key
		}
	}
}
