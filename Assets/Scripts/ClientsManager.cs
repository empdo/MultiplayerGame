using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

using UnityEngine;

namespace MultiplayerAssets
{
    public class ClientsManager : MonoBehaviour
    {
        public GameObject playerPrefab;

        public List<(ushort, Vector3)> positionPacketBuffer = new List<(ushort, Vector3)>();
        public List<Client> clients = new List<Client>();
        public float tickRate;
        public class Client
        {
            public ushort id;
            public Vector3 position;

            public Lerper lerper;

            public GameObject player;

            public Client(ushort _id, Vector3 _position, GameObject _player)
            {
                id = _id;
                position = _position;
                player = _player;
            }

        }

        void Start()
        {
            Debug.Log("Start");
        }

        void Update()
        {
        }

        public void HandleNewPlayer(ushort id, Vector3 position)
        {
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);

            Client client = new Client(id, position, player);
            client.lerper = client.player.GetComponent(typeof(Lerper)) as Lerper;
            client.lerper.player = player;

            clients.Add(client);


            Debug.Log("Instantiate player at:" + position);
        }

        public void PlayerRotation(int id, float rotation)
        {

            Client? client = clients.Find(client => client.id == id);

            if (client != null)
            {
                Quaternion target = Quaternion.Euler(new Vector3(0, rotation, 0));
                client.player.transform.rotation = target;
            }
        }

        public void PlayerPosition(ushort id, Vector3 position)
        {
            Client? client = clients.Find(client => client.id == id);

            positionPacketBuffer.Add((id, position));

            if (client != null && client.lerper != null)
            {
                List<(ushort, Vector3)> positions = positionPacketBuffer.Where(packet => packet.Item1 == id).ToList();

                if (positions.Count() == 1)
                {
                    client.player.transform.position = position;
                }
                else
                {
                    client.lerper.time = tickRate;
                    client.lerper.startPos = positions[^2].Item2;
                    client.lerper.targetPos = positions[^1].Item2;

                }

            }
            else
            {
                HandleNewPlayer(id, position);
            }


        }
    }
}