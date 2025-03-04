using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class CameraRigEnabler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // If not the local owner, disable the rig
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }
    }
}
