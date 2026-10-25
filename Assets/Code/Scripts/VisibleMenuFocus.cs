using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Desconecta
{
    /// <summary>Shows a thick focus border independently of hover and color changes.</summary>
    [RequireComponent(typeof(Outline))]
    public sealed class VisibleMenuFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline border;
        private void Awake() { border=GetComponent<Outline>(); border.enabled=false; }
        public void OnSelect(BaseEventData eventData) { border.enabled=true; }
        public void OnDeselect(BaseEventData eventData) { border.enabled=false; }
    }
}
