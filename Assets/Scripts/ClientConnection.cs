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
        playerJoin,
        playerPosition,
        playerRotation,
    }
    public enum SCTypes
    {
        ping = 1,
        SyncTick,
        playerRotation,
    }

    public class ClientConnection : MonoBehaviour
    {
        public int port = 13000;
        public string IpAddress = "83.227.32.208";
        static TcpClient client;
        static NetworkStream stream;
        public GameObject playerPrefab;

        public GameObject localPlayerPrefab;
        public GameObject localPlayer;

        Vector3 oldpos;
        float oldRot;

        private int currentTickRate = 8;
        StopWatch sw = new StopWatch();

        public ClientsManager clientsManager;

        Queue<byte[]> packetQueue = new Queue<byte[]>();

        private ushort serverTick;

        public UIManager _UIManager;

        public static object _lock = new object();
        void Start()
        {

            _UIManager.submitButton.onClick.AddListener(Connect);
            serverTick = 2;
        }
        void Connect()
        {
            Debug.Log("Connecting");
            client = new TcpClient(IpAddress, port);
            stream = client.GetStream();

            _UIManager.UIState = false;

            localPlayer = Instantiate(localPlayerPrefab, new Vector3(0, 5, 0), Quaternion.identity);

            localPlayer.GetComponent<StarterAssets.StarterAssetsInputs>().cursorLocked = true;

        }

        void FixedUpdate()
        {
            RunProcessData();
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
            Debug.Log("ProcessData!");
            //TODO: Check stream.Length edge case
            int streamLength = 4096;
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
                        OnTick();
                        break;
                    case (ushort)CSTypes.playerPosition:
                        PlayerPosition(packetContent);

                        break;
                    case (ushort)CSTypes.playerRotation:
                        PlayerRotation(packetContent);
                        break;
                }

            }
        }

        void OnTick()
        {
            sw.Stop();

            if (serverTick % 125 == 0)
            {
                currentTickRate = (int)sw.ElapsedMilliseconds;
            }

            _UIManager.pingText.text = "Tickrate:" + currentTickRate + " ms\nPing: " + (int)currentTickRate / 8;

            sw.Reset();
            sw.Start();


            if (localPlayer && oldpos != localPlayer.transform.position)
            {
                positionToPacket(localPlayer.transform.position, (ushort)CSTypes.playerPosition);
                oldpos = localPlayer.transform.position;
            }

            float rotation = localPlayer.transform.localRotation.eulerAngles.y;

            if (localPlayer && oldRot != rotation) ;
            {
                RotationToPacket(rotation, (ushort)CSTypes.playerRotation);
                oldRot = rotation;
            }



            if (packetQueue.Count > 0)
            {
                foreach (byte[] packet in packetQueue)
                {
                    stream.Write(packet, 0, packet.Length);
                }

                packetQueue.Clear();
            }


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

            packetQueue.Enqueue(packet);
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

            packetQueue.Enqueue(packet.ToArray());
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

        void HandleTickSync(byte[] packetContent)
        {
            ushort tick = BitConverter.ToUInt16(packetContent);

            if (serverTick != tick)
            {
                Debug.Log("Synced tick, from: " + serverTick + " to " + tick);
                serverTick = tick;
            }

        }

        Tuple<ushort, float> ReadPlayerRotation(byte[] bytes)
        {
            ushort id = BitConverter.ToUInt16(bytes, 0);

            float rotation = BitConverter.ToSingle(bytes, sizeof(ushort));

            return Tuple.Create<ushort, float>(id, rotation);
        }

        void PlayerPosition(byte[] packetContent)
        {
            Tuple<ushort?, Vector3> response = byteToPosition(packetContent, 1);

            if (response.Item1 != null)
            {
                ushort id = (ushort)response.Item1;
                Vector3 position = response.Item2;

                clientsManager.PlayerPosition(id, position);
            }
        }

        void PlayerRotation(byte[] packetContent)
        {
            Tuple<ushort, float> response = ReadPlayerRotation(packetContent);

            clientsManager.PlayerRotation((ushort)response.Item1, response.Item2);
        }

    }
}