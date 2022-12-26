using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StopWatch = System.Diagnostics.Stopwatch;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using StarterAssets;

namespace MultiplayerAssets
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
    public enum SCTypes
    {
        ping = 1,
        SyncTick,
        playerRotation,
    }

    public class ClientConnection : MonoBehaviour
    {
        public ClientConnection instance;
        public int port = 13000;
        public string IpAddress = "127.0.0.1";

        public Udp udp;
        static TcpClient client;
        static NetworkStream stream;
        public GameObject playerPrefab;
        public GameObject localPlayerPrefab;
        GameObject localPlayer;

        public ushort playerId;
        Vector3 oldpos;

        float oldRot;
        float currentTickRate;

        StopWatch sw = new StopWatch();

        public ClientsManager clientsManager;

        Queue<byte[]> packetQueue = new Queue<byte[]>();

        ushort serverTick;

        public UIManager _UIManager;

        public static object _lock = new object();
        void Start()
        {
            instance = this;
            _UIManager.submitButton.onClick.AddListener(Connect);
            serverTick = 2;
        }
        void Connect()
        {
            Debug.Log("Connecting");
            client = new TcpClient(IpAddress, port);
            stream = client.GetStream();

            udp = new Udp(instance);

            _UIManager.UIState = false;

            localPlayer = Instantiate(localPlayerPrefab, new Vector3(0, 5, 0), Quaternion.identity);

        }


        public void SetIP(string value)
        {
            IpAddress = value;
            Debug.Log(value);
        }

        public void SetPort(string value)
        {
            port = Int32.Parse(value);
            Debug.Log(port);
        }

        void FixedUpdate()
        {
            currentTickRate += Time.fixedDeltaTime;
            RunProcessData();

        }

        void Update()
        {
            if (udp != null)
            {

                foreach (byte[] packet in udp.inPacketQueue.ToList())
                {
                    udp.HandleUdpData(packet);
                }
                udp.inPacketQueue.Clear();
            }
        }

        void RunProcessData()
        {
            if (stream != null && stream.DataAvailable)

            {
                ProcessData();
            }
        }

        void ProcessData()
        {
            //TODO: Check stream.Length edge case
            int streamLength = 32000;
            byte[] streamBuffer = new byte[streamLength];
            stream.Read(streamBuffer, 0, (int)streamLength);
            PacketStream result = new PacketStream(streamBuffer);

            serverTick++;

            while (result.offset < result.Length)
            {
                ushort packetType = result.ReadUShort();

                if (packetType == 0)
                {
                    return;
                }

                ushort packetLength = result.ReadUShort();

                byte[] packetContent = result.ReadContent(packetLength);


                switch (packetType)
                {
                    case (ushort)CSTypes.ping:
                        TcpOnTick();
                        break;
                    case (ushort)CSTypes.playerId:
                        HandlePlayerId(packetContent);
                        break;
                }

            }
        }

        void HandlePlayerId(byte[] data)
        {
            playerId = BitConverter.ToUInt16(data);

            udp.Connect(port);
        }

        public void TcpOnTick()
        {

            if (packetQueue.Count > 0)
            {
                foreach (byte[] packet in packetQueue)
                {
                    stream.Write(packet, 0, packet.Length);
                }

                packetQueue.Clear();
            }


        }

        public void UdpOnTick()
        {
            Debug.Log("UdpONTock");

            clientsManager.tickRate = currentTickRate;

            _UIManager.pingText.text = "Tickrate :" + currentTickRate;
            currentTickRate = 0;

            if (localPlayer != null && oldpos != localPlayer.transform.position)
            {

                positionToPacket(localPlayer.transform.position, (ushort)CSTypes.playerPosition);
                oldpos = localPlayer.transform.position;
            }

            float rotation = localPlayer.transform.localRotation.eulerAngles.y;

            if (localPlayer != null && oldRot != rotation)
            {
                RotationToPacket(rotation, (ushort)CSTypes.playerRotation);
                oldRot = rotation;
            }

            if (udp.connected)
            {

                foreach (byte[] packet in udp.packetQueue)
                {
                    udp.client.Send(packet, packet.Length);
                }
            }


            udp.packetQueue.Clear();
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
        void RotationToPacket(float rotation, ushort type)
        {
            byte[] _rotation = BitConverter.GetBytes(rotation);

            byte[] packet = ConstructPackage((ushort)CSTypes.playerRotation, _rotation);

            udp.QueuePacket(packet, playerId);
        }

        void positionToPacket(Vector3 position, ushort type)
        {
            List<byte> packet = new List<byte>();
            Int16 length = sizeof(float) * 3;

            byte[] packetType = BitConverter.GetBytes(type);
            byte[] xpos = BitConverter.GetBytes(position.x);
            byte[] ypos = BitConverter.GetBytes(position.y);
            byte[] zpos = BitConverter.GetBytes(position.z);
            byte[] packetLength = BitConverter.GetBytes(length);

            packet.AddRange(packetType);
            packet.AddRange(packetLength);
            packet.AddRange(xpos);
            packet.AddRange(ypos);
            packet.AddRange(zpos);

            udp.QueuePacket(packet.ToArray(), playerId);
        }



    }

    public class Udp
    {
        public UdpClient client;
        public IPEndPoint endpoint;

        public bool connected = false;
        ClientConnection instance;


        public Queue<byte[]> packetQueue = new Queue<byte[]>();
        public Queue<byte[]> inPacketQueue = new Queue<byte[]>();
        public Udp(ClientConnection _instance)
        {
            instance = _instance;
            endpoint = new IPEndPoint(IPAddress.Parse(instance.IpAddress), instance.port) ;
        }

        public void Connect(int _port)
        {
            client = new UdpClient(_port + 7);
                client.Client.SetSocketOption(
                SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            client.Connect(endpoint);
            client.BeginReceive(new AsyncCallback(ReceiveCallback), null);

            connected = true;
            Debug.Log("Udp connection established");

            byte[] packet = instance.ConstructPackage((ushort)CSTypes.ping, Encoding.ASCII.GetBytes("ping"));
            client.Send(packet, packet.Length);

        }

        void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = client.EndReceive(_result, ref endpoint);
                client.BeginReceive(new AsyncCallback(ReceiveCallback), null);

                if (_data.Length < 4)
                {
                    return;
                }

                inPacketQueue.Enqueue(_data);
            }
            catch (Exception e)
            {
                Debug.Log("Error: " + e.ToString());
            }
        }

        public void HandleUdpData(byte[] data)
        {
            int streamLength = data.Length;
            PacketStream result = new PacketStream(data);

            ushort packetType = result.ReadUShort();

            if (packetType == 0)
            {
                return;
            }

            ushort packetLength = result.ReadUShort();

            byte[] packetContent = result.ReadContent(packetLength);

            Debug.Log("packetType: " + packetType);

            switch (packetType)
            {
                case (ushort)CSTypes.ping:
                    instance.UdpOnTick();
                    break;
                case (ushort)CSTypes.playerPosition:
                    PlayerPosition(packetContent);
                    break;
                case (ushort)CSTypes.playerRotation:
                    PlayerRotation(packetContent);
                    break;

            }

        }
        public void QueuePacket(byte[] _packet, ushort id)
        {
            List<byte> packet = new List<byte>();
            packet.AddRange(BitConverter.GetBytes(id));
            packet.AddRange(_packet);

            packetQueue.Enqueue(packet.ToArray());

        }

        Tuple<ushort, float> ReadPlayerRotation(byte[] bytes)
        {
            ushort id = BitConverter.ToUInt16(bytes, 0);

            float rotation = BitConverter.ToSingle(bytes, sizeof(ushort));

            return Tuple.Create<ushort, float>(id, rotation);
        }

        Tuple<ushort?, Vector3> byteToPosition(byte[] bytes, int type)
        {
            ushort? id = null;
            int offset = 0;
            if (type == 1)
            {
                id = BitConverter.ToUInt16(bytes, 0);
                offset += sizeof(Int16);
            }
            float xpos = BitConverter.ToSingle(bytes, offset);
            offset += sizeof(float);
            float ypos = BitConverter.ToSingle(bytes, offset);
            offset += sizeof(float);
            float zpos = BitConverter.ToSingle(bytes, offset);

            return Tuple.Create<ushort?, Vector3>(id, new Vector3(xpos, ypos, zpos));
        }
        void PlayerPosition(byte[] packetContent)
        {
            Tuple<ushort?, Vector3> response = byteToPosition(packetContent, 1);

            if (response.Item1 != null)
            {
                ushort id = (ushort)response.Item1;
                Vector3 position = response.Item2;

                instance.clientsManager.PlayerPosition(id, position);
            }
        }

        void PlayerRotation(byte[] packetContent)
        {
            Tuple<ushort, float> response = ReadPlayerRotation(packetContent);

            instance.clientsManager.PlayerRotation((ushort)response.Item1, response.Item2);
        }

    }
}