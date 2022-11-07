using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using StopWatch = System.Diagnostics.Stopwatch;
using System.Linq;
using System.Timers;

using UnityEngine;

using StarterAssets;

namespace MultiplayerAssets
{

    public enum CSTypes
    {
        ping,
        playerJoin,
        playerPosition,
    }
    public enum SCTypes
    {
        ping,
        SyncTick,
    }

    public class ClientConnection : MonoBehaviour
    {
        public int port = 13000;
        string IpAddress = "127.0.0.1";
        static TcpClient client;
        static NetworkStream stream;
        public GameObject playerPrefab;
        public GameObject localPlayer;

        public Vector3 oldpos;

        private int currentTickRate = 8;
        StopWatch sw = new StopWatch();

        public ClientsManager clientsManager;

        Queue<byte[]> packetQueue = new Queue<byte[]>();

        private ushort serverTick;

        public UIManager _UIManager;
        void Start()
        {
            _UIManager.submitButton.onClick.AddListener(Connect);
            serverTick = 2;
        }

        void Connect()
        {
            client = new TcpClient(IpAddress, port);
            stream = client.GetStream();

            if (stream.CanRead)
            {
                _UIManager.UIState = false;
                localPlayer.GetComponent<StarterAssets.StarterAssetsInputs>().cursorLocked = true;
                StartTimer();

            }
        }


        void StartTimer()
        {
            System.Timers.Timer timer = new System.Timers.Timer(8);
            timer.Elapsed += (System.Object source, ElapsedEventArgs e) => serverTick++;

            timer.Enabled = true;
            Console.WriteLine("Started Timer");
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

        void FixedUpdate()
        {
            if (stream == null || !client.Connected || stream.CanRead || !stream.CanWrite)
            {
                return;
            }

            if (localPlayer && oldpos != localPlayer.transform.position)
            {
                positionToPacket(localPlayer.transform.position, (ushort)CSTypes.playerPosition);
                oldpos = localPlayer.transform.position;
            }

            if (packetQueue.Count > 0)
            {
                foreach (byte[] packet in packetQueue)
                {
                    stream.Write(packet, 0, packet.Length);
                }

                packetQueue.Clear();
            }


            while (stream.DataAvailable)
            {
                //Storlek av en ushort: 2 bytes
                byte[] buffer = new byte[2];
                stream.Read(buffer, 0, buffer.Length);
                ushort packetType = BitConverter.ToUInt16(buffer, 0);

                stream.Read(buffer, 0, buffer.Length);
                ushort packetLength = BitConverter.ToUInt16(buffer, 0);

                byte[] packetContent = new byte[packetLength];
                int bytes = stream.Read(packetContent, 0, packetContent.Length);

                switch (packetType)
                {
                    case (ushort)CSTypes.ping:
                        OnTick();
                        break;
                    case (ushort)CSTypes.playerPosition:
                        PlayerPosition(packetContent);
                        break;
                }

            }

        }

    }
}