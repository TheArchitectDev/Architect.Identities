using Xunit;

namespace Architect.Identities.EntityFramework.IntegrationTests
{
	public sealed class PropertyBuilderPrecisionSetterTests
	{
		[Fact]
		public void Value_WithEfCore5_ShouldHaveValue()
		{
			Assert.NotNull(PropertyBuilderPrecisionSetter.Value);
		}
	}
}
