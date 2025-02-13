using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour {
    [SerializeField] private GameObject _playerPrefab; // Changed from PlayerController to GameObject

    public override void OnNetworkSpawn() {
        if (IsServer) {
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }   

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong playerId) {
        var spawn = Instantiate(_playerPrefab);
        var networkObject = spawn.GetComponent<NetworkObject>();

        if (networkObject != null) {
            networkObject.SpawnWithOwnership(playerId);
        } else {
            Debug.LogError("Spawned prefab is missing a NetworkObject component!");
        }
    }

    public override void OnDestroy() {
        base.OnDestroy();
        MatchmakingService.LeaveLobby();
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }
}
