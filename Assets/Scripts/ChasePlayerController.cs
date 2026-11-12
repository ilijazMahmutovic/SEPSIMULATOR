using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ChasePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.2f;
    public float sprintSpeed = 6.5f;
    public float gravity = -24f;
    public float mouseSensitivity = 2.0f;
    public float lookHeight = 1.65f;
    public float thirdPersonDistance = 4.2f;
    public float thirdPersonHeight = 0.15f;
    public float cameraSmooth = 14f;

    CharacterController controller;
    Transform model;
    Transform cameraPivot;
    Camera playerCamera;
    Renderer[] modelRenderers;
    bool[] modelRendererDefaults;
    float yaw;
    float pitch = 12f;
    float verticalVelocity;
    bool firstPerson;
    bool cursorCaptured = true;

    public Camera PlayerCamera => playerCamera;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        model = transform.Find("PlayerModel");
        modelRenderers = (model != null ? model : transform).GetComponentsInChildren<Renderer>(true);
        modelRendererDefaults = new bool[modelRenderers.Length];
        for (int i = 0; i < modelRenderers.Length; i++) modelRendererDefaults[i] = modelRenderers[i].enabled;
        yaw = transform.eulerAngles.y;
        BuildCamera();
        SetCursor(true);
    }

    void BuildCamera()
    {
        var pivotObject = new GameObject("PlayerCameraPivot");
        cameraPivot = pivotObject.transform;
        cameraPivot.SetParent(transform, false);
        cameraPivot.localPosition = Vector3.up * lookHeight;
        var cameraObject = new GameObject("PlayerCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraPivot, false);
        playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.nearClipPlane = 0.05f;
        cameraObject.AddComponent<AudioListener>();
        UpdateCameraMode(true);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursor(!cursorCaptured);
        }
        if (cursorCaptured && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            firstPerson = !firstPerson;
            UpdateCameraMode(false);
        }
        if (cursorCaptured && Mouse.current != null)
        {
            Vector2 look = Mouse.current.delta.ReadValue() * (mouseSensitivity * 0.08f);
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -25f, 65f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (!cursorCaptured || GameManager.GameOver) return;
        var keyboard = Keyboard.current;
        float horizontal = keyboard == null ? 0f : (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float vertical = keyboard == null ? 0f : (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move = Vector3.ClampMagnitude(move, 1f);
        bool sprint = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        float speed = sprint ? sprintSpeed : walkSpeed;
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (cameraPivot == null || playerCamera == null) return;
        Vector3 target = firstPerson
            ? cameraPivot.position
            : cameraPivot.position + cameraPivot.rotation * new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, target, 1f - Mathf.Exp(-cameraSmooth * Time.deltaTime));
        playerCamera.transform.rotation = cameraPivot.rotation;
    }

    void UpdateCameraMode(bool instant)
    {
        if (playerCamera == null) return;
        playerCamera.fieldOfView = firstPerson ? 82f : 70f;
        if (modelRenderers != null)
            for (int i = 0; i < modelRenderers.Length; i++) modelRenderers[i].enabled = !firstPerson && modelRendererDefaults[i];
        Vector3 target = firstPerson ? cameraPivot.position : cameraPivot.position + cameraPivot.rotation * new Vector3(0f, thirdPersonHeight, -thirdPersonDistance);
        if (instant) playerCamera.transform.position = target;
        playerCamera.transform.rotation = cameraPivot.rotation;
    }

    void SetCursor(bool captured)
    {
        cursorCaptured = captured;
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }
}




