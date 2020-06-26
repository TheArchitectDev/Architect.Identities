using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		public static string GetAlphanumeric(long id)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			return GetAlphanumeric((ulong)id);
		}

		public static string GetAlphanumeric(ulong id)
		{
			return String.Create(11, id, (charSpan, theId) =>
			{
				Span<byte> byteSpan = stackalloc byte[11];
				GetAlphanumeric(id, byteSpan);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)byteSpan[i];
			});
		}

		public static string GetAlphanumeric(decimal id)
		{
			return String.Create(16, id, (charSpan, theId) =>
			{
				Span<byte> byteSpan = stackalloc byte[16];
				GetAlphanumeric(id, byteSpan);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)byteSpan[i];
			});
		}
	}
}
