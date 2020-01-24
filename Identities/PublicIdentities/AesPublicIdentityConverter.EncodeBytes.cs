using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	internal sealed partial class AesPublicIdentityConverter
	{
		public void GetPublicString(ulong id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, LongAsciiEncoder, 32);
		public void GetPublicString(long id, Span<byte> asciiOutput) => this.GetPublicString(id >= 0 ? (ulong)id : throw new ArgumentOutOfRangeException(), asciiOutput);

		public void GetPublicShortString(ulong id, Span<byte> asciiOutput) => this.GetAsciiBytesCore(id, asciiOutput, ShortAsciiEncoder, 22);
		public void GetPublicShortString(long id, Span<byte> asciiOutput) => this.GetPublicShortString(id >= 0 ? (ulong)id : throw new ArgumentOutOfRangeException(), asciiOutput);

		public void GetPublicBytes(ulong id, Span<byte> outputBytes) => this.GetAsciiBytesCore(id, outputBytes, CopyOnlyEncoder, 16);
		public void GetPublicBytes(long id, Span<byte> outputBytes) => this.GetPublicBytes(id >= 0 ? (ulong)id : throw new ArgumentOutOfRangeException(), outputBytes);

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
				// The first 8 bytes are always zero, and the last 8 bytes we overwrite
				System.Diagnostics.Debug.Assert(this.EncryptorInputBlock.Length == 16);
				System.Diagnostics.Debug.Assert(this.EncryptorInputUlongSpan[0] == 0UL);
				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock) == 0, "The left 8 bytes were inadvertently used. They should remain 0.");

				this.EncryptorInputUlongSpan[1] = id;

				System.Diagnostics.Debug.Assert(MemoryMarshal.Read<ulong>(this.EncryptorInputBlock.AsSpan().Slice(8)) == id); // Confirm reversible operation

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
