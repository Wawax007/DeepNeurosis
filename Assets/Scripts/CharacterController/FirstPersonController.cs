using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Player Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 1.5f; // Hauteur du saut en mode marche
    public float runJumpHeight = 1f; // Hauteur du saut en mode course
    public float gravity = -20f; // Gravité modérée
    public float fallMultiplier = 2f; // Accélération pendant la descente

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 1f;
    public float maxLookAngle = 90f;

    private CharacterController characterController;
    private InputActions inputActions;
    private Vector3 velocity;
    private float cameraPitch = 0f;
    
    [Header("Footstep Manager")]
    public FootstepManager footstepManager;

    // Gestion du curseur
    private bool isCursorLocked = true;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new InputActions();
    }

    void Start()
    {
        LockCursor(); // Verrouiller le curseur dès le début
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        HandleCursorLock(); // Gérer le verrouillage du curseur

        if (isCursorLocked)
        {
            HandleMovement();
            HandleCameraRotation();
            ApplyGravityAndJump();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = inputActions.Character.Move.ReadValue<Vector2>();
        bool isRunning = inputActions.Character.Run.IsPressed();
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 forward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        Vector3 right = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;
        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;

        moveDirection.y = velocity.y;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Appeler le gestionnaire de sons si le joueur bouge et est au sol
        if (characterController.velocity.magnitude > 0.1f && characterController.isGrounded)
        {
            footstepManager.PlayFootstep(isRunning);
        }
    }

    private void HandleCameraRotation()
    {
        Vector2 lookInput = inputActions.Character.Look.ReadValue<Vector2>();
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    private void ApplyGravityAndJump()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded)
        {
            velocity.y = -2f; // Légère pression vers le bas pour stabiliser le joueur au sol

            if (inputActions.Character.Jump.triggered)
            {
                // Vérifier si le joueur court
                bool isRunning = inputActions.Character.Run.IsPressed();

                // Calculer la hauteur du saut en fonction de l'état
                if (isRunning)
                {
                    velocity.y = Mathf.Sqrt(runJumpHeight * -2f * gravity); // Saut plus bas
                }
                else
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Saut normal
                }
            }
        }
        else
        {
            if (velocity.y < 0)
            {
                // Appliquer une gravité amplifiée en descente
                velocity.y += gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                // Appliquer une gravité normale en montée
                velocity.y += gravity * Time.deltaTime;
            }
        }

        characterController.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // Verrouiller le curseur
        Cursor.visible = false; // Masquer le curseur
        isCursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None; // Déverrouiller le curseur
        Cursor.visible = true; // Rendre le curseur visible
        isCursorLocked = false;
    }

    private void HandleCursorLock()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isCursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
    }
}
