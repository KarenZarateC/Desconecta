using UnityEngine;

namespace Desconecta
{
    /// <summary>One-shot pickup; spinning and pickup motion respect pause and accessibility.</summary>
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class CollectibleCoin : MonoBehaviour
    {
        public string coinId;
        public Transform visual;
        public Animation coinAnimation;
        public ParticleSystem pickupParticles;
        public float pickupDuration = .48f;
        public bool IsCollected { get; private set; }
        private SphereCollider pickupTrigger;
        private float pickupElapsed;

        private void Awake() { pickupTrigger = GetComponent<SphereCollider>(); }
        private void OnTriggerEnter(Collider other) { TryPickup(other); }
        private void OnTriggerStay(Collider other) { TryPickup(other); }

        public bool TryPickup(Collider other)
        {
            var session = GameSession.Instance;
            var explorer = other != null ? other.GetComponentInParent<FirstPersonExplorer>() : null;
            if (IsCollected || session == null || explorer == null || explorer != session.explorer) return false;
            // A nearby coin behind a wall must not be collected through that wall.
            Vector3 playerCenter = explorer.transform.position + Vector3.up * .9f;
            var controller = explorer.GetComponent<CharacterController>();
            if (Vector3.Distance(transform.position, playerCenter) > pickupTrigger.radius + controller.radius + .05f) return false;
            if (Physics.Linecast(transform.position, playerCenter, out var obstruction,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                obstruction.collider.GetComponentInParent<FirstPersonExplorer>() != explorer) return false;
            if (!session.CollectCoin(this)) return false;
            IsCollected = true;
            pickupTrigger.enabled = false;
            pickupElapsed = 0;
            coinAnimation.Stop();
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
            if (session.ReducedMotion) visual.gameObject.SetActive(false);
            else
            {
                coinAnimation.Play("CoinCollect");
                if (pickupParticles != null) pickupParticles.Play();
            }
            return true;
        }

        public void ResetCoin()
        {
            IsCollected = false;
            pickupElapsed = 0;
            pickupTrigger = GetComponent<SphereCollider>();
            pickupTrigger.enabled = true;
            coinAnimation.Stop();
            visual.localPosition = Vector3.zero;
            visual.localScale = Vector3.one;
            visual.localRotation = Quaternion.identity;
            visual.gameObject.SetActive(true);
            if (pickupParticles != null) pickupParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            var session = GameSession.Instance;
            bool playing = session != null && session.Screen == SessionScreen.Playing;
            coinAnimation[IsCollected ? "CoinCollect" : "CoinSpin"].speed = playing && !session.ReducedMotion ? 1 : 0;
            if (!IsCollected)
            {
                if (session != null && session.ReducedMotion)
                {
                    coinAnimation.Stop();
                    visual.localRotation = Quaternion.identity;
                    visual.localPosition = Vector3.zero;
                }
                else if (!coinAnimation.IsPlaying("CoinSpin")) coinAnimation.Play("CoinSpin");
                return;
            }
            if (pickupParticles != null)
            {
                if (!playing && pickupParticles.isPlaying) pickupParticles.Pause();
                else if (playing && pickupParticles.isPaused) pickupParticles.Play();
            }
            if (!playing) return;
            pickupElapsed += Time.deltaTime;
            if (pickupElapsed >= pickupDuration || session.ReducedMotion) visual.gameObject.SetActive(false);
        }
    }
}
