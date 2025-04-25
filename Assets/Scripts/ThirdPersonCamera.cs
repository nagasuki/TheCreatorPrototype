using Mirror;
using UnityEngine;

public class ThirdPersonCamera : NetworkBehaviour
{
    public Transform cameraPivot; // จุดหมุนกล้อง (สร้างไว้บนตัวละคร เช่น บริเวณหัว)
    public Vector3 offset = new Vector3(0, 2f, -4f);
    public float rotationSpeed = 5f;
    public float pitchMin = -30f;
    public float pitchMax = 60f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 1.0f; // 👈 เพิ่มตรงนี้

    private float yaw;
    private float pitch;

    private Transform cam;
    private VideoRecorder videoRecorder;
    private PlayerController playerController;

    private void Awake()
    {
        videoRecorder = GetComponent<VideoRecorder>();
        playerController = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        cam = Camera.main.transform;

        if (cam == null)
        {
            Debug.LogWarning("No Main Camera found in scene!");
            return;
        }

        cam.position = cameraPivot.position + Quaternion.Euler(pitch, yaw, 0) * offset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isLocalPlayer || cam == null || playerController.IsMenuOpen) return;

        if (videoRecorder.IsInCameraView)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || cam == null || playerController.IsMenuOpen) return;

        if (!videoRecorder.IsInCameraView)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 desiredPosition = cameraPivot.position + rotation * offset;

            cam.position = desiredPosition;
            cam.LookAt(cameraPivot.position + Vector3.up * 1f);
        }
    }

    public void OnSensitivityChanged(float value)
    {
        mouseSensitivity = value;
    }
}

