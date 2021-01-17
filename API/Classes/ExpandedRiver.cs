using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Pustalorc.Plugins.BaseClustering.API.Classes
{
    public class ExpandedRiver
    {
        public double ReadDouble()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToDouble(Buffer, 0);
        }

        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        [NotNull]
        public string ReadString()
        {
            var count = Stream.ReadByte();
            Stream.Read(Buffer, 0, count);
            return Encoding.UTF8.GetString(Buffer, 0, count);
        }

        public bool ReadBoolean()
        {
            return Stream.ReadByte() != 0;
        }

        public byte ReadByte()
        {
            return (byte) Stream.ReadByte();
        }

        [NotNull]
        public byte[] ReadBytes()
        {
            var array = new byte[ReadUInt16()];
            Stream.Read(array, 0, array.Length);
            return array;
        }

        public short ReadInt16()
        {
            Stream.Read(Buffer, 0, 2);
            return BitConverter.ToInt16(Buffer, 0);
        }

        public ushort ReadUInt16()
        {
            Stream.Read(Buffer, 0, 2);
            return BitConverter.ToUInt16(Buffer, 0);
        }

        public int ReadInt32()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToInt32(Buffer, 0);
        }

        public uint ReadUInt32()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToUInt32(Buffer, 0);
        }

        public float ReadSingle()
        {
            Stream.Read(Buffer, 0, 4);
            return BitConverter.ToSingle(Buffer, 0);
        }

        public long ReadInt64()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToInt64(Buffer, 0);
        }

        public ulong ReadUInt64()
        {
            Stream.Read(Buffer, 0, 8);
            return BitConverter.ToUInt64(Buffer, 0);
        }

        public CSteamID ReadSteamID()
        {
            return new CSteamID(ReadUInt64());
        }

        public Vector3 ReadSingleVector3()
        {
            return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Quaternion ReadSingleQuaternion()
        {
            return Quaternion.Euler(ReadSingle(), ReadSingle(), ReadSingle());
        }

        public Color ReadColor()
        {
            return new Color(ReadByte() / 255f, ReadByte() / 255f, ReadByte() / 255f);
        }

        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        public void WriteString([NotNull] string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var b = (byte) bytes.Length;
            Stream.WriteByte(b);
            Stream.Write(bytes, 0, b);
            Water += 1 + b;
        }

        public void WriteBoolean(bool value)
        {
            Stream.WriteByte((byte) (value ? 1 : 0));
            Water++;
        }

        public void WriteByte(byte value)
        {
            Stream.WriteByte(value);
            Water++;
        }

        public void WriteBytes([NotNull] byte[] values)
        {
            var num = (ushort) values.Length;
            WriteUInt16(num);
            Stream.Write(values, 0, num);
            Water += num;
        }

        public void WriteInt16(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 2);
            Water += 2;
        }

        public void WriteUInt16(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 2);
            Water += 2;
        }

        public void WriteInt32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        public void WriteUInt32(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        public void WriteSingle(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 4);
            Water += 4;
        }

        public void WriteInt64(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        public void WriteUInt64(ulong value)
        {
            var bytes = BitConverter.GetBytes(value);
            Stream.Write(bytes, 0, 8);
            Water += 8;
        }

        public void WriteSteamID(CSteamID steamID)
        {
            WriteUInt64(steamID.m_SteamID);
        }

        public void WriteSingleVector3(Vector3 value)
        {
            WriteSingle(value.x);
            WriteSingle(value.y);
            WriteSingle(value.z);
        }

        public void WriteSingleQuaternion(Quaternion value)
        {
            var eulerAngles = value.eulerAngles;
            WriteSingle(eulerAngles.x);
            WriteSingle(eulerAngles.y);
            WriteSingle(eulerAngles.z);
        }

        public void WriteColor(Color value)
        {
            WriteByte((byte) (value.r * 255f));
            WriteByte((byte) (value.g * 255f));
            WriteByte((byte) (value.b * 255f));
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.ToBinary());
        }

        public void CloseRiver()
        {
            if (Water > 0)
                Stream.SetLength(Water);

            Stream.Flush();
            Stream.Close();
            Stream.Dispose();
        }

        public ExpandedRiver(string newPath, bool usePath = true)
        {
            Path = newPath;
            if (usePath)
                Path = ReadWrite.PATH + Path;

            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            Stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            Water = 0;
        }

        protected byte[] Buffer = new byte[Block.BUFFER_SIZE];

        protected int Water;

        protected string Path;

        protected FileStream Stream;
    }
}