using System;
using System.Runtime.CompilerServices;

namespace Architect.Identities.Encodings;

internal static class DecimalExtensions
{
	static DecimalExtensions()
	{
		if (GetSignAndScale(-0.1m) != -2147418112)
			throw new PlatformNotSupportedException("The binary structure of decimals has changed. An updated package version is needed to avoid handling them incorrectly.");
	}

	public static int GetSignAndScale(this decimal value)
	{
		var result = Unsafe.As<decimal, int>(ref value);
		return result;
	}
}
