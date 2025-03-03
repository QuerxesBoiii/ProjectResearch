using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;
using sapra.InfiniteLands;

public class CameraRigEnabler : NetworkBehaviour
{
    [SerializeField] private Camera farCamera;           // The Base camera
    [SerializeField] private Camera mainCameraAsPlayer; // The Overlay camera

    public override void OnNetworkSpawn()
    {
        // If not the local owner, disable the rig
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        // We are the local owner: set up camera stacking
        if (farCamera != null && mainCameraAsPlayer != null)
        {
            var baseData = farCamera.GetUniversalAdditionalCameraData();
            baseData.renderType = CameraRenderType.Base;

            var overlayData = mainCameraAsPlayer.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;

            // Clear old stack and add the overlay
            baseData.cameraStack.Clear();
            baseData.cameraStack.Add(mainCameraAsPlayer);
        }
        else
        {
            Debug.LogError("Please assign FarCamera and MainCameraAsPlayer in the inspector!");
        }

        // --- Approach B: Assign this camera rig to FloatingOrigin at runtime ---
        var floating = FindObjectOfType<FloatingOrigin>();
        if (floating != null)
        {
            // If you want the floating origin to track the camera rig's position:
            floating.SetOriginReference(this.transform);

            // If you'd rather track the player's root, do:
            // floating.SetOriginReference(transform.parent);
        }
    }
}
