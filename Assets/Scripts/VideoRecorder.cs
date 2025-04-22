using Mirror;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class VideoRecorder : NetworkBehaviour
{
    [Header("Camera Settings")]
    public GameObject videoCameraObj;
    public Camera fpsCam;
    public Camera videoCam;
    public RenderTexture renderTexture;
    private bool hasCamera = false; // ✅ เพิ่มตัวแปร

    [Header("Selfie Settings")]
    public Transform normalCamTransform;
    public Transform selfieCamTransform;

    [Header("Recording Settings")]
    public int captureWidth = 512;
    public int captureHeight = 512;
    public float frameRate = 10f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minFov = 30f;
    public float maxFov = 60f;

    [Header("UI Settings")]
    public CanvasGroup recImage;

    private bool isRecording = false;
    private bool isPaused = false;
    public bool IsInCameraView = false;
    private bool isSelfie = false;

    private Camera mainCam;
    private float recordTimer = 0f;
    private List<Texture2D> recordedFrames = new();

    public override void OnStartLocalPlayer()
    {
        mainCam = Camera.main;

        // ✅ เฉพาะ Local Player เท่านั้นที่ใช้กล้อง FPS
        videoCameraObj.SetActive(true);
        fpsCam.enabled = false;

        recImage.alpha = 0f;

        if (videoCam.targetTexture == null && renderTexture != null)
            videoCam.targetTexture = renderTexture;

        if (fpsCam != null) fpsCam.enabled = false;
        if (videoCam != null) videoCam.enabled = false;
        if (videoCameraObj != null) videoCameraObj.SetActive(false);
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            // ✅ ถ้าไม่ใช่ Local Player ให้ปิดกล้อง FPS ทั้งหมด
            if (fpsCam != null) fpsCam.enabled = false;
            if (videoCam != null) videoCam.enabled = false;
            if (videoCameraObj != null) videoCameraObj.SetActive(false);
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !hasCamera)
            return; // 🛑 ไม่มีกล้อง → ไม่ให้ใช้งาน

        if (Input.GetMouseButtonDown(1)) ToggleCameraView(true);
        if (Input.GetMouseButtonUp(1)) ToggleCameraView(false);

        if (!IsInCameraView) return;

        if (IsInCameraView && isLocalPlayer)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float newFov = videoCam.fieldOfView - scroll * zoomSpeed;
                videoCam.fieldOfView = Mathf.Clamp(newFov, minFov, maxFov);
            }
        }

        // 🎥 คลิกซ้าย: เริ่ม/หยุดการอัดวิดีโอ
        if (Input.GetMouseButtonDown(0))
        {
            isRecording = !isRecording;
            isPaused = !isRecording; // reset pause

            UnityEngine.Debug.Log($"[{netId}] Recording: {isRecording}");
        }

        recImage.alpha = isRecording ? 1f : 0f;

        // สลับโหมดเซลฟี่
        if (Input.GetKeyDown(KeyCode.R))
        {
            isSelfie = !isSelfie;
            SwitchCameraPose();
        }

        // ⏺️ บันทึก Frame
        if (isRecording && !isPaused)
        {
            recordTimer += Time.deltaTime;
            if (recordTimer >= 1f / frameRate)
            {
                CaptureFrame();
                recordTimer = 0f;
            }
        }
    }

    public void PickupCamera(GameObject cameraInWorld)
    {
        if (!isLocalPlayer) return;

        UnityEngine.Debug.Log($"[{netId}] Picked up camera!");

        CmdPickupCamera(cameraInWorld);
    }

    [Command]
    void CmdPickupCamera(GameObject cameraInWorld)
    {
        RpcPickupCamera(cameraInWorld);
    }

    [ClientRpc]
    void RpcPickupCamera(GameObject cameraInWorld)
    {
        hasCamera = true;
        videoCameraObj.SetActive(true);
        videoCam.enabled = true; // ปิดก่อนเข้าสู่ FPS Mode
        recImage.alpha = 0f;

        // Destroy world object (optional)
        Destroy(cameraInWorld);
    }

    void CaptureFrame()
    {
        RenderTexture.active = renderTexture;

        Texture2D frame = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        frame.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        frame.Apply();

        recordedFrames.Add(frame);

        RenderTexture.active = null;

        UnityEngine.Debug.Log($"Captured frame: {recordedFrames.Count}");
    }

    void ToggleCameraView(bool camView)
    {
        IsInCameraView = camView;

        if (mainCam != null)
            mainCam.enabled = !camView;

        //videoCameraObj.SetActive(camView);
        fpsCam.enabled = camView;

        Cursor.lockState = camView ? CursorLockMode.Locked : CursorLockMode.Locked;
        Cursor.visible = !camView;

        if (camView)
        {
            isSelfie = false;
            SwitchCameraPose();
        }
    }

    void SwitchCameraPose()
    {
        if (!isLocalPlayer) return; // 🛑 ป้องกัน Remote คนอื่นมายุ่ง

        if (isSelfie && selfieCamTransform != null)
        {
            videoCam.transform.SetPositionAndRotation(selfieCamTransform.position, selfieCamTransform.rotation);
        }
        else if (normalCamTransform != null)
        {
            videoCam.transform.SetPositionAndRotation(normalCamTransform.position, normalCamTransform.rotation);
        }
    }

    public void ExportVideo()
    {
        isRecording = false;
        isPaused = false;

        UnityEngine.Debug.Log($"Exporting {recordedFrames.Count} frames...");

        if (recordedFrames.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No frames to export.");
            return;
        }

        string baseFolder = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Videos");

        if (!System.IO.Directory.Exists(baseFolder))
            System.IO.Directory.CreateDirectory(baseFolder);

        string folderName = $"record_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        string recordPath = System.IO.Path.Combine(baseFolder, folderName);
        System.IO.Directory.CreateDirectory(recordPath);

        // Save PNG frames
        for (int i = 0; i < recordedFrames.Count; i++)
        {
            Texture2D frame = recordedFrames[i];
            byte[] bytes = frame.EncodeToPNG();
            string filename = System.IO.Path.Combine(recordPath, $"frame_{i:D4}.png");
            System.IO.File.WriteAllBytes(filename, bytes);
        }

        UnityEngine.Debug.Log($"✅ Exported {recordedFrames.Count} frames to: {recordPath}");

        // 👉 Call FFmpeg
        string ffmpegPath;

#if UNITY_EDITOR
        // Editor ใช้จาก path ด้านบน project
        ffmpegPath = System.IO.Path.Combine(Application.dataPath, "../ffmpeg.exe");
#else
// Build ใช้จาก StreamingAssets
ffmpegPath = System.IO.Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
#endif
        string outputFile = System.IO.Path.Combine(recordPath, "video.mp4");

        string args = $"-framerate {frameRate} -i frame_%04d.png -c:v libx264 -pix_fmt yuv420p \"{outputFile}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            WorkingDirectory = recordPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process process = new Process { StartInfo = startInfo };
        process.Start();

        string output = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (System.IO.File.Exists(outputFile))
        {
            UnityEngine.Debug.Log($"🎬 Video exported to: {outputFile}");
            UIManager.Instance.ShowPopupText($"🎬 Video exported to: {outputFile}");
            recordedFrames.Clear();
        }
        else
        {
            UnityEngine.Debug.LogError($"❌ FFmpeg failed: {output}");
            UIManager.Instance.ShowPopupText($"❌ FFmpeg failed: {output}");
        }
    }

}
