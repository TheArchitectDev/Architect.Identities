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
			var string1 = host1.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicString(0UL);

			var hostBuilder2 = new HostBuilder();
			hostBuilder2.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key2)));
			using var host2 = hostBuilder2.Build();
			var string2 = host2.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicString(0UL);

			Assert.Equal(string1, string2); // A second custom key gives the same result as an identical first custom key
		}

		[Fact]
		public void AddPublicIdentities_WithKey_ShouldGenerateResultBasedOnThatKey()
		{
			var key1 = new byte[32];
			key1[0] = 1;
			var key2 = new byte[32];
			key2[31] = 2;

			var string0 = PublicIdentityScope.Current.Converter.GetPublicString(0UL);

			var hostBuilder1 = new HostBuilder();
			hostBuilder1.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key1)));
			using var host1 = hostBuilder1.Build();
			var string1 = host1.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicString(0UL);

			var hostBuilder2 = new HostBuilder();
			hostBuilder2.ConfigureServices(services => services.AddPublicIdentities(identities => identities.Key(key2)));
			using var host2 = hostBuilder2.Build();
			var string2 = host2.Services.GetRequiredService<IPublicIdentityConverter>().GetPublicString(0UL);

			Assert.NotEqual(string0, string1); // A custom key gives a different result than the default unit test key
			Assert.NotEqual(string1, string2); // A second custom key gives a different result than the first custom key
		}
	}
}
