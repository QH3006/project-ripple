import cv2
import socket
import os
import urllib.request
import ssl
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

ssl._create_default_https_context = ssl._create_unverified_context

model_name = "pose_landmarker_lite.task"
if not os.path.exists(model_name):
    print("Đang tải model Pose Landmarker...")
    model_url = "https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/latest/pose_landmarker_lite.task"
    urllib.request.urlretrieve(model_url, model_name)

base_options = python.BaseOptions(model_asset_path=model_name)
options = vision.PoseLandmarkerOptions(
    base_options=base_options,
    running_mode=vision.RunningMode.IMAGE
)
detector = vision.PoseLandmarker.create_from_options(options)

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_address = ("127.0.0.1", 5065)

# Thử camera index 0 (nếu không lên hình, đổi thành 1 hoặc 2)
cap = cv2.VideoCapture(0)

print("Đang chạy kiểm tra... Nhìn vào terminal xem có in tọa độ không.")

while cap.isOpened():
    success, frame = cap.read()
    if not success:
        print("Không đọc được hình ảnh từ Camera!")
        continue

    rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
    detection_result = detector.detect(mp_image)

    if detection_result.pose_landmarks and len(detection_result.pose_landmarks) > 0:
        wrist = detection_result.pose_landmarks[0][16]
        message = f"{wrist.x:.4f},{wrist.y:.4f}"
        
        # IN TỌA ĐỘ RA TERMINAL ĐỂ KIỂM TRA
        print(f"-> Đang gửi sang Unity: X={wrist.x:.2f}, Y={wrist.y:.2f}")
        sock.sendto(message.encode('utf-8'), unity_address)
        
        # Vẽ một chấm tròn nhỏ lên cửa sổ Python để xác nhận
        h, w, _ = frame.shape
        cx, cy = int(wrist.x * w), int(wrist.y * h)
        cv2.circle(frame, (cx, cy), 10, (0, 0, 255), -1)

    # Mở cửa sổ xem trực tiếp những gì Python thấy
    cv2.imshow("Python Camera Debug", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()