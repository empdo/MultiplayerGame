using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace CoolNameSpace
{
    public class Client
    {
        public ushort id;

        public float x, y, z;

        public float rotation;

        public Udp udp;

        public Tcp tcp;

        public Server instance;

        public Client(TcpClient _client, int _id, Server _instance)
        {
            id = (ushort)_id;
            tcp = new Tcp(_client);
            udp = new Udp();
            instance = _instance;
        }

        public void UpdatePosition(float[] position)
        {
            x = position[0];
            y = position[1];
            z = position[2];

        }

        public void ApplyDeltaPosition(float[] position)
        {
            x += position[0];
            y += position[1];
            z += position[2];
        }


        public byte[] ConstructPackage(ushort packetType, byte[] data)
        {
            List<byte> packet = new List<byte>();

            ushort packetLength = (ushort)(data.Length);

            packet.AddRange(BitConverter.GetBytes(packetType));
            packet.AddRange(BitConverter.GetBytes(packetLength));
            packet.AddRange(data);

            return packet.ToArray();
        }
        public byte[] PositionToBytes(float[] position, ushort id)
        {

            List<byte> packet = new List<byte>();

            byte[] xpos = BitConverter.GetBytes(position[0]);
            byte[] ypos = BitConverter.GetBytes(position[1]);
            byte[] zpos = BitConverter.GetBytes(position[2]);

            packet.AddRange(BitConverter.GetBytes((ushort)id));

            packet.AddRange(xpos);
            packet.AddRange(ypos);
            packet.AddRange(zpos);

            return packet.ToArray();
        }
        float[] byteToPosition(byte[] bytes)
        {

            int offset = 0;
            float xpos = BitConverter.ToSingle(bytes, 0);
            offset += sizeof(float);
            float ypos = BitConverter.ToSingle(bytes, offset);
            offset += sizeof(float);
            float zpos = BitConverter.ToSingle(bytes, offset);

            return new float[3] { xpos, ypos, zpos };
        }

        void SendToAllOther(byte[] packet)
        {

            foreach (Client client in instance.clients.Values)
            {
                if (client.id != id)
                {
                    Console.WriteLine("Enqued packet");
                    client.udp.OutPacketQueue.Enqueue(packet);
                }
            }

        }

        public void HandlePlayerRotation(byte[] packetContent, Client client)
        {

            List<byte> bytes = new List<byte>();

            float rotation = BitConverter.ToSingle(packetContent);
            client.rotation = rotation;

            byte[] idBytes = BitConverter.GetBytes(client.id);
            byte[] rotationBytes = BitConverter.GetBytes(client.rotation);

            bytes.AddRange(idBytes);
            bytes.AddRange(rotationBytes);

            byte[] packet = ConstructPackage((ushort)CSTypes.playerRotation, bytes.ToArray());

            Console.WriteLine("Type:" + BitConverter.ToUInt16(packet));

            SendToAllOther(packet);
        }
        public void HandlePlayerPosition(byte[] packetContent)
        {
            float[] position = byteToPosition(packetContent);
            UpdatePosition(position);

            byte[] bytes = PositionToBytes(new float[3] { x, y, z }, id);
            byte[] packet = ConstructPackage((ushort)CSTypes.playerPosition, bytes);

            SendToAllOther(packet);
        }
        public void HandlePacket(byte[] data)
        {
            PacketStream packet = new PacketStream(data);

            ushort packetType = packet.ReadUShort();
            ushort packetLength = packet.ReadUShort();
            byte[] packetContent = packet.ReadContent(packetLength);
            Console.WriteLine(packetType);

            if (packetType == 0)
            {
                return;
            }

            switch (packetType)
            {
                case ((ushort)CSTypes.playerPosition):
                    HandlePlayerPosition(packetContent);
                    break;
            }

        }
        public class Tcp
        {
            public TcpClient tcpClient;
            public NetworkStream stream;

            public Queue<byte[]> packetQueue = new Queue<byte[]>();
            public Tcp(TcpClient _tcpClient)
            {
                tcpClient = _tcpClient;
                stream = tcpClient.GetStream();
            }

        }

        public class Udp
        {

            public IPEndPoint endpoint;
            public Queue<byte[]> InPacketQueue = new Queue<byte[]>();
            public Queue<byte[]> OutPacketQueue = new Queue<byte[]>();
            public bool connected = false;
            public void Connect(IPEndPoint _endpoint)
            {
                endpoint = _endpoint;
                connected = true;
            }


        }
    }

}