using Xunit;

namespace Architect.Identities.Tests.PublicIdentities
{
	public sealed class PublicIdentityConverterTests
	{
		[Fact]
		public void GetDefault_WithXUnitAssemblyLoaded_ShouldReturnInstance()
		{
			Assert.NotNull(PublicIdentityConverter.Default);
		}
	}
}
