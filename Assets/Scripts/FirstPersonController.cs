using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 1f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float maxLookAngle = 90f;

    private CharacterController characterController;
    private InputActions inputActions;
    private Vector3 velocity;
    private float cameraPitch = 0f;

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
        HandleLook();
        HandleGravity();
    }

    private void HandleMovement()
    {
        // Récupérer l'entrée pour les déplacements (ZQSD)
        Vector2 input = inputActions.Character.Move.ReadValue<Vector2>();

        // Calculer la direction du mouvement en utilisant la caméra, mais ignorer l'axe Y
        Vector3 forward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        Vector3 right = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;

        // Appliquer les directions au mouvement
        Vector3 move = forward * input.y + right * input.x;

        // Déplacer la capsule
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        // Gestion de la rotation de la caméra avec la souris
        Vector2 lookInput = inputActions.Character.Look.ReadValue<Vector2>() * mouseSensitivity;

        // Rotation horizontale du joueur
        float horizontalRotation = lookInput.x;
        transform.Rotate(Vector3.up * horizontalRotation); // Rotation sur l'axe Y seulement

        // Rotation verticale de la caméra
        cameraPitch -= lookInput.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle); // Limiter l'angle vertical
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            velocity.y = 0f;

            if (inputActions.Character.Jump.triggered)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
