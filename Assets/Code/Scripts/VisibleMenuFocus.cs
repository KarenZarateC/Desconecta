using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Desconecta
{
    /// <summary>Shows a thick focus border independently of hover and color changes.</summary>
    [RequireComponent(typeof(Outline))]
    public sealed class VisibleMenuFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        // Outline used as an explicit focus indicator independent of button hover colors.
        private Outline border;
        // Cache the required outline and keep it hidden until the element receives focus.
        private void Awake() { border=GetComponent<Outline>(); border.enabled=false; }
        /// <summary>Shows the focus border when keyboard or controller navigation selects this menu element.</summary>
        public void OnSelect(BaseEventData eventData) { border.enabled=true; }
        /// <summary>Hides the focus border when navigation moves to another menu element.</summary>
        public void OnDeselect(BaseEventData eventData) { border.enabled=false; }
    }
}
