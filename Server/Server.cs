using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace CoolNameSpace
{
    public enum CSTypes
    {
        ping = 1,
        playerJoin,
        playerPosition,
    }
    public enum SCTypes
    {
        ping = 1,
        SyncTick,
    }
    public class Server
    {
        static Dictionary<Client, int> clients = new Dictionary<Client, int>();
        ushort currentTick = 0;
        public void StartServer()
        {
            StartTimer();
            TcpListener server = null;
            try
            {
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, port);
                server.Start();

                while (true)
                {
                    Console.WriteLine("Waiting for a connection... ");

                    TcpClient client = server.AcceptTcpClient();

                    Thread thread = new Thread(() => clientHandler(client));
                    thread.Start();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
            }

            Console.Write("\nHit enter to continue...");
            Console.Read();
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

        public void StartTimer()
        {
            System.Timers.Timer timer = new System.Timers.Timer(8);
            timer.Elapsed += TickHandler;

            timer.Enabled = true;
            Console.WriteLine("Started Timer");
        }

        public void TickHandler(Object source, ElapsedEventArgs e)
        {
            //Tickrate 125, intervall på 8ms
            currentTick++;

            //Every fifth second
            if (currentTick % 625 == 0)
            {
                SyncTick();
            }

            byte[] packet = ConstructPackage((ushort)CSTypes.ping, Encoding.ASCII.GetBytes("ping"));
            foreach (Client client in clients.Keys)
            {
                client.stream.Write(packet, 0, packet.Length);
                foreach (byte[] _packet in client.packetQueue)
                {
                    client.stream.Write(_packet, 0, _packet.Length);
                    Console.WriteLine("Sent package");
                }
                client.packetQueue.Clear();
            }
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

        public void SendToAllOther(Client client, byte[] packet)
        {
            foreach (Client _client in clients.Keys)
            {
                if (_client.id != client.id)
                {
                    _client.packetQueue.Enqueue(packet);
                }
            }

        }

        public void HandlePlayerPosition(byte[] packetContent, Client client)
        {

            byte[] bytes = PositionToBytes(new float[3] { client.x, client.y, client.z }, client.id);
            byte[] packet = ConstructPackage((ushort)CSTypes.playerPosition, bytes);

            float[] position = byteToPosition(packetContent);
            client.UpdatePosition(position);

            SendToAllOther(client, packet);
        }

        void SyncTick()
        {
            byte[] tick = BitConverter.GetBytes(currentTick);
            byte[] packet = ConstructPackage((ushort)SCTypes.SyncTick, tick);

            foreach (Client client in clients.Keys)
            {
                client.packetQueue.Enqueue(packet);
            }
        }


        public void clientHandler(TcpClient _client)
        {
            Console.WriteLine("Connection established from: " + _client.Client.RemoteEndPoint);

            //Skapa en instans av klienten 
            int _id = clients.Count + 1;
            Client client = new Client(_client, _id);
            clients.Add(client, _id);

            SyncTick();

            while (_client.Client.Connected)
            {
                if (client.stream.CanRead & client.stream.DataAvailable)
                {
                    Console.WriteLine("Listening for packages...");

                    //Storlek av en ushort: 2 bytes
                    byte[] buffer = new byte[2];
                    client.stream.Read(buffer, 0, buffer.Length);
                    ushort packetType = BitConverter.ToUInt16(buffer, 0);

                    client.stream.Read(buffer, 0, buffer.Length);
                    ushort packetLength = BitConverter.ToUInt16(buffer, 0);

                    byte[] packetContent = new byte[packetLength];
                    int bytes = client.stream.Read(packetContent, 0, packetContent.Length);

                    Console.WriteLine("packetType: " + packetType + " with length " + packetLength);
                    switch (packetType)
                    {
                        case (ushort)CSTypes.playerPosition:
                            HandlePlayerPosition(packetContent, client);
                            break;
                    }

                }
            }
            clients.Remove(client);
            client.client.Client.Shutdown(SocketShutdown.Both);
            client.client.Close();

        }
    }

}