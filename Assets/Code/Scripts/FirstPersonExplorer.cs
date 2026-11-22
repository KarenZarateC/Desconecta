using UnityEngine;
using UnityEngine.InputSystem;

namespace Desconecta
{
    /// <summary>Controls first-person exploration, camera look, and line-of-sight interaction with household situations.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonExplorer : MonoBehaviour
    {
        /// <summary>Camera used as the origin and direction for looking at interactive situations.</summary>
        public Camera viewCamera;
        /// <summary>Movement speed applied while the session is in the playing state.</summary>
        public float speed = 4f;
        /// <summary>Maximum distance at which the camera can detect a household situation.</summary>
        public float interactionDistance = 3.8f;
        /// <summary>World position restored when a run starts or the explorer falls out of the level.</summary>
        public Vector3 spawnPosition = new Vector3(5.5f, .1f, .75f);
        /// <summary>Initial horizontal camera orientation restored with the spawn position.</summary>
        public float spawnYaw;
        /// <summary>Situation currently targeted by the camera and eligible for inspection.</summary>
        public HouseholdSituation Focused { get; private set; }
        // Required controller used for collision-aware movement and grounding checks.
        private CharacterController controller;
        // Vertical camera rotation constrained to avoid overturning the first-person view.
        private float pitch;
        // Current vertical movement, including gravity and the grounded reset.
        private float verticalVelocity;

        // Cache the required controller before movement begins.
        private void Awake() { controller = GetComponent<CharacterController>(); }

        /// <summary>Restores the explorer position, orientation, camera pitch, and vertical motion for a new run.</summary>
        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0, spawnYaw, 0));
            controller.enabled = true;
            pitch = 0;
            verticalVelocity = 0;
            viewCamera.transform.localRotation = Quaternion.identity;
        }

        // Reads movement and look input only during active play, then resolves the visible interaction target.
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
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out var hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                Focused = hit.collider.GetComponentInParent<HouseholdSituation>();
            if (Focused != null && Focused.IsResolved) Focused = null;
            bool interact = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (keys != null && (GameInput.Pressed(keys.enterKey, KeyCode.Return) || GameInput.Pressed(keys.spaceKey, KeyCode.Space)));
            if (Focused != null && interact) session.Inspect(Focused);
        }
    }
}
