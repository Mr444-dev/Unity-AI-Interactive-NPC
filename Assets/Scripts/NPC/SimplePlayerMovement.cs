using UnityEngine;

namespace InteractiveNPC.NPC
{
    /// <summary>
    /// Simple keyboard WASD/arrow keys movement controller for testing the NPC interaction range.
    /// </summary>
    public class SimplePlayerMovement : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 720f;

        private CharacterController _characterController;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
            }
        }

        private void Update()
        {
            // Simple keyboard input (independent of new input system to avoid compilation/config dependencies)
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            if (direction.magnitude >= 0.1f)
            {
                // Calculate rotation angle based on input direction
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSpeed, 0.1f);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                _characterController.Move(moveDir.normalized * _moveSpeed * Time.deltaTime);
            }

            // Apply gravity
            if (!_characterController.isGrounded)
            {
                _characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
        }
    }
}
