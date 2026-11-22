using UnityEngine;

namespace Desconecta
{
    /// <summary>Represents an educational household decision and synchronizes its resolved state with scene feedback.</summary>
    public sealed class HouseholdSituation : MonoBehaviour
    {
        /// <summary>Unique identifier used by mission progress to record this situation once.</summary>
        public string situationId;
        /// <summary>Displayed number that lets the player identify this situation in the house.</summary>
        public int number;
        /// <summary>Short title shown in labels, focus feedback, and mission lists.</summary>
        public string shortLabel;
        /// <summary>Room or area used to describe the situation and provide hints.</summary>
        public string room;
        /// <summary>Question presented to the player before choosing an action.</summary>
        [TextArea] public string question;
        /// <summary>First available answer choice.</summary>
        public string optionA;
        /// <summary>Second available answer choice.</summary>
        public string optionB;
        /// <summary>Zero-based index of the answer that correctly resolves the situation.</summary>
        public int correctOption;
        /// <summary>Feedback shown after the player selects the correct action.</summary>
        [TextArea] public string successMessage;
        /// <summary>Feedback shown after an incorrect action, before the player tries again.</summary>
        [TextArea] public string retryMessage;
        /// <summary>Text alternative describing the situation's audio context.</summary>
        public string soundDescription;
        /// <summary>World-space label that faces the explorer and displays the situation state.</summary>
        public TextMesh label;
        /// <summary>Renderer whose color indicates whether the situation is pending or resolved.</summary>
        public Renderer statusRenderer;
        /// <summary>Optional visual effect active only while the situation remains unresolved.</summary>
        public GameObject activeEffect;
        /// <summary>Ambient audio associated with this situation.</summary>
        public AudioSource ambientSource;
        /// <summary>Optional light disabled after the situation is resolved.</summary>
        public Light controlledLight;
        /// <summary>Optional particles played when the situation is resolved.</summary>
        public ParticleSystem completionParticles;
        /// <summary>Indicates whether the player has already resolved this situation in the current run.</summary>
        public bool IsResolved { get; private set; }
        // Original label position used as the base for the idle bobbing animation.
        private Vector3 initialLabelPosition;

        // Cache the label position before the run can modify its presentation.
        private void Awake() { if (label != null) initialLabelPosition = label.transform.localPosition; }

        // Face the label toward the player and apply optional motion while the situation is pending.
        private void LateUpdate()
        {
            var session = GameSession.Instance;
            if (label == null || session == null || session.explorer == null) return;
            label.transform.rotation = session.explorer.viewCamera.transform.rotation;
            float bob = session.ReducedMotion || IsResolved ? 0 : Mathf.Sin(Time.time * 1.5f + number) * .035f;
            label.transform.localPosition = initialLabelPosition + Vector3.up * bob;
        }

        /// <summary>Marks the situation as resolved, updates its visuals, and plays completion feedback when allowed.</summary>
        public void Resolve()
        {
            IsResolved = true;
            RefreshVisuals();
            if (completionParticles != null && !GameSession.Instance.ReducedMotion) completionParticles.Play();
        }

        /// <summary>Restores the unresolved state and clears completion effects for a new run.</summary>
        public void ResetSituation()
        {
            IsResolved = false;
            if (completionParticles != null) completionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RefreshVisuals();
        }

        // Updates label text, status color, active effects, lighting, and ambient audio for the current state.
        private void RefreshVisuals()
        {
            if (label != null) label.text = number + " · " + shortLabel + (IsResolved ? "\nRESUELTO" : "\nREVISAR");
            if (statusRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                var color = IsResolved ? new Color(.15f,.5f,.36f) : new Color(.95f,.65f,.16f);
                block.SetColor("_BaseColor", color);
                statusRenderer.SetPropertyBlock(block);
            }
            if (activeEffect != null) activeEffect.SetActive(!IsResolved);
            if (controlledLight != null) controlledLight.enabled = !IsResolved;
            if (ambientSource != null && IsResolved) ambientSource.Stop();
        }

        /// <summary>Starts or pauses ambient audio according to play state and whether the situation is resolved.</summary>
        public void SetAmbientPlaying(bool playing)
        {
            if (ambientSource == null) return;
            if (playing && !IsResolved) { if (!ambientSource.isPlaying) ambientSource.Play(); }
            else ambientSource.Pause();
        }

        /// <summary>Applies the session effects volume to this situation's ambient audio.</summary>
        public void UpdateVolume(float volume) { if (ambientSource != null) ambientSource.volume = volume * .25f; }
    }
}
