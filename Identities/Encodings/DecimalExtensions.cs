using System;
using System.Runtime.CompilerServices;

namespace Architect.Identities.Encodings;

internal static class DecimalExtensions
{
	static DecimalExtensions()
	{
		// Example value 0.1m has flags 0x80010000 (sign bit + scale 1)
		// Flags is the first 4 bytes of decimal's binary structure on every supported platform
		if (GetSignAndScale(-0.1m) != unchecked((int)0x80010000))
			throw new PlatformNotSupportedException("The binary structure of decimals has changed. An updated package version is needed to avoid handling them incorrectly.");
	}

	public static int GetSignAndScale(this decimal value)
	{
		var result = Unsafe.As<decimal, int>(ref value);
		return result;
	}
}
