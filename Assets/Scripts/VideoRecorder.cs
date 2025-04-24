using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;


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

    public Action<float>? OnExportProgress;

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

        if (UIManager.Instance.IsExporting) return;

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

    public IEnumerator ExportVideo()
    {
        UIManager.Instance.IsExporting = true;
        isRecording = false;
        isPaused = false;

        Debug.Log($"Exporting {recordedFrames.Count} frames...");

        if (recordedFrames.Count == 0)
        {
            Debug.LogWarning("No frames to export.");
            yield break;
        }

        string baseFolder = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Videos");
        Directory.CreateDirectory(baseFolder);

        string folderName = $"record_{DateTime.Now:yyyyMMdd_HHmmss}";
        string recordPath = Path.Combine(baseFolder, folderName);
        Directory.CreateDirectory(recordPath);

        // ⏳ Export PNG one by one (yield each frame)
        for (int i = 0; i < recordedFrames.Count; i++)
        {
            Texture2D frame = recordedFrames[i];
            byte[] bytes = frame.EncodeToPNG();
            string filename = Path.Combine(recordPath, $"frame_{i:D4}.png");
            File.WriteAllBytes(filename, bytes);

            float progress = (i + 1f) / recordedFrames.Count;
            OnExportProgress?.Invoke(progress);

            yield return null; // wait 1 frame
        }

        Debug.Log($"✅ Exported {recordedFrames.Count} PNGs to: {recordPath}");

        // ▶️ Run FFmpeg (still blocking, run in background)
        string ffmpegPath =
#if UNITY_EDITOR
            Path.Combine(Application.dataPath, "../ffmpeg.exe");
#else
        Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
#endif

        string outputFile = Path.Combine(recordPath, "video.mp4");
        string args = $"-framerate {frameRate} -i frame_%04d.png -c:v libx264 -pix_fmt yuv420p \"{outputFile}\"";

        // ✅ Run FFmpeg in background, but wait in coroutine
        bool done = false;
        bool success = false;
        string ffmpegOutput = "";

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
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

                ffmpegOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                success = File.Exists(outputFile);
            }
            catch (Exception e)
            {
                ffmpegOutput = e.Message;
                success = false;
            }
            finally
            {
                done = true;
            }
        });

        // รอ FFmpeg จนเสร็จ
        while (!done)
        {
            yield return null;
        }

        if (success)
        {
            Debug.Log($"🎬 Video exported to: {outputFile}");
            UIManager.Instance?.ShowPopupText($"🎬 Exported: {outputFile}");
        }
        else
        {
            Debug.LogError($"❌ FFmpeg failed: {ffmpegOutput}");
            UIManager.Instance?.ShowPopupText($"❌ FFmpeg failed");
        }

        recordedFrames.Clear();
        OnExportProgress?.Invoke(1f);
        UIManager.Instance.IsExporting = true;
    }

}
