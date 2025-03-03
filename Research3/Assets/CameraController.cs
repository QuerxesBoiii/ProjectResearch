using UnityEngine;
using UnityEngine.Rendering.Universal; // Needed for URP camera stacking

public class CameraController : MonoBehaviour
{
    [Header("Assign your two cameras here")]
    [Tooltip("This should be your Far Camera (the Base Camera) with clipping planes set, etc.")]
    public Camera baseCamera; // e.g., Far Camera

    [Tooltip("This should be your Main Camera As Player (the Overlay Camera)")]
    public Camera overlayCamera; // e.g., Main Camera (Overlay)

    [Header("Follow Settings")]
    [Tooltip("Optional offset from the player's position.")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    private Transform player;

    void Start()
    {
        // Find the player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player with tag 'Player' not found!");
        }

        // Check if both cameras are assigned
        if (baseCamera == null || overlayCamera == null)
        {
            Debug.LogError("Please assign both Base and Overlay cameras in the inspector.");
            return;
        }

        // Ensure the overlay camera is set to Overlay render type (if using URP)
        var overlayData = overlayCamera.GetUniversalAdditionalCameraData();
        overlayData.renderType = CameraRenderType.Overlay;

        // Ensure the base camera is set to Base render type (if using URP)
        var baseData = baseCamera.GetUniversalAdditionalCameraData();
        baseData.renderType = CameraRenderType.Base;

        // Add the overlay camera to the base camera's camera stack (if not already added)
        if (!baseData.cameraStack.Contains(overlayCamera))
        {
            baseData.cameraStack.Add(overlayCamera);
        }
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Update the CameraController's position to follow the player with an optional offset.
            transform.position = player.position + offset;

            // Optionally, if you want the cameras to match the player's rotation:
            // transform.rotation = player.rotation;
        }
    }
}
