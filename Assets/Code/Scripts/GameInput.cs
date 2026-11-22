using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Desconecta
{
    /// <summary>Provides a unified keyboard input layer that supports Unity's Input System and optional legacy input for assistive software compatibility.</summary>
    public static class GameInput
    {
        /// <summary>Returns whether a key is currently held in either the active input system or the legacy input manager.</summary>
        public static bool Held(ButtonControl key, KeyCode legacyKey)
        {
            bool active = key != null && key.isPressed;
#if ENABLE_LEGACY_INPUT_MANAGER
            active |= Input.GetKey(legacyKey);
#endif
            return active;
        }

        /// <summary>Returns whether a key was pressed during the current frame in either supported input system.</summary>
        public static bool Pressed(ButtonControl key, KeyCode legacyKey)
        {
            bool active = key != null && key.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            active |= Input.GetKeyDown(legacyKey);
#endif
            return active;
        }
    }
}
