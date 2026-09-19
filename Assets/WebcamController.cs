using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    [Header("Gắn RawImage vào đây")]
    public RawImage displayScreen;

    private WebCamTexture webcamTexture;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("Không tìm thấy bất kỳ thiết bị camera nào!");
            return;
        }

        // Tự động tìm camera có tên DroidCam, nếu không thấy sẽ lấy camera đầu tiên
        string targetDeviceName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"Camera [{i}]: {devices[i].name}");
            if (devices[i].name.Contains("DroidCam"))
            {
                targetDeviceName = devices[i].name;
                break;
            }
        }

        // Mở luồng video ở mức 640x480 và 30 FPS để tối ưu hiệu năng
        webcamTexture = new WebCamTexture(targetDeviceName, 640, 480, 30);
        displayScreen.texture = webcamTexture;
        webcamTexture.Play();
    }

    void OnDestroy()
    {
        // Giải phóng camera khi thoát game để tránh bị khóa luồng thiết bị
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}