using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Utilities
{
    /// <summary>
    /// A modified class of <see cref="River"/> that implements more types and isn't sealed, so other plugins can inherit and expand it with more.
    /// </summary>
    [UsedImplicitly]
    public class RiverExpanded
    {
        /// <summary>
        /// Reads a <see cref="double"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="double"/>.</returns>
        [UsedImplicitly]
        public double ReadDouble()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToDouble(Buffer, 0);
        }

        /// <summary>
        /// Writes a <see cref="double"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="double"/> value to write.</param>
        [UsedImplicitly]
        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        /// <summary>
        /// Reads a <see cref="string"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        [UsedImplicitly]
        public string ReadString()
        {
            var count = Stream.ReadByte();
            Stream.Read(Buffer, 0, count);
            return Encoding.UTF8.GetString(Buffer, 0, count);
        }

        /// <summary>
        /// Reads a <see cref="bool"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="bool"/>.</returns>
        public bool ReadBoolean()
        {
            return Stream.ReadByte() != 0;
        }

        /// <summary>
        /// Reads a <see cref="byte"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="byte"/>.</returns>
        public byte ReadByte()
        {
            return (byte) Stream.ReadByte();
        }

        /// <summary>
        /// Reads multiple <see cref="byte"/>s from the stream.
        /// </summary>
        /// <returns>An <see cref="Array"/> of <see cref="byte"/>s.</returns>
        [UsedImplicitly]
        public byte[] ReadBytes()
        {
            var array = new byte[ReadUInt16()];
            Stream.Read(array, 0, array.Length);
            return array;
        }

        /// <summary>
        /// Reads an <see cref="short"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="short"/>.</returns>
        [UsedImplicitly]
        public short ReadInt16()
        {
            Stream.Read(Buffer, 0, 2);
            return BitConverter.ToInt16(Buffer, 0);
        }

        /// <summary>
        /// Reads an <see cref="ushort"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="ushort"/>.</returns>
        public ushort ReadUInt16()
        {
            Stream.Read(Buffer, 0, 2);
            return BitConverter.ToUInt16(Buffer, 0);
        }

        /// <summary>
        /// Reads an <see cref="int"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="int"/>.</returns>
        public int ReadInt32()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToInt32(Buffer, 0);
        }

        /// <summary>
        /// Reads an <see cref="uint"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="uint"/>.</returns>
        public uint ReadUInt32()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToUInt32(Buffer, 0);
        }

        /// <summary>
        /// Reads a <see cref="float"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="float"/>.</returns>
        public float ReadSingle()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToSingle(Buffer, 0);
        }

        /// <summary>
        /// Reads an <see cref="long"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="long"/>.</returns>
        public long ReadInt64()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToInt64(Buffer, 0);
        }

        /// <summary>
        /// Reads an <see cref="ulong"/> from the stream.
        /// </summary>
        /// <returns>An <see cref="ulong"/>.</returns>
        public ulong ReadUInt64()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToUInt64(Buffer, 0);
        }

        /// <summary>
        /// Reads a <see cref="CSteamID"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="CSteamID"/>.</returns>
        [UsedImplicitly]
        public CSteamID ReadSteamID()
        {
            return new CSteamID(ReadUInt64());
        }

        /// <summary>
        /// Reads a <see cref="Vector3"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="Vector3"/>.</returns>
        [UsedImplicitly]
        public Vector3 ReadSingleVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        /// Reads a <see cref="Quaternion"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="Quaternion"/>.</returns>
        [UsedImplicitly]
        public Quaternion ReadSingleQuaternion()
        {
            return Quaternion.Euler(ReadSingle(), ReadSingle(), ReadSingle());
        }

        /// <summary>
        /// Reads a <see cref="Color"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="Color"/>.</returns>
        [UsedImplicitly]
        public Color ReadColor()
        {
            return new Color(ReadByte() / 255f, ReadByte() / 255f, ReadByte() / 255f);
        }

        /// <summary>
        /// Reads a <see cref="DateTime"/> from the stream.
        /// </summary>
        /// <returns>A <see cref="DateTime"/>.</returns>
        [UsedImplicitly]
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        /// <summary>
        /// Writes a <see cref="string"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="string"/> value to write.</param>
        [UsedImplicitly]
        public void WriteString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var b = (byte) bytes.Length;
            Stream.WriteByte(b);
            Stream.Write(bytes, 0, b);
            Water += 1 + b;
        }

        /// <summary>
        /// Writes a <see cref="bool"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="bool"/> value to write.</param>
        public void WriteBoolean(bool value)
        {
            // ReSharper disable once RedundantCast
            // Removing cast causes compile exception.
            Stream.WriteByte((byte) (value ? 1 : 0));
            Water++;
        }

        /// <summary>
        /// Writes a <see cref="byte"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="byte"/> value to write.</param>
        public void WriteByte(byte value)
        {
            Stream.WriteByte(value);
            Water++;
        }

        /// <summary>
        /// Writes an <see cref="Array"/> of <see cref="double"/>s to the stream.
        /// </summary>
        /// <param name="values">The <see cref="Array"/> of <see cref="double"/>s to write.</param>
        [UsedImplicitly]
        public void WriteBytes(byte[] values)
        {
            var num = (ushort) values.Length;
            WriteUInt16(num);
            Stream.Write(values, 0, num);
            Water += num;
        }

        /// <summary>
        /// Writes an <see cref="short"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="short"/> value to write.</param>
        [UsedImplicitly]
        public void WriteInt16(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 2);
            Water += 2;
        }

        /// <summary>
        /// Writes an <see cref="ushort"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="ushort"/> value to write.</param>
        public void WriteUInt16(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 2);
            Water += 2;
        }

        /// <summary>
        /// Writes an <see cref="int"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="int"/> value to write.</param>
        public void WriteInt32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        /// <summary>
        /// Writes an <see cref="uint"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="uint"/> value to write.</param>
        public void WriteUInt32(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        /// <summary>
        /// Writes a <see cref="float"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="float"/> value to write.</param>
        public void WriteSingle(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        /// <summary>
        /// Writes an <see cref="long"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="long"/> value to write.</param>
        public void WriteInt64(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        /// <summary>
        /// Writes an <see cref="ulong"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="ulong"/> value to write.</param>
        public void WriteUInt64(ulong value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        /// <summary>
        /// Writes a <see cref="CSteamID"/> to the stream.
        /// </summary>
        /// <param name="steamId">The <see cref="CSteamID"/> value to write.</param>
        [UsedImplicitly]
        public void WriteSteamID(CSteamID steamId)
        {
            WriteUInt64(steamId.m_SteamID);
        }

        /// <summary>
        /// Writes a <see cref="Vector3"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="Vector3"/> value to write.</param>
        [UsedImplicitly]
        public void WriteSingleVector3(Vector3 value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
        }

        /// <summary>
        /// Writes a <see cref="Quaternion"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="Quaternion"/> value to write.</param>
        [UsedImplicitly]
        public void WriteSingleQuaternion(Quaternion value)
        {
            var eulerAngles = value.eulerAngles;
            WriteSingle(eulerAngles.x);
            WriteSingle(eulerAngles.y);
            WriteSingle(eulerAngles.z);
        }

        /// <summary>
        /// Writes a <see cref="Color"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="Color"/> value to write.</param>
        [UsedImplicitly]
        public void WriteColor(Color value)
        {
            WriteByte((byte) (value.r * 255f));
            WriteByte((byte) (value.g * 255f));
            WriteByte((byte) (value.b * 255f));
        }

        /// <summary>
        /// Writes a <see cref="DateTime"/> to the stream.
        /// </summary>
        /// <param name="value">The <see cref="DateTime"/> value to write.</param>
        [UsedImplicitly]
        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.ToBinary());
        }

        /// <summary>
        /// Closes and disposes of the stream.
        /// </summary>
        public void CloseRiver()
        {
            if (Water > 0)
                Stream.SetLength(Water);

            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
        }

        /// <summary>
        /// Creates a new instance of <see cref="RiverExpanded"/>.
        /// </summary>
        /// <param name="newPath">The path of the file to which this object will read and write to.</param>
        /// <param name="usePath">If the path should be combined with <see cref="ReadWrite.PATH"/>.</param>
        public RiverExpanded(string newPath, bool usePath = true)
        {
            Path = newPath;
            if (usePath)
                Path = ReadWrite.PATH + Path;

            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            Stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            Water = 0;
        }

        /// <summary>
        /// Reads <paramref name="count"/> <see cref="byte"/>s but does not interpret them in any way, essentially skipping them.
        /// </summary>
        /// <param name="count">The number of <see cref="byte"/>s that you wish to skip ahead.</param>
        [UsedImplicitly]
        public void Skip(int count)
        {
            Stream.Read(Buffer, 0, count);
        }

        /// <summary>
        /// The buffer to which all reads are performed to.
        /// </summary>
        [UsedImplicitly]
        public byte[] Buffer { get; protected set; } = new byte[Block.BUFFER_SIZE];

        /// <summary>
        /// The number of bytes currently pending to be written and flushed.
        /// </summary>
        public int Water { get; protected set; }

        /// <summary>
        /// The path to the file to read/write from/to.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The FileStream dealing with the file.
        /// </summary>
        [UsedImplicitly]
        public FileStream Stream { get; protected set; }
    }
}