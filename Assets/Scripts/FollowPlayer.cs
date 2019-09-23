using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public Vector3 cameraAngle;

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position + offset;
        transform.rotation = Quaternion.Euler(cameraAngle); // For adjusting the camera angle
    }
}
