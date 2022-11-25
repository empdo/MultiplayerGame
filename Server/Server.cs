using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Linq;

namespace CoolNameSpace
{

    public enum CSTypes
    {
        ping = 1,
        playerId,

        playerJoin,
        playerPosition,
        playerRotation,
        playerStateChange,
    }
    public class Server
    {
        public Server instance;
        static object _lock = new object();
        public Dictionary<int, Client> clients = new Dictionary<int, Client>();
        ushort currentTick = 0;

        TcpListener tcpServer;
        UdpClient udpServer;

        public Server()
        {
            instance = this;
        }
        public void StartServer()
        {
            StartTimer();
            try
            {
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("0.0.0.0");

                tcpServer = new TcpListener(localAddr, port);
                tcpServer.Start();

                udpServer = new UdpClient(port);
                udpServer.BeginReceive(UDPReceiveCallback, null);

                int count = 1;

                while (true)
                {

                    TcpClient client = tcpServer.AcceptTcpClient();
                    Client _client = new Client(client, count, instance);
                    lock (_lock) { clients.Add(count, _client); }

                    Console.WriteLine("New connection, count:" + count);

                    Thread thread = new Thread(clientHandler);
                    thread.Start(count);

                    count++;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.Write("\nHit enter to continue...");
            Console.Read();
        }

        private void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.EndReceive(_result, ref _endPoint);
                udpServer.BeginReceive(UDPReceiveCallback, null);

                if (data.Length < 2)
                {
                    return;
                }

                int clientId = BitConverter.ToUInt16(data, 0);

                if (clientId == 0)
                {
                    return;
                }
                if (clients[clientId].udp.endpoint == null)
                {
                    Console.WriteLine("New Client");
                    // If this is a new connection
                    clients[clientId].udp.Connect(_endPoint);
                    return;
                }

                if (clients[clientId].udp.endpoint.ToString() == _endPoint.ToString())
                {
                    clients[clientId].HandlePacket(data.Skip(sizeof(ushort)).ToArray());
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
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

            byte[] packet = ConstructPackage((ushort)CSTypes.ping, Encoding.ASCII.GetBytes("ping"));

            foreach (Client client in clients.Values)
            {

                if (client.udp.endpoint != null)
                {

                    udpServer.BeginSend(packet, packet.Length, client.udp.endpoint, null, null);
                    foreach (byte[] _packet in client.udp.OutPacketQueue)
                    {
                        Console.WriteLine(BitConverter.ToUInt16(_packet));
                        udpServer.BeginSend(_packet, _packet.Length, client.udp.endpoint, null, null);
                    }

                }
                client.udp.OutPacketQueue.Clear();

                client.tcp.stream.Write(packet, 0, packet.Length);
                foreach (byte[] _packet in client.tcp.packetQueue)
                {
                    client.tcp.stream.Write(_packet, 0, _packet.Length);
                }
                client.tcp.packetQueue.Clear();
            }
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
        public void SendToAllOther(Client client, byte[] packet)
        {
            lock (client)
            {
                foreach (Client _client in clients.Values)
                {
                    if (_client.id != client.id)
                    {
                        _client.tcp.packetQueue.Enqueue(packet);
                    }
                }
            }

        }

        //    public void HandlePlayerState(byte[] packetContent, Client client)
        //    {
        //        lock (client)
        //        {
        //            List<byte> bytes = new List<byte>();

        //            ushort _state = BitConverter.ToUInt16(packetContent);

        //            CoolNameSpace.PlayerStates state;

        //            if (Enum.IsDefined(typeof(CoolNameSpace.PlayerStates), _state))
        //            {
        //                state = (CoolNameSpace.PlayerStates)_state;
        //            }
        //            else
        //            {
        //                return;
        //            }

        //            client.playerState = state;


        //            byte[] stateBytes = BitConverter.GetBytes((ushort)client.playerState);
        //            byte[] idBytes = BitConverter.GetBytes(client.id);

        //            bytes.AddRange(idBytes);
        //            bytes.AddRange(stateBytes);

        //            byte[] packet = ConstructPackage((ushort)CSTypes.playerStateChange, bytes.ToArray());


        //            SendToAllOther(client, packet);
        //        }
        //    }


        //    void OnJoin(Client client)
        //    {
        //        lock (client)
        //        {

        //            foreach (Client _client in clients.Values)
        //            {
        //                if (client != _client)
        //                {
        //                    byte[] packet = ConstructPackage((ushort)CSTypes.playerPosition, PositionToBytes(new float[] { _client.x, _client.y, _client.z }, _client.id));
        //                    client.tcp.packetQueue.Enqueue(packet);
        //                }
        //            }
        //        }
        //    }

        public void clientHandler(object count)
        {
            Console.WriteLine("Count: " + count);
            Client client;
            lock (_lock) { client = clients[(int)count]; }
            byte[] byteId = BitConverter.GetBytes((ushort)(int)count);

            client.tcp.packetQueue.Enqueue(ConstructPackage((ushort)CSTypes.playerId, byteId));

            while (client.tcp.tcpClient.Client.Connected)
            {
                if (client.tcp.stream.CanRead & client.tcp.stream.DataAvailable)
                {
                    //Storlek av en ushort: 2 bytes
                    byte[] buffer = new byte[2];
                    client.tcp.stream.Read(buffer, 0, buffer.Length);
                    ushort packetType = BitConverter.ToUInt16(buffer, 0);

                    client.tcp.stream.Read(buffer, 0, buffer.Length);
                    ushort packetLength = BitConverter.ToUInt16(buffer, 0);

                    byte[] packetContent = new byte[packetLength];
                    int bytes = client.tcp.stream.Read(packetContent, 0, packetContent.Length);

                    switch (packetType)
                    {
                    }
                }
            }
            Console.WriteLine("Removing client");
            lock (_lock) clients.Remove((int)count);
            client.tcp.tcpClient.Client.Shutdown(SocketShutdown.Both);
            client.tcp.tcpClient.Close();
        }

    }
}

