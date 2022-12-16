using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Architect.Identities.Encodings;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// Converts identities to public identities using AES-ECB.
	/// </para>
	/// <para>
	/// For optimal performance, singleton or pooled use is preferred.
	/// </para>
	/// </summary>
#if NET5_0_OR_GREATER
	[System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
	internal sealed class AesPublicIdentityConverter : IPublicIdentityConverter
	{
		static AesPublicIdentityConverter()
		{
			if (!BitConverter.IsLittleEndian)
				throw new PlatformNotSupportedException($"{nameof(IPublicIdentityConverter)} is not supported on big-endian architectures, to avoid issues with the portability of values between architectures.");

			// Ensure that decimals are still structured the same way
			// This prevents the application from ever generating incorrect public identities in this extremely unlikely scenario, allowing a fix to be created
			DecimalStructure.ThrowIfDecimalStructureIsUnexpected();
		}

		#region Byte arrays to store temporary state for ICryptoTransform parameters
		private byte[] EncryptorInputBlock { get; } = new byte[16];
		private byte[] EncryptorOutputBlock { get; } = new byte[16];
		private Span<ulong> EncryptorInputUlongSpan => MemoryMarshal.Cast<byte, ulong>(this.EncryptorInputBlock);
		private Span<decimal> EncryptorInputDecimalSpan => MemoryMarshal.Cast<byte, decimal>(this.EncryptorInputBlock);
		private Span<int> EncryptorInputDecimalComponentSpan => MemoryMarshal.Cast<byte, int>(this.EncryptorInputBlock);
		private byte[] DecryptorInputBlock { get; } = new byte[16];
		private byte[] DecryptorOutputBlock { get; } = new byte[16];
		private Span<ulong> DecryptorOutputUlongSpan => MemoryMarshal.Cast<byte, ulong>(this.DecryptorOutputBlock);
		private Span<decimal> DecryptorOutputDecimalSpan => MemoryMarshal.Cast<byte, decimal>(this.DecryptorOutputBlock);
		#endregion

		private byte[] Key { get; }
		private Aes Aes { get; }
		private ICryptoTransform Encryptor { get; }
		private ICryptoTransform Decryptor { get; }

		public AesPublicIdentityConverter(ReadOnlySpan<byte> aesKey)
		{
			if (aesKey.Length < 16) throw new ArgumentException("Expected at least a 128-bit key.");

			this.Key = aesKey.ToArray();
			this.Aes = new AesManaged() // Single-block crypto is faster managed than unmanaged
			{
				Key = Key,
				Mode = CipherMode.ECB,
				Padding = PaddingMode.None, // Required for correct results
				IV = new byte[16], // Not used with ECB, but set to zero anyway
			};
			this.Encryptor = this.Aes.CreateEncryptor() ?? throw new ArgumentException($"{this.Aes} produced a null encryptor.");
			this.Decryptor = this.Aes.CreateDecryptor() ?? throw new ArgumentException($"{this.Aes} produced a null decryptor.");
		}

		public void Dispose()
		{
			this.Encryptor.Dispose();
			this.Decryptor.Dispose();
			this.Aes.Dispose();
		}

		/// <summary>
		/// Converts the given long to ulong, or throws an <see cref="ArgumentOutOfRangeException"/> if it is negative.
		/// </summary>
		private static ulong LongToUlong(long id) => id >= 0 ? (ulong)id : throw new ArgumentOutOfRangeException(nameof(id));

		public Guid GetPublicRepresentation(ulong id)
		{
			Span<byte> outputBytes = stackalloc byte[16];
			this.WriteBytes(id, outputBytes);

			var publicId = new Guid(outputBytes);
			return publicId;
		}

		public Guid GetPublicRepresentation(decimal id)
		{
			Span<byte> outputBytes = stackalloc byte[16];
			this.WriteBytes(id, outputBytes);

			var publicId = new Guid(outputBytes);
			return publicId;
		}

		public bool TryGetUlong(Guid publicId, out ulong id)
		{
			Span<byte> idBytes = stackalloc byte[16];
			var idUlongs = MemoryMarshal.Cast<byte, ulong>(idBytes);

			if (!this.TryGetIdBytes(publicId, idBytes) || idUlongs[0] != 0UL) // Invalid input if the left 8 bytes contain any non-zeros
			{
				id = default;
				return false;
			}
			id = idUlongs[1];
			return true;
		}

		public bool TryGetDecimal(Guid publicId, out decimal id)
		{
			Span<byte> idBytes = stackalloc byte[16];
			var decimals = MemoryMarshal.Cast<byte, decimal>(idBytes);
			var decimalComponents = MemoryMarshal.Cast<byte, int>(idBytes);

			// Invalid input if sign-and-scale component (4 bytes) are non-zero or max value is exceeded
			if (!this.TryGetIdBytes(publicId, idBytes) || DecimalStructure.GetSignAndScale(decimalComponents) != 0 || (id = decimals[0]) > DistributedIdGenerator.MaxValue)
			{
				id = default;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Implementation that writes to a span.
		/// </summary>
		private void WriteBytes(ulong id, Span<byte> outputBytes)
		{
			System.Diagnostics.Debug.Assert(outputBytes.Length == 16);

			lock (this.Encryptor)
			{
				this.EncryptorInputUlongSpan[1] = id;

				// The first 8 bytes are always zero, and the last 8 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(this.EncryptorInputUlongSpan[0] == 0UL);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock) == 0, "The left 8 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock.AsSpan()[8..]) == id); // Confirm reversible operation

				var byteCount = this.Encryptor.TransformBlock(this.EncryptorInputBlock, 0, 16, this.EncryptorOutputBlock, 0);
				System.Diagnostics.Debug.Assert(byteCount == 16);

				// Copy the bytes over so that we can release the lock
				this.EncryptorOutputBlock.CopyTo(outputBytes);
			}
		}

		/// <summary>
		/// Implementation that writes to a span.
		/// </summary>
		private void WriteBytes(decimal id, Span<byte> outputBytes)
		{
			System.Diagnostics.Debug.Assert(outputBytes.Length == 16);

			if (id < 0m) throw new ArgumentOutOfRangeException(nameof(id));

			lock (this.Encryptor)
			{
				this.EncryptorInputDecimalSpan[0] = id;

				if (id > DistributedIdGenerator.MaxValue || DecimalStructure.GetSignAndScale(this.EncryptorInputDecimalComponentSpan) != 0)
					throw new ArgumentException($"The ID must be positive, have no decimal places, and consist of no more than 28 digits.", nameof(id));

				// The first 4 bytes are always zero, and the last 12 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(this.EncryptorInputDecimalComponentSpan[0] == 0);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<int>(this.EncryptorInputBlock) == 0, "The left 4 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(this.EncryptorInputDecimalSpan[0] == id); // Confirm reversible operation

				var byteCount = this.Encryptor.TransformBlock(this.EncryptorInputBlock, 0, 16, this.EncryptorOutputBlock, 0);
				System.Diagnostics.Debug.Assert(byteCount == 16);

				// Copy the bytes over so that we can release the lock
				this.EncryptorOutputBlock.CopyTo(outputBytes);
			}
		}

		/// <summary>
		/// <para>
		/// Decrypts the given public ID, writing the result into the given output span, without checking if it is valid.
		/// </para>
		/// </summary>
		private bool TryGetIdBytes(Guid publicId, Span<byte> outputBytes)
		{
			System.Diagnostics.Debug.Assert(outputBytes.Length == 16);

			// Abuse the output bytes as input space
			if (!publicId.TryWriteBytes(outputBytes)) return false;

			// Decrypt
			lock (this.Decryptor)
			{
				System.Diagnostics.Debug.Assert(this.DecryptorInputBlock.Length == 16);

				outputBytes.CopyTo(this.DecryptorInputBlock);
				this.Decryptor.TransformBlock(this.DecryptorInputBlock, 0, 16, this.DecryptorOutputBlock, 0);

				this.DecryptorOutputBlock.CopyTo(outputBytes);
			}

			return true;
		}
	}
}
