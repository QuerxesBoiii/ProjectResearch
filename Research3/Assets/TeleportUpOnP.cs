using UnityEngine;
using Unity.Netcode;

public class TeleportUpOnP : NetworkBehaviour
{
    void Update()
    {
        // Only process input if this object is owned by the local client.
        if (!IsOwner)
            return;
        
        // Check if the P key was pressed.
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Teleport the player 200 units up on the Y axis.
            transform.position += new Vector3(0, 200, 0);
            Debug.Log("Player teleported 200 units up.");
        }
    }
}
