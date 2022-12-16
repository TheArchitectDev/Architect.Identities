using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Architect.Identities.Encodings
{
	/// <summary>
	/// Provides operations related to the binary layout of decimals.
	/// </summary>
	internal static class DecimalStructure
	{
		/// <summary>
		/// Throws if the binary layout of decimals is different then expected, i.e. if it has changed for the current runtime since the time of writing.
		/// </summary>
		public static void ThrowIfDecimalStructureIsUnexpected()
		{
			// Fill a decimal's bits according to its current structure (yes, decimal composition is weird)
			Span<decimal> decimals = stackalloc decimal[1];
			var ints = MemoryMarshal.Cast<decimal, int>(decimals);
			ints[0] = 0; // Sign and scale
			ints[1] = 3; // Hi
			ints[2] = 1; // Lo
			ints[3] = 2; // Mid

			// Confirm that it interprets them as expected
			var components = Decimal.GetBits(decimals[0]); // Lo, mid, hi, sign-and-scale
			if (components[0] != 1 ||
				components[1] != 2 ||
				components[2] != 3 ||
				components[3] != 0)
			{
				throw new NotSupportedException("The binary structure of decimals has changed. An updated package version is needed to avoid handling them incorrectly.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetSignAndScale(Span<int> decimalComponents) => decimalComponents[0];
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetSignAndScale(ReadOnlySpan<int> decimalComponents) => decimalComponents[0];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLo(Span<int> decimalComponents) => decimalComponents[2];
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLo(ReadOnlySpan<int> decimalComponents) => decimalComponents[2];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetMid(Span<int> decimalComponents) => decimalComponents[3];
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetMid(ReadOnlySpan<int> decimalComponents) => decimalComponents[3];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetHi(Span<int> decimalComponents) => decimalComponents[1];
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetHi(ReadOnlySpan<int> decimalComponents) => decimalComponents[1];
	}
}
