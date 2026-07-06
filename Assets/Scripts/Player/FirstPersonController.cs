using UnityEngine;
using FirstGame.Core;
using FirstGame.Progression;

namespace FirstGame.Player
{
    /// <summary>
    /// CharacterController-based FPS movement. Reads explicit keycodes so it works on
    /// AZERTY (ZQSD) and QWERTY (WASD) and arrow keys without InputManager config.
    /// Exposes look/jump accumulators consumed by the tutorial.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float baseMoveSpeed = 6f;   // GDD base 6 m/s
        public float sprintMultiplier = 1.35f;
        public float jumpHeight = 1.2f;    // ~5 m/s impulse under g=-20
        public float gravity = -20f;

        [Header("Look")]
        public float mouseSensitivity = 2.0f;
        public Transform cameraPivot;

        CharacterController _cc;
        float _pitch;
        Vector3 _velocity;
        float _speedMul = 1f;

        public bool ControlEnabled = true;
        public float equipSpeedMul = 1f; // set by equipment (armour)
        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public float CurrentSpeed { get; private set; }

        // Tutorial instrumentation
        public float AccumYaw { get; private set; }
        public float AccumPitch { get; private set; }
        public int JumpCount { get; private set; }
        public void ResetLookAccumulators() { AccumYaw = 0; AccumPitch = 0; }

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _speedMul = PlayerProfile.Current.MoveSpeedMultiplier;
        }

        void Update()
        {
            if (!ControlEnabled) return;
            Look();
            Move();
        }

        void Look()
        {
            float sens = Settings.MouseSensitivity;
            float mx = Input.GetAxis("Mouse X") * sens;
            float my = Input.GetAxis("Mouse Y") * sens;

            AccumYaw += Mathf.Abs(mx);
            AccumPitch += Mathf.Abs(my);

            transform.Rotate(Vector3.up, mx);
            _pitch = Mathf.Clamp(_pitch - my, -89f, 89f);
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void Move()
        {
            float x = 0f, z = 0f;
            if (K(KeyCode.Z) || K(KeyCode.W) || K(KeyCode.UpArrow)) z += 1f;
            if (K(KeyCode.S) || K(KeyCode.DownArrow)) z -= 1f;
            if (K(KeyCode.Q) || K(KeyCode.A) || K(KeyCode.LeftArrow)) x -= 1f;
            if (K(KeyCode.D) || K(KeyCode.RightArrow)) x += 1f;

            Vector3 dir = transform.right * x + transform.forward * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = baseMoveSpeed * _speedMul * equipSpeedMul;
            if (Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

            Vector3 horizontal = dir * speed;
            CurrentSpeed = new Vector2(horizontal.x, horizontal.z).magnitude;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    JumpCount++;
                }
            }
            _velocity.y += gravity * Time.deltaTime;

            Vector3 motion = horizontal + Vector3.up * _velocity.y;
            _cc.Move(motion * Time.deltaTime);
        }

        static bool K(KeyCode k) => Input.GetKey(k);
    }
}
