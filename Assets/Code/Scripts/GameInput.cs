using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Desconecta
{
    /// <summary>Accepts native keys and legacy events from assistive input software.</summary>
    public static class GameInput
    {
        public static bool Held(ButtonControl key, KeyCode legacyKey)
        {
            bool active = key != null && key.isPressed;
#if ENABLE_LEGACY_INPUT_MANAGER
            active |= Input.GetKey(legacyKey);
#endif
            return active;
        }

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
