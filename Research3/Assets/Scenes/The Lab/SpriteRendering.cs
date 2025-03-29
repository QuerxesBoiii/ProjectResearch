using UnityEngine;

public class SpriteRendering : MonoBehaviour
{
    [SerializeField]
    private GameObject MainCamera;

    private void LateUpdate()
    {
        Vector3 cameraPosition = MainCamera.transform.position;

        cameraPosition.y = transform.position.y;

        transform.LookAt(cameraPosition);

        transform.Rotate(0f, 180f, 0f);
    }
}
