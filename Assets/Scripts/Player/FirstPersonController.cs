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
        public Camera cam;                 // set by PlayerRig; drives FOV + ADS zoom
        public float adsFovScale = 0.72f;  // FOV multiplier while aiming

        CharacterController _cc;
        float _pitch;
        float _recoil; // upward view kick from firing, recovers over time
        Vector3 _velocity;
        float _speedMul = 1f;

        /// <summary>Adds an upward recoil kick (degrees) to the view; recovers automatically.</summary>
        public void AddRecoil(float degrees) => _recoil += degrees;

        // Temporary speed buff (e.g. Brasier — Élan Ardent: +12% for 3s after a kill).
        float _buffMul = 1f;
        float _buffUntil = -1f;
        float BuffMul => Time.time < _buffUntil ? _buffMul : 1f;

        public bool ControlEnabled = true;
        public float equipSpeedMul = 1f; // set by equipment (armour)

        /// <summary>Apply a timed speed multiplier (latest call wins). Used by agent passives.</summary>
        public void AddSpeedBuff(float multiplier, float duration)
        {
            _buffMul = multiplier;
            _buffUntil = Time.time + duration;
        }
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

            // Re-assert the cursor lock every frame: in windowed mode Unity/Windows can silently
            // drop CursorLockMode.Locked (fast movement, focus quirks) and the cursor escapes.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            bool aiming = Keybinds.Held(GameAction.Aim);
            Look(aiming);
            Move();
            ApplyFov(aiming);
        }

        void Look(bool aiming)
        {
            float sens = Settings.MouseSensitivity * (aiming ? Settings.AdsSensMultiplier : 1f);
            float mx = Input.GetAxis("Mouse X") * sens;
            float my = Input.GetAxis("Mouse Y") * sens;
            if (Settings.InvertY) my = -my;

            AccumYaw += Mathf.Abs(mx);
            AccumPitch += Mathf.Abs(my);

            transform.Rotate(Vector3.up, mx);
            _pitch = Mathf.Clamp(_pitch - my, -89f, 89f);
            _recoil = Mathf.MoveTowards(_recoil, 0f, 14f * Time.deltaTime); // recover recoil
            if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(_pitch - _recoil, 0f, 0f);
        }

        void ApplyFov(bool aiming)
        {
            if (cam == null) return;
            float target = aiming ? Settings.FieldOfView * adsFovScale : Settings.FieldOfView;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, 0.35f);
        }

        void Move()
        {
            float x = 0f, z = 0f;
            // Rebindable keys (Keybinds) + arrow keys always available as a fallback.
            if (Keybinds.Held(GameAction.Forward) || K(KeyCode.UpArrow)) z += 1f;
            if (Keybinds.Held(GameAction.Back)    || K(KeyCode.DownArrow)) z -= 1f;
            if (Keybinds.Held(GameAction.Left)    || K(KeyCode.LeftArrow)) x -= 1f;
            if (Keybinds.Held(GameAction.Right)   || K(KeyCode.RightArrow)) x += 1f;

            Vector3 dir = transform.right * x + transform.forward * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = baseMoveSpeed * _speedMul * equipSpeedMul * BuffMul;
            if (Keybinds.Held(GameAction.Sprint)) speed *= sprintMultiplier;

            Vector3 horizontal = dir * speed;
            CurrentSpeed = new Vector2(horizontal.x, horizontal.z).magnitude;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0f) _velocity.y = -2f;
                if (Keybinds.Pressed(GameAction.Jump))
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
