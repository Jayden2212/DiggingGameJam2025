using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.forward);
        transform.Rotate(new Vector3(0f, 5f * Time.deltaTime, 0f));
    }
}
