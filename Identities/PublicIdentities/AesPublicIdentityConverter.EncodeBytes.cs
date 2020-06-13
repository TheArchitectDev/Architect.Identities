using System;
using System.Runtime.InteropServices;
using Architect.Identities.Helpers;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		static AesPublicIdentityConverter()
		{
			// Ensure that decimals are still structured the same way
			// This prevents the application from ever generating incorrect public identities in this extremely unlikely scenario, allowing a fix to be created
			DecimalStructure.ThrowIfDecimalStructureIsUnexpected();
		}

		public void GetPublicString(ulong id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, LongAsciiEncoder, 32);
		public void GetPublicString(long id, Span<byte> asciiOutput) => this.GetPublicString(LongToUlong(id), asciiOutput);
		public void GetPublicString(decimal id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, LongAsciiEncoder, 32);

		public void GetPublicShortString(ulong id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, ShortAsciiEncoder, 22);
		public void GetPublicShortString(long id, Span<byte> asciiOutput) => this.GetPublicShortString(LongToUlong(id), asciiOutput);
		public void GetPublicShortString(decimal id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, ShortAsciiEncoder, 22);

		public void GetPublicBytes(ulong id, Span<byte> outputBytes) => this.GetAsciiBytesCore(id, outputBytes, CopyOnlyEncoder, 16);
		public void GetPublicBytes(long id, Span<byte> outputBytes) => this.GetPublicBytes(LongToUlong(id), outputBytes);
		public void GetPublicBytes(decimal id, Span<byte> outputBytes) => this.GetAsciiBytesCore(id, outputBytes, CopyOnlyEncoder, 16);

		/// <summary>
		/// Implementation that writes to a span.
		/// </summary>
		private void GetAsciiBytesCore(ulong id, Span<byte> outputBytes, EncodingFunction encoder, int outputLength)
		{
			if (outputBytes.Length < outputLength) throw new ArgumentException($"Need {outputLength} bytes of output space.");

			// As soon as encryption is done, we will copy the bytes so that we can release the lock before encoding
			Span<byte> encryptedCopy = stackalloc byte[16];

			lock (this.Encryptor)
			{
				this.EncryptorInputUlongSpan[1] = id;

				// The first 8 bytes are always zero, and the last 8 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(this.EncryptorInputUlongSpan[0] == 0UL);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock) == 0, "The left 8 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock.AsSpan().Slice(8)) == id); // Confirm reversible operation

				var byteCount = this.Encryptor.TransformBlock(this.EncryptorInputBlock, 0, 16, this.EncryptorOutputBlock, 0);
				System.Diagnostics.Debug.Assert(byteCount == 16);

				// Copy the bytes over so that we can release the lock
				this.EncryptorOutputBlock.CopyTo(encryptedCopy);
			}

			// Now that we have released the lock, encode the output
			encoder(encryptedCopy, outputBytes.Slice(0, outputLength));
		}

		/// <summary>
		/// Implementation that writes to a span.
		/// </summary>
		private void GetAsciiBytesCore(decimal id, Span<byte> outputBytes, EncodingFunction encoder, int outputLength)
		{
			if (outputBytes.Length < outputLength) throw new ArgumentException($"Need {outputLength} bytes of output space.");

			if (id < 0m) throw new ArgumentOutOfRangeException();

			// As soon as encryption is done, we will copy the bytes so that we can release the lock before encoding
			Span<byte> encryptedCopy = stackalloc byte[16];

			lock (this.Encryptor)
			{
				this.EncryptorInputDecimalSpan[0] = id;

				if (id > CompanyUniqueIdEncoder.MaxDecimalValue || DecimalStructure.GetSignAndScale(this.EncryptorInputDecimalComponentSpan) != 0m)
					throw new ArgumentException($"Unexpected input value. Pass only values created by {nameof(CompanyUniqueId)}.{nameof(CompanyUniqueId.CreateId)}.", nameof(id));

				// The first 4 bytes are always zero, and the last 12 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(this.EncryptorInputDecimalComponentSpan[0] == 0);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<int>(this.EncryptorInputBlock) == 0, "The left 4 bytes were inadvertently used. They should remain 0.");

				System.Diagnostics.Debug.Assert(this.EncryptorInputDecimalSpan[0] == id); // Confirm reversible operation

				var byteCount = this.Encryptor.TransformBlock(this.EncryptorInputBlock, 0, 16, this.EncryptorOutputBlock, 0);
				System.Diagnostics.Debug.Assert(byteCount == 16);

				// Copy the bytes over so that we can release the lock
				this.EncryptorOutputBlock.CopyTo(encryptedCopy);
			}

			// Now that we have released the lock, encode the output
			encoder(encryptedCopy, outputBytes.Slice(0, outputLength));
		}
	}
}
