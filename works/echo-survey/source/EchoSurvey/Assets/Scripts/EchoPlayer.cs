using UnityEngine;

namespace EchoSurvey
{
    [RequireComponent(typeof(CharacterController))]
    public class EchoPlayer : MonoBehaviour
    {
        public Camera Cam;
        public float EyeHeight = 1.62f;
        public float MoveSpeed = 4.1f;
        public float SlowSpeed = 2.0f;

        private CharacterController _cc;
        private float _yaw;
        private float _pitch;
        private float _vertical;
        private float _pulseCooldown;
        private float _bob;
        private float _pulseKick;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cc.height = 1.72f;
            _cc.radius = 0.34f;
            _cc.center = new Vector3(0, 0.88f, 0);
            _cc.stepOffset = 0.38f;
            _cc.slopeLimit = 48f;

            var camGo = new GameObject("EchoCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0, EyeHeight, 0);
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.006f, 0.014f, 0.018f);
            Cam.nearClipPlane = 0.035f;
            Cam.farClipPlane = 130f;
            Cam.fieldOfView = 64f;
            Cam.allowHDR = false;
            Cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (EchoGame.I == null) return;
            if (!EchoGame.I.Started)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (EchoGame.I.Complete) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EchoGame.I.TogglePause();
            }
            if (EchoGame.I.Paused) return;

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Look();
            Move();
            Pulse();
        }

        private void Look()
        {
            _yaw += Input.GetAxisRaw("Mouse X") * 2.05f;
            _pitch -= Input.GetAxisRaw("Mouse Y") * 2.05f;
            _pitch = Mathf.Clamp(_pitch, -82f, 82f);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            Cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        private void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            bool slow = Input.GetKey(KeyCode.LeftShift);
            var input = new Vector3(h, 0, v);
            if (input.sqrMagnitude > 1f) input.Normalize();
            var move = transform.right * input.x + transform.forward * input.z;

            if (_cc.isGrounded && _vertical < 0) _vertical = -2f;
            _vertical += Physics.gravity.y * Time.deltaTime;
            _cc.Move((move * (slow ? SlowSpeed : MoveSpeed) + Vector3.up * _vertical) * Time.deltaTime);

            float targetBob = _cc.isGrounded && input.sqrMagnitude > .05f ? Mathf.Sin(Time.time * (slow ? 5f : 8f)) * .025f : 0f;
            _bob = Mathf.Lerp(_bob, targetBob, Time.deltaTime * 8f);
            _pulseKick = Mathf.MoveTowards(_pulseKick, 0f, Time.deltaTime * 3.2f);
            Cam.transform.localPosition = new Vector3(0, EyeHeight + _bob, 0);
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, 64f + _pulseKick, Time.deltaTime * 7f);
        }

        private void Pulse()
        {
            _pulseCooldown -= Time.deltaTime;
            if (_pulseCooldown > 0f) return;
            if (!Input.GetMouseButtonDown(0)) return;
            _pulseCooldown = 0.52f;
            _pulseKick = 3.2f;
            EchoGame.I.EmitPulse(transform.position + Vector3.up * 1.0f);
        }

        public void ResetPlayer(Vector3 position, float yaw)
        {
            _cc.enabled = false;
            transform.position = position;
            _yaw = yaw;
            _pitch = 0;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (Cam != null) Cam.transform.localRotation = Quaternion.identity;
            _cc.enabled = true;
            _vertical = -2f;
        }
    }
}
