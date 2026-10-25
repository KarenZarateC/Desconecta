using UnityEngine;

namespace Desconecta
{
    public sealed class HouseholdSituation : MonoBehaviour
    {
        public string situationId;
        public int number;
        public string shortLabel;
        public string room;
        [TextArea] public string question;
        public string optionA;
        public string optionB;
        public int correctOption;
        [TextArea] public string successMessage;
        [TextArea] public string retryMessage;
        public string soundDescription;
        public TextMesh label;
        public Renderer statusRenderer;
        public GameObject activeEffect;
        public AudioSource ambientSource;
        public Light controlledLight;
        public ParticleSystem completionParticles;
        public bool IsResolved { get; private set; }
        private Vector3 initialLabelPosition;

        private void Awake() { if (label != null) initialLabelPosition = label.transform.localPosition; }

        private void LateUpdate()
        {
            var session = GameSession.Instance;
            if (label == null || session == null || session.explorer == null) return;
            label.transform.rotation = session.explorer.viewCamera.transform.rotation;
            float bob = session.ReducedMotion || IsResolved ? 0 : Mathf.Sin(Time.time * 1.5f + number) * .035f;
            label.transform.localPosition = initialLabelPosition + Vector3.up * bob;
        }

        public void Resolve()
        {
            IsResolved = true;
            RefreshVisuals();
            if (completionParticles != null && !GameSession.Instance.ReducedMotion) completionParticles.Play();
        }

        public void ResetSituation()
        {
            IsResolved = false;
            if (completionParticles != null) completionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RefreshVisuals();
        }

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

        public void SetAmbientPlaying(bool playing)
        {
            if (ambientSource == null) return;
            if (playing && !IsResolved) { if (!ambientSource.isPlaying) ambientSource.Play(); }
            else ambientSource.Pause();
        }

        public void UpdateVolume(float volume) { if (ambientSource != null) ambientSource.volume = volume * .25f; }
    }
}
