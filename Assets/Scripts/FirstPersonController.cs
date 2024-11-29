using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Player Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -15f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 1f;
    public float maxLookAngle = 90f;

    private CharacterController characterController;
    private InputActions inputActions;
    private Vector3 velocity;
    private float cameraPitch = 0f;

    // Ajout pour fiabiliser le saut
    private bool isGrounded = false;
    private bool canJump = false;
    private float groundCheckDelay = 0.2f; // Délai de tolérance au sol
    private float lastGroundedTime;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new InputActions();
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
        HandleMovement();
        HandleCameraRotation();
        ApplyGravityAndJump();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = inputActions.Character.Move.ReadValue<Vector2>();
        bool isRunning = inputActions.Character.Run.IsPressed();
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 forward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        Vector3 right = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;
        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
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
        // Vérification au sol avec tolérance
        if (characterController.isGrounded)
        {
            isGrounded = true;
            lastGroundedTime = Time.time; // Enregistrer le dernier moment où le joueur était au sol
            velocity.y = -2f; // Petite vélocité descendante pour rester stable

            // Activer la possibilité de sauter
            canJump = true;
        }
        else
        {
            // Vérifier si le joueur était récemment au sol
            if (Time.time - lastGroundedTime <= groundCheckDelay)
            {
                isGrounded = true; // Toujours considéré au sol
            }
            else
            {
                isGrounded = false;
                canJump = false; // Empêcher de sauter en l'air
            }
        }

        // Gestion du saut
        if (canJump && inputActions.Character.Jump.triggered)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isGrounded = false; // Forcer l'état aérien
            canJump = false;    // Désactiver temporairement le saut
        }

        // Appliquer la gravité si le joueur est en l'air
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Appliquer le mouvement vertical
        characterController.Move(Vector3.up * velocity.y * Time.deltaTime);
    }
}
