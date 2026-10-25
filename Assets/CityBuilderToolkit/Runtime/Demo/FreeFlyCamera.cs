using UnityEngine;

namespace CityBuilderToolkit.Demo
{
    /// <summary>
    /// Simple, smooth flythrough camera controller for inspecting procedural cities in Play mode.
    /// WASD to move, Space/Ctrl to elevate, Right-Click drag or Mouse to look around.
    /// </summary>
    public class FreeFlyCamera : MonoBehaviour
    {
        public float moveSpeed = 25f;
        public float fastMoveFactor = 3f;
        public float lookSensitivity = 3f;

        private float _rotationX = 0f;
        private float _rotationY = 0f;

        private void Start()
        {
            Vector3 rot = transform.localEulerAngles;
            _rotationX = rot.y;
            _rotationY = -rot.x;
        }

        private void Update()
        {
            // Mouse Look (Hold Right-Click or Left-Click)
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
            {
                _rotationX += Input.GetAxis("Mouse X") * lookSensitivity;
                _rotationY += Input.GetAxis("Mouse Y") * lookSensitivity;
                _rotationY = Mathf.Clamp(_rotationY, -85f, 85f);

                transform.localRotation = Quaternion.AngleAxis(_rotationX, Vector3.up);
                transform.localRotation *= Quaternion.AngleAxis(_rotationY, Vector3.left);
            }

            // Movement
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMoveFactor : 1f);

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

            transform.position += move.normalized * (speed * Time.deltaTime);
        }
    }
}
