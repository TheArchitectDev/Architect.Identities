using Xunit;

namespace Architect.Identities.Tests.PublicIdentities
{
	public sealed class PublicIdentityScopeTests
	{
		[Fact]
		public void GetCurrent_WithXUnitAssemblyLoaded_ShouldReturnInstance()
		{
			Assert.NotNull(PublicIdentityScope.Current);
		}

		[Fact]
		public void GetCurrent_WithLocalScope_ShouldReturnExpectedInstance()
		{
			var localConverter = new AesPublicIdentityConverter(new byte[32]);
			using (new PublicIdentityScope(localConverter))
				Assert.Equal(localConverter, PublicIdentityScope.Current.Converter);
		}

		[Fact]
		public void GetCurrent_WithNestedLocalScope_ShouldReturnExpectedInstance()
		{
			var outerConverter = new AesPublicIdentityConverter(new byte[32]);
			using (new PublicIdentityScope(outerConverter))
			{
				var innerConverter = new AesPublicIdentityConverter(new byte[32]);
				using (new PublicIdentityScope(innerConverter))
				{
					var converter = PublicIdentityScope.Current.Converter;
					Assert.Equal(innerConverter, converter);
					Assert.NotEqual(outerConverter, converter);
				}
			}
		}
	}
}
