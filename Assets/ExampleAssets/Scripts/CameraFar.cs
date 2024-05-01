using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Android;

public class CameraFar : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager cameraManager; // AR 카메라 매니저 참조
    public float farClipDistance = 1000f; // 원하는 Far clipping plane 거리 설정

    [System.Obsolete]

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.farClipPlane = farClipDistance;
        }
        else
        {
            Debug.LogError("Camera component not found on this GameObject");
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        // 나침반 활성화
        Input.compass.enabled = true;
        Debug.Log(Input.compass.trueHeading);
        Quaternion rotation = Quaternion.Euler(0, -Input.compass.trueHeading, 0);
        transform.rotation = rotation;
    }

    void Update()
    {
        Debug.Log(Input.compass.trueHeading);
        transform.eulerAngles = new Vector3(-Input.compass.trueHeading, 0.0f, 0.0f);
    }
}
