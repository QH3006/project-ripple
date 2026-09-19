using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class HandTrackerReceiver : MonoBehaviour
{
    [Header("Gắn HandCursor vào đây")]
    public RectTransform handCursor;
    public RectTransform canvasRect;

    private Thread receiveThread;
    private UdpClient client;
    private const int Port = 5065;

    // Lưu trữ tọa độ chuẩn hóa (0.0 đến 1.0) từ Python
    private float rawNormX = 0.5f;
    private float rawNormY = 0.5f;
    private bool hasData = false;
    private bool isRunning = true;
    private readonly object lockObj = new object();

    void Start()
    {
        if (canvasRect == null)
            canvasRect = GetComponent<RectTransform>();

        receiveThread = new Thread(ReceiveData) { IsBackground = true };
        receiveThread.Start();
    }

    // LUỒNG CHẠY NGẦM: Chỉ nhận dữ liệu thô, không đụng tới Unity API
    private void ReceiveData()
    {
        try
        {
            client = new UdpClient(Port);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UDP] Không mở được cổng {Port}: {ex.Message}");
            return;
        }

        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);

                string[] coords = text.Split(',');
                if (coords.Length == 2)
                {
                    float x = float.Parse(coords[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(coords[1], CultureInfo.InvariantCulture);

                    lock (lockObj)
                    {
                        rawNormX = x;
                        rawNormY = y;
                        hasData = true;
                    }
                }
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UDP] Lỗi đọc gói tin: {ex.Message}");
            }
        }
    }

    // LUỒNG CHÍNH (MAIN THREAD): Xử lý hiển thị và tương tác tọa độ an toàn
    void Update()
    {
        if (!hasData || handCursor == null || canvasRect == null) return;

        float currentNormX;
        float currentNormY;

        lock (lockObj)
        {
            currentNormX = rawNormX;
            currentNormY = rawNormY;
        }

        // Gọi canvasRect.rect trên Main Thread an toàn tuyệt đối
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        // Chuyển đổi tọa độ từ dải [0, 1] sang hệ tọa độ Canvas tâm (0, 0)
        float targetX = (0.5f - currentNormX) * width;
        float targetY = (0.5f - currentNormY) * height;

        Vector2 currentPos = handCursor.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, targetY);

        // Làm mượt cử động ở 60 FPS
        handCursor.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * 30f);
    }

    void OnDestroy()
    {
        isRunning = false;
        client?.Close();
        receiveThread?.Abort();
    }
}