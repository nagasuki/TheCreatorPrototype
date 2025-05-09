﻿using Mirror;
using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 12f;
    public Transform cameraTransform;
    public Animator animator;

    [Header("Movement")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isSprinting = false;
    public bool IsMenuOpen = false;

    [Header("Body Parts")]
    public GameObject[] availableCharacters;
    [SerializeField] private Transform bodyPoint;

    [Header("Camera")]
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private Camera firstPersonCamera;

    private CharacterController controller;
    private VideoRecorder videoRecorder;
    private ThirdPersonCamera thirdPersonCam;
    [SerializeField] private NetworkAnimator netAnimator;
    [SyncVar(hook = nameof(OnBodyPrefabChanged)), SerializeField] private int bodyPrefabIndex;

    [SyncVar] private bool isJumping = false;

    private GameObject body;

    void OnBodyPrefabChanged(int _, int newIndex)
    {
        SpawnBody(newIndex);
    }

    public override void OnStartClient()
    {
        SpawnBody(bodyPrefabIndex);

        if (!isLocalPlayer) return;

        UIManager.Instance.HideMenu();
        UIManager.Instance.HidePopupText();

        if (cameraTransform == null)
        {
            thirdPersonCamera = Camera.main;
            cameraTransform = Camera.main.transform;
        }

        Debug.Log($"Start First Person Camera: {firstPersonCamera.name} End!");
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        videoRecorder = GetComponent<VideoRecorder>();
        thirdPersonCam = GetComponent<ThirdPersonCamera>();

        //var hand = GameObject.Find()
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

                UIManager.Instance.ShowMenu(videoRecorder.ExportVideo, exitCallBack, thirdPersonCam.OnSensitivityChanged, exitMessage);
            }
            else
            {
                UIManager.Instance.HideMenu();
            }
        }

        if (IsMenuOpen) return;

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

            if (Input.GetKeyDown(KeyCode.LeftShift) && !videoRecorder.EquipCamera)
            {
                isSprinting = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift) && !videoRecorder.EquipCamera)
            {
                isSprinting = false;
            }
        }

        var speed = isSprinting ? runSpeed : moveSpeed;
        controller.Move(move * speed * Time.deltaTime);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
        }

        // เช็คว่า "กำลังตกหลังจากกระโดด" → จบ jump
        if (isJumping && velocity.y < 0 && !isGrounded)
        {
            isJumping = false;
        }

        // apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat("Speed", move.magnitude * moveSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("Jump", isJumping);
        animator.SetBool("Sprint", isSprinting);
    }

    void SpawnBody(int index)
    {
        if (body != null)
            Destroy(body);

        Debug.Log($"Spawning body with index {index}");

        body = Instantiate(availableCharacters[index], bodyPoint);
        body.transform.localPosition = Vector3.zero;

        animator = body.GetComponent<Animator>();

        if (netAnimator != null)
        {
            netAnimator.animator = animator;
            netAnimator.enabled = true;
        }
    }

    public void SetBodyIndex(int index)
    {
        bodyPrefabIndex = index;
    }
}
