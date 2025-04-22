using Mirror;
using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public Transform cameraTransform;
    public Animator animator;

    [Header("Movement")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    private bool isGrounded;
    public bool IsMenuOpen = false;

    [Header("Camera")]
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private Camera firstPersonCamera;

    private CharacterController controller;
    private VideoRecorder videoRecorder;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        videoRecorder = GetComponent<VideoRecorder>();

        if (!isLocalPlayer) return;

        UIManager.Instance.HideMenu();
        UIManager.Instance.HidePopupText();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
            thirdPersonCamera = Camera.main;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            IsMenuOpen = !IsMenuOpen;

            Cursor.lockState = IsMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsMenuOpen;

            if (IsMenuOpen)
            {
                Action exitCallBack = isServer ? NetworkManager.singleton.StopHost : NetworkManager.singleton.StopClient;
                var exitMessage = isServer ? "Stop Host" : "Stop Client";

                UIManager.Instance.ShowMenu(videoRecorder.ExportVideo, exitCallBack, exitMessage);
            }
            else
            {
                UIManager.Instance.HideMenu();
            }
        }

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // แปะพื้นแน่นอน

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move;

        if (videoRecorder.IsInCameraView)
        {
            if (cameraTransform != firstPersonCamera.transform)
                cameraTransform = firstPersonCamera.transform;

            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0;
            right.Normalize();

            move = forward * v + right * h;

            if (move != Vector3.zero)
            {
                Vector3 lookDir = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
                transform.forward = lookDir;
            }
        }
        else
        {
            if (cameraTransform != thirdPersonCamera.transform)
                cameraTransform = thirdPersonCamera.transform;

            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            move = camForward * v + camRight * h;

            if (move != Vector3.zero)
            {
                transform.forward = move;
            }
        }

        controller.Move(move * moveSpeed * Time.deltaTime);

        // 🟩 กระโดด
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // ⬇️ แรงโน้มถ่วง
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
}
