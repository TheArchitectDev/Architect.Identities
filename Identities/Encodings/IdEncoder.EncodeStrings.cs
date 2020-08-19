using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	public static partial class IdEncoder
	{
		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The positive ID to encode.</param>
		public static string GetAlphanumeric(long id)
		{
			if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));

			return GetAlphanumeric((ulong)id);
		}

		/// <summary>
		/// <para>
		/// Returns an 11-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">The ID to encode.</param>
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

		/// <summary>
		/// <para>
		/// Returns a 16-character alphanumeric string representation of the given ID.
		/// </para>
		/// <para>
		/// Throws if the input is not a proper ID value.
		/// </para>
		/// </summary>
		/// <param name="id">A positive decimal with 0 decimal places, consisting of no more than 28 digits, such as a value generated using <see cref="DistributedId.CreateId"/>.</param>
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

		/// <summary>
		/// <para>
		/// Returns a 22-character alphanumeric string representation of the given ID.
		/// </para>
		/// </summary>
		/// <param name="id">Any sequence of bytes stored in a <see cref="Guid"/>.</param>
		public static string GetAlphanumeric(Guid id)
		{
			return String.Create(22, id, (charSpan, theId) =>
			{
				Span<byte> byteSpan = stackalloc byte[22];
				GetAlphanumeric(id, byteSpan);
				for (var i = 0; i < charSpan.Length; i++)
					charSpan[i] = (char)byteSpan[i];
			});
		}
	}
}
