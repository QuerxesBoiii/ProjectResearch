using UnityEngine;
using Unity.Netcode;

public class NetworkSpawnModifier : MonoBehaviour
{
    [SerializeField] private float spawnHeight = 10f; // Height above ground to spawn players

    private void Awake()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObject = networkClient.PlayerObject;
            if (playerObject != null)
            {
                var position = playerObject.transform.position;
                playerObject.transform.position = new Vector3(position.x, position.y + spawnHeight, position.z);
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}