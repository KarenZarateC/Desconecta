using UnityEngine;

namespace Desconecta
{
    /// <summary>Handles a single collectible coin, validates player pickups through the active game session, prevents collection through obstacles, and adapts its effects to the reduced-motion accessibility setting.</summary>
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class CollectibleCoin : MonoBehaviour
    {
        /// <summary>Identifies this coin in the collection progress system and links the scene object to its registered reward state.</summary>
        public string coinId;
        /// <summary>References the coin visual transform used to reset, animate, and hide the collectible during its pickup lifecycle.</summary>
        public Transform visual;
        /// <summary>Controls the coin's idle and collection animations according to its state, pause status, and accessibility settings.</summary>
        public Animation coinAnimation;
        /// <summary>Provides the optional particle effect displayed when the coin is successfully collected.</summary>
        public ParticleSystem pickupParticles;
        /// <summary>Defines how long the collection animation remains visible before the coin is hidden.</summary>
        public float pickupDuration = .48f;
        /// <summary>Indicates whether this coin has already been accepted by the active game session.</summary>
        public bool IsCollected { get; private set; }
        // Stores the collider used to detect the player and disable further pickup attempts.
        private SphereCollider pickupTrigger;
        // Accumulates collection animation time independently from the session timer.
        private float pickupElapsed;

        // Cache the required trigger collider before collision callbacks can occur.
        private void Awake() { pickupTrigger = GetComponent<SphereCollider>(); }
        // Try pickup on entry and stay because the player may approach through an existing overlap.
        private void OnTriggerEnter(Collider other) { TryPickup(other); }
        private void OnTriggerStay(Collider other) { TryPickup(other); }

        /// <summary>Attempts to validate and register a player pickup, then starts the appropriate visual feedback.</summary>
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

        /// <summary>Restores the coin to its available state and clears collection effects for a new run.</summary>
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

        // Keeps idle and collection effects synchronized with the session state and accessibility settings.
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
