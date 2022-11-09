using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace CoolNameSpace
{
    public class Client
    {
        public ushort id;
        public TcpClient client;
        public NetworkStream stream;

        public float x, y, z;

        public float rotation;

        public Queue<byte[]> packetQueue = new Queue<byte[]>();
        public Client(TcpClient _client, int _id)
        {
            client = _client;
            stream = client.GetStream();
            id = (ushort)_id;
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
        public void Main()
        {

        }
    }

}