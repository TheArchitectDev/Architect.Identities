using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Architect.Identities.Encodings;

#pragma warning disable IDE0130 // Namespace does not match folder structure
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
	internal sealed class AesPublicIdentityConverter : IPublicIdentityConverter
	{
		#region Byte arrays to store temporary state for ICryptoTransform parameters
		private byte[] EncryptorInputBlock { get; } = new byte[16];
		private byte[] EncryptorOutputBlock { get; } = new byte[16];
		private byte[] DecryptorInputBlock { get; } = new byte[16];
		private byte[] DecryptorOutputBlock { get; } = new byte[16];
		#endregion

		private byte[] Key { get; }
		private Aes Aes { get; }
		private ICryptoTransform Encryptor { get; }
		private ICryptoTransform Decryptor { get; }

		public AesPublicIdentityConverter(ReadOnlySpan<byte> aesKey)
		{
			if (aesKey.Length < 16) throw new ArgumentException("Expected at least a 128-bit key.");

			this.Key = aesKey.ToArray();

			this.Aes = Aes.Create();
			this.Aes.Key = this.Key;
			this.Aes.Mode = CipherMode.ECB;
			this.Aes.Padding = PaddingMode.None; // Required for correct results
			this.Aes.IV = new byte[16]; // Not used with ECB, but set to zero anyway

			this.Encryptor = this.Aes.CreateEncryptor() ?? throw new ArgumentException($"{this.Aes} produced a null encryptor.");
			this.Decryptor = this.Aes.CreateDecryptor() ?? throw new ArgumentException($"{this.Aes} produced a null decryptor.");
		}

		public void Dispose()
		{
			this.Encryptor.Dispose();
			this.Decryptor.Dispose();
			this.Aes.Dispose();
		}

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

		public Guid GetPublicRepresentation(UInt128 id)
		{
			// Since this package supports transcoding between UInt128 and Guid, it is desirable for the two to result in the same public representation
			// As such, we piggyback on the Guid overload

			Span<byte> idBytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, idBytes);
			BinaryIdEncoder.TryDecodeGuid(idBytes, out var guid);

			var result = this.GetPublicRepresentation(guid);
			return result;
		}

		public Guid GetPublicRepresentation(Guid id)
		{
			// To maintain the option of working based on UInt128 in the future, put the bytes in big-endian order first
			Span<byte> idBytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(id, idBytes);

			Span<byte> outputBytes = stackalloc byte[16];
			this.WriteBytes(idBytes, outputBytes);

			var publicId = new Guid(outputBytes);
			return publicId;
		}

		public bool TryGetUlong(Guid publicId, out ulong id)
		{
			Span<byte> idBytes = stackalloc byte[16];

			if (!this.TryGetIdBytes(publicId, idBytes) || Unsafe.ReadUnaligned<ulong>(ref idBytes[0]) != 0UL) // Invalid input if the left 8 bytes contain any non-zeros
			{
				id = default;
				return false;
			}
			id = BinaryPrimitives.ReadUInt64LittleEndian(idBytes[8..]);
			return true;
		}

		public bool TryGetDecimal(Guid publicId, out decimal id)
		{
			Span<byte> idBytes = stackalloc byte[16];

			if (!this.TryGetIdBytes(publicId, idBytes))
			{
				id = default;
				return false;
			}

			// Little-endian decimal layout because that is what was initially done
			var signAndScale = BinaryPrimitives.ReadInt32LittleEndian(idBytes);
			var hi = BinaryPrimitives.ReadInt32LittleEndian(idBytes[4..]);
			var lo = BinaryPrimitives.ReadInt32LittleEndian(idBytes[8..]);
			var mid = BinaryPrimitives.ReadInt32LittleEndian(idBytes[12..]);

			id = new decimal(lo: lo, mid: mid, hi: hi, isNegative: false, scale: 0);

			// Invalid input if sign-and-scale component (4 bytes) are non-zero or max value is exceeded
			if (id > DistributedIdGenerator.MaxValue || signAndScale != 0)
			{
				id = default;
				return false;
			}

			return true;
		}

		public bool TryGetUInt128(Guid publicId, out UInt128 id)
		{
			// Since this package supports transcoding between UInt128 and Guid, it is desirable for the two to result in the same public representation
			// As such, we piggyback on the Guid overload

			if (!this.TryGetGuid(publicId, out var guid))
			{
				id = default;
				return false;
			}

			Span<byte> idBytes = stackalloc byte[16];
			BinaryIdEncoder.Encode(guid, idBytes);
			BinaryIdEncoder.TryDecodeUInt128(idBytes, out id);
			return true;
		}

		public bool TryGetGuid(Guid publicId, out Guid id)
		{
			Span<byte> idBytes = stackalloc byte[16];

			if (!this.TryGetIdBytes(publicId, idBytes) || !BinaryIdEncoder.TryDecodeGuid(idBytes, out id))
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
				Unsafe.WriteUnaligned(ref this.EncryptorInputBlock[0], 0UL);
				BinaryPrimitives.WriteUInt64LittleEndian(this.EncryptorInputBlock.AsSpan()[8..], id);

				// The first 8 bytes are always zero, and the last 8 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(BinaryPrimitives.ReadUInt64LittleEndian(this.EncryptorInputBlock) == 0, "The left 8 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(BinaryPrimitives.ReadUInt64LittleEndian(this.EncryptorInputBlock.AsSpan()[8..]) == id); // Confirm reversible operation

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
				// Little-endian decimal layout because that is what was initially done
				Span<int> decimalComponents = stackalloc int[4];
				Decimal.GetBits(id, decimalComponents);
				Unsafe.WriteUnaligned(ref outputBytes[0], 0U); // Flags
				BinaryPrimitives.WriteInt32LittleEndian(outputBytes[4..], decimalComponents[2]); // Hi
				BinaryPrimitives.WriteInt32LittleEndian(outputBytes[8..], decimalComponents[0]); // Lo
				BinaryPrimitives.WriteInt32LittleEndian(outputBytes[12..], decimalComponents[1]); // Mid

				if (id > DistributedIdGenerator.MaxValue || id.GetSignAndScale() != 0)
					throw new ArgumentException($"The ID must be positive, have no decimal places, and consist of no more than 28 digits.", nameof(id));

				outputBytes.CopyTo(this.EncryptorInputBlock);

				// The first 4 bytes are always zero, and the last 12 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<decimal>(this.EncryptorInputBlock).GetSignAndScale() == 0);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<int>(this.EncryptorInputBlock) == 0, "The left 4 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<decimal>(this.EncryptorInputBlock) == id); // Confirm reversible operation

				var byteCount = this.Encryptor.TransformBlock(this.EncryptorInputBlock, 0, 16, this.EncryptorOutputBlock, 0);
				System.Diagnostics.Debug.Assert(byteCount == 16);

				// Copy the bytes over so that we can release the lock
				this.EncryptorOutputBlock.CopyTo(outputBytes);
			}
		}

		/// <summary>
		/// Implementation that writes a 16-byte ID to a span.
		/// </summary>
		private void WriteBytes(ReadOnlySpan<byte> idBytes, Span<byte> outputBytes)
		{
			System.Diagnostics.Debug.Assert(idBytes.Length == 16);
			System.Diagnostics.Debug.Assert(outputBytes.Length == 16);

			lock (this.Encryptor)
			{
				idBytes.CopyTo(this.EncryptorInputBlock.AsSpan());

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
			if (!publicId.TryWriteBytes(outputBytes))
				return false;

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
