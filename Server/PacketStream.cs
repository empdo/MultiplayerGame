using System;
using System.Collections;
using System.Collections.Generic;

namespace CoolNameSpace 
{

    public class PacketStream
    {
        byte[] buffer;
        public int Length { get => buffer.Length; }

        public int offset = 0;

        public PacketStream(byte[] _stream)
        {
            buffer = _stream;

        }

        public ushort ReadUShort()
        {
            ushort _ushort = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            return _ushort;
        }

        public byte[] ReadContent(int length)
        {
            byte[] content = new List<byte>(buffer).GetRange(offset, length).ToArray();
            offset += length;

            return content;

        }
    }

}