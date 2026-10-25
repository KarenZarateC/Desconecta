using UnityEngine;
using UnityEngine.InputSystem;

namespace Desconecta
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonExplorer : MonoBehaviour
    {
        public Camera viewCamera;
        public float speed = 4f;
        public float interactionDistance = 3.8f;
        public Vector3 spawnPosition = new Vector3(5.5f, .1f, .75f);
        public float spawnYaw;
        public HouseholdSituation Focused { get; private set; }
        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Awake() { controller = GetComponent<CharacterController>(); }

        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0, spawnYaw, 0));
            controller.enabled = true;
            pitch = 0;
            verticalVelocity = 0;
            viewCamera.transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            var session = GameSession.Instance;
            if (session == null || session.Screen != SessionScreen.Playing) { Focused = null; return; }
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            Vector2 movement = Vector2.zero;
            float yawDelta = 0, pitchDelta = 0;
            if (keys != null)
            {
                movement.x = ((GameInput.Held(keys.dKey, KeyCode.D) || GameInput.Held(keys.rightArrowKey, KeyCode.RightArrow)) ? 1 : 0) - ((GameInput.Held(keys.aKey, KeyCode.A) || GameInput.Held(keys.leftArrowKey, KeyCode.LeftArrow)) ? 1 : 0);
                movement.y = ((GameInput.Held(keys.wKey, KeyCode.W) || GameInput.Held(keys.upArrowKey, KeyCode.UpArrow)) ? 1 : 0) - ((GameInput.Held(keys.sKey, KeyCode.S) || GameInput.Held(keys.downArrowKey, KeyCode.DownArrow)) ? 1 : 0);
                // Keyboard-only looking is an alternative to mouse movement.
                yawDelta = (GameInput.Held(keys.eKey, KeyCode.E) ? 1 : 0) - (GameInput.Held(keys.qKey, KeyCode.Q) ? 1 : 0);
                pitchDelta = (GameInput.Held(keys.fKey, KeyCode.F) ? 1 : 0) - (GameInput.Held(keys.rKey, KeyCode.R) ? 1 : 0);
            }
            Vector2 look = mouse != null ? mouse.delta.ReadValue() * (.08f + session.Sensitivity * .12f) : Vector2.zero;
            transform.Rotate(Vector3.up, look.x + yawDelta * 65f * Time.deltaTime);
            pitch = Mathf.Clamp(pitch - look.y + pitchDelta * 55f * Time.deltaTime, -70f, 70f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            movement = Vector2.ClampMagnitude(movement, 1);
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((transform.right * movement.x * speed + transform.forward * movement.y * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            if (transform.position.y < -4) ResetPosition();

            Focused = null;
            // Raycast against the whole scene so walls and furniture occlude targets.
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out var hit, interactionDistance, ~0, QueryTriggerInteraction.Collide))
                Focused = hit.collider.GetComponentInParent<HouseholdSituation>();
            if (Focused != null && Focused.IsResolved) Focused = null;
            bool interact = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (keys != null && (GameInput.Pressed(keys.enterKey, KeyCode.Return) || GameInput.Pressed(keys.spaceKey, KeyCode.Space)));
            if (Focused != null && interact) session.Inspect(Focused);
        }
    }
}
