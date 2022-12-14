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

        public Queue<(ushort, Vector3)> playerSpawnQueue = new Queue<(ushort, Vector3)>();

        public class Client
        {
            public ushort id;
            public Vector3 position;

            public Lerper lerper;

            public PlayerScript playerScript;

            public GameObject player;


            public Client(ushort _id, Vector3 _position, GameObject _player)
            {
                id = _id;
                position = _position;
                player = _player;

                playerScript = _player.GetComponent<PlayerScript>();
            }

        }

        void Start()
        {
        }

        void Update()
        {
            foreach ((ushort, Vector3) player in playerSpawnQueue)
            {
                HandleNewPlayer(player.Item1, player.Item2);
            }

            playerSpawnQueue.Clear();
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

        public void PlayerRotation(int id, float pitch, float yawn)
        {

            Client? client = clients.Find(client => client.id == id);

            if (client != null)
            {
                client.playerScript.PlayerRotation(pitch, yawn);
            }
        }

        public void PlayerPosition(ushort _id, Vector3 position)
        {

            Client? client = clients.Find(client => client.id == _id);

            positionPacketBuffer.Add((_id, position));

            if (client != null && client.lerper != null)
            {
                List<(ushort, Vector3)> positions = positionPacketBuffer.Where(packet => packet.Item1 == _id).ToList();

                if (positions.Count() == 1)
                {
                    client.player.transform.position = position;
                }
                else
                {
                    client.lerper.time = 0.008f;
                    client.lerper.startPos = positions[^2].Item2;
                    client.lerper.targetPos = positions[^1].Item2;
                }

            }
            else
            {
                List<int> l = new List<int>();
                foreach ((ushort, Vector3) player in playerSpawnQueue)
                {
                    l.Append(player.Item1);
                    Debug.Log("Added player with id: " + player.Item1);
                }

                if (!l.Contains(_id))
                {

                    playerSpawnQueue.Enqueue((_id, position));
                }
            }


        }
    }
}
