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
        public class Client
        {
            public ushort id;
            public Vector3 position;

            public GameObject player;

            public Client(ushort _id, Vector3 _position, GameObject _player)
            {
                id = _id;
                position = _position;
                player = _player;
            }

        }
        public List<Client> clients = new List<Client>();

        void start()
        {
            Debug.Log("Start");
        }

        public void HandleNewPlayer(ushort id, Vector3 position)
        {
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);

            clients.Add(new Client(id, position, player));

            Debug.Log("Instantiate player at:" + position);
        }

        public void PlayerPosition(ushort id, Vector3 position)
        {
            Client? client = clients.Find(client => client.id == id);

            Debug.Log("Move client with id " + id + "To" + position);

            if (client != null)
            {
                client.player.transform.position = position;
            }
            else
            {
                HandleNewPlayer(id, position);
            }
        }
    }
}