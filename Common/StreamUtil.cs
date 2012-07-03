using System;
using System.IO;

namespace Front {

	/// <summary>
	/// Provides utility functions for <see cref="Stream"/> management.
	/// </summary>
	public class StreamUtil {
		#region Constants

		private const int DefaultBufferSize = 64 * 1024;

		#endregion

		#region Methods

		#region Read Byte

		/// <summary>
		/// Reads one unsigned byte from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static byte ReadUByte(Stream stream) {
#if DEBUG
			int value = stream.ReadByte();

			if (value == -1) {
				throw new InvalidDataException("Unexpected end of stream.");
			}

			return (byte)value;
#else
			return (byte)stream.ReadByte();
#endif
		}

		/// <summary>
		/// Reads one signed byte from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static sbyte ReadSByte(Stream stream) {
#if DEBUG
			int value = stream.ReadByte();

			if (value == -1) {
				throw new InvalidDataException("Unexpected end of stream.");
			}

			return (sbyte)value;
#else
			return (sbyte)stream.ReadByte();
#endif

		}

		#endregion

		#region Write Byte

		/// <summary>
		/// Write one unsigned byte to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteUByte(Stream stream, byte value) {
			stream.WriteByte(value);
		}

		/// <summary>
		/// Write one signed byte to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteSByte(Stream stream, sbyte value) {
			stream.WriteByte((byte)value);
		}

		#endregion

		#region Read Word

		/// <summary>
		/// Reads one unsigned big-endian word from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static ushort ReadUWordBE(Stream stream) {
			return (ushort)(
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one unsigned little-endian word from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static ushort ReadUWordLE(Stream stream) {
			return (ushort)(
				(ReadUByte(stream) << 0) |
				(ReadUByte(stream) << 8));
		}

		/// <summary>
		/// Reads one signed big-endian word from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static short ReadSWordBE(Stream stream) {
			return (short)(
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one signed little-endian word from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static short ReadSWordLE(Stream stream) {
			return (short)(
				(ReadUByte(stream) << 0) |
				(ReadUByte(stream) << 8));
		}

		#endregion

		#region Write Word

		/// <summary>
		/// Write one unsigned big-endian word to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUWordBE(Stream stream, ushort value) {
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one unsigned little-endian word to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUWordLE(Stream stream, ushort value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
		}

		/// <summary>
		/// Write one signed big-endian word to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSWordBE(Stream stream, short value) {
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one signed little-endian word to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSWordLE(Stream stream, short value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
		}

		#endregion

		#region Read DWord

		/// <summary>
		/// Reads one unsigned big-endian dword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static uint ReadUDWordBE(Stream stream) {
			return (uint)(
				(ReadUByte(stream) << 24) |
				(ReadUByte(stream) << 16) |
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one unsigned little-endian dword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static uint ReadUDWordLE(Stream stream) {
			return (uint)(
				(ReadUByte(stream) << 0) |
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 16) |
				(ReadUByte(stream) << 24));
		}

		/// <summary>
		/// Reads one signed big-endian dword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static int ReadSDWordBE(Stream stream) {
			return (int)(
				(ReadUByte(stream) << 24) |
				(ReadUByte(stream) << 16) |
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one signed little-endian dword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static int ReadSDWordLE(Stream stream) {
			return (int)(
				(ReadUByte(stream) << 0) |
				(ReadUByte(stream) << 8) |
				(ReadUByte(stream) << 16) |
				(ReadUByte(stream) << 24));
		}

		#endregion

		#region Write DWord

		/// <summary>
		/// Write one unsigned big-endian dword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUDWordBE(Stream stream, uint value) {
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one unsigned little-endian dword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUDWordLE(Stream stream, uint value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
		}

		/// <summary>
		/// Write one signed big-endian dword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSDWordBE(Stream stream, int value) {
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one signed little-endian dword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSDWordLE(Stream stream, int value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
		}

		#endregion

		#region Read QWord

		/// <summary>
		/// Reads one unsigned big-endian qword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static ulong ReadUQWordBE(Stream stream) {
			return (ulong)(
				((ulong)ReadUByte(stream) << 56) |
				((ulong)ReadUByte(stream) << 48) |
				((ulong)ReadUByte(stream) << 40) |
				((ulong)ReadUByte(stream) << 32) |
				((ulong)ReadUByte(stream) << 24) |
				((ulong)ReadUByte(stream) << 16) |
				((ulong)ReadUByte(stream) << 8) |
				((ulong)ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one unsigned little-endian qword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		[CLSCompliant(false)]
		public static ulong ReadUQWordLE(Stream stream) {
			return (ulong)(
				((ulong)ReadUByte(stream) << 0) |
				((ulong)ReadUByte(stream) << 8) |
				((ulong)ReadUByte(stream) << 16) |
				((ulong)ReadUByte(stream) << 24) |
				((ulong)ReadUByte(stream) << 32) |
				((ulong)ReadUByte(stream) << 40) |
				((ulong)ReadUByte(stream) << 48) |
				((ulong)ReadUByte(stream) << 56));
		}

		/// <summary>
		/// Reads one signed big-endian qword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static long ReadSQWordBE(Stream stream) {
			return (long)(
				((long)ReadUByte(stream) << 56) |
				((long)ReadUByte(stream) << 48) |
				((long)ReadUByte(stream) << 40) |
				((long)ReadUByte(stream) << 32) |
				((long)ReadUByte(stream) << 24) |
				((long)ReadUByte(stream) << 16) |
				((long)ReadUByte(stream) << 8) |
				((long)ReadUByte(stream) << 0));
		}

		/// <summary>
		/// Reads one signed little-endian qword from stream.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <returns>read value.</returns>
		public static long ReadSQWordLE(Stream stream) {
			return (long)(
				((long)ReadUByte(stream) << 0) |
				((long)ReadUByte(stream) << 8) |
				((long)ReadUByte(stream) << 16) |
				((long)ReadUByte(stream) << 24) |
				((long)ReadUByte(stream) << 32) |
				((long)ReadUByte(stream) << 40) |
				((long)ReadUByte(stream) << 48) |
				((long)ReadUByte(stream) << 56));
		}

		#endregion

		#region Write QWord

		/// <summary>
		/// Write one unsigned big-endian qword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUQWordBE(Stream stream, ulong value) {
			stream.WriteByte((byte)((value >> 56) & 0xFF));
			stream.WriteByte((byte)((value >> 48) & 0xFF));
			stream.WriteByte((byte)((value >> 40) & 0xFF));
			stream.WriteByte((byte)((value >> 32) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one unsigned little-endian qword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		[CLSCompliant(false)]
		public static void WriteUQWordLE(Stream stream, ulong value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 32) & 0xFF));
			stream.WriteByte((byte)((value >> 40) & 0xFF));
			stream.WriteByte((byte)((value >> 48) & 0xFF));
			stream.WriteByte((byte)((value >> 56) & 0xFF));
		}

		/// <summary>
		/// Write one signed big-endian qword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSQWordBE(Stream stream, long value) {
			stream.WriteByte((byte)((value >> 56) & 0xFF));
			stream.WriteByte((byte)((value >> 48) & 0xFF));
			stream.WriteByte((byte)((value >> 40) & 0xFF));
			stream.WriteByte((byte)((value >> 32) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 0) & 0xFF));
		}

		/// <summary>
		/// Write one signed little-endian qword to stream.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="value">Value to write.</param>
		public static void WriteSQWordLE(Stream stream, long value) {
			stream.WriteByte((byte)((value >> 0) & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 32) & 0xFF));
			stream.WriteByte((byte)((value >> 40) & 0xFF));
			stream.WriteByte((byte)((value >> 48) & 0xFF));
			stream.WriteByte((byte)((value >> 56) & 0xFF));
		}

		#endregion

		#region Read Array

		/// <summary>
		/// Reads array from stream.
		/// Array must be written to stream by <see cref="WriteArray"/> function.
		/// </summary>
		/// <param name="stream">Readable stream.</param>
		/// <param name="array">Read array.</param>
		public static void ReadArray(Stream stream, out byte[] array) {
			byte[] arrayLength = new byte[4];

			stream.Read(arrayLength, 0, arrayLength.Length);

			array = new byte[(arrayLength[0] << 0) | (arrayLength[1] << 8) | (arrayLength[2] << 16) | (arrayLength[3] << 24)];

			stream.Read(array, 0, array.Length);
		}

		#endregion

		#region Write Array

		/// <summary>
		/// Writes array to stream.
		/// Array must be read by <see cref="ReadArray"/> function.
		/// </summary>
		/// <param name="stream">Writable stream.</param>
		/// <param name="array">Array to read.</param>
		public static void WriteArray(Stream stream, byte[] array) {
			byte[] arrayLength = new byte[4]
			{
				(byte)((array.Length >> 0) & 0xFF),
				(byte)((array.Length >> 8) & 0xFF),
				(byte)((array.Length >> 16) & 0xFF),
				(byte)((array.Length >> 24) & 0xFF)
			};

			stream.Write(arrayLength, 0, arrayLength.Length);
			stream.Write(array, 0, array.Length);
		}

		#endregion

		#region Copy Byte

		/// <summary>
		/// Copies data from source stream to destination stream.
		/// </summary>
		/// <param name="source">Source stream.</param>
		/// <param name="destination">Destination stream.</param>
		/// <param name="length">Number of bytes to copy.</param>
		/// <param name="bufferSize">size of buffer to use while copying.</param>
		public static void Copy(Stream source, Stream destination, long length, int bufferSize) {
			byte[] buffer = new byte[bufferSize];
			int count = 0;

			for (long offset = 0; offset < length; offset += count) {
				count = (int)((offset + buffer.Length > length) ? (length - offset) : buffer.Length);
				count = source.Read(buffer, 0, count);

				if (count == 0) {
					break;
				}

				destination.Write(buffer, 0, count);
			}
		}

		/// <summary>
		/// Copies data from source stream to destination stream.
		/// </summary>
		/// <param name="source">Source stream.</param>
		/// <param name="destination">Destination stream.</param>
		/// <param name="length">Number of bytes to copy.</param>
		public static void Copy(Stream source, Stream destination, long length) {
			Copy(source, destination, length, DefaultBufferSize);
		}

		/// <summary>
		/// Copies source stream to destination stream.
		/// </summary>
		/// <param name="source">Source stream.</param>
		/// <param name="destination">Destination stream.</param>
		public static void Copy(Stream source, Stream destination) {
			Copy(source, destination, source.Length, DefaultBufferSize);
		}

		#endregion

		#endregion
	}
}
