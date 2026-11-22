using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

namespace Desconecta
{
    /// <summary>Identifies the active screen and determines which gameplay systems should respond to input.</summary>
    public enum SessionScreen { Home, Playing, Decision, Paused, Results }

    /// <summary>Coordinates mission state, exploration, collectible coins, interface updates, audio feedback, and accessibility preferences.</summary>
    public sealed class GameSession : MonoBehaviour
    {
        /// <summary>Provides the scene-wide session instance used by gameplay components.</summary>
        public static GameSession Instance { get; private set; }
        /// <summary>Situations the player must inspect and resolve during the mission.</summary>
        public HouseholdSituation[] situations;
        /// <summary>Explorer controlled during the active gameplay screen.</summary>
        public FirstPersonExplorer explorer;
        /// <summary>Interface refreshed whenever session state or settings change.</summary>
        public GameInterface interfaceView;
        /// <summary>Audio source used for ambient music.</summary>
        public AudioSource musicSource;
        /// <summary>Audio source used for narrated feedback and gameplay effects.</summary>
        public AudioSource feedbackSource;
        /// <summary>Sound played after a correct situation decision.</summary>
        public AudioClip correctClip;
        /// <summary>Sound played after an incorrect situation decision.</summary>
        public AudioClip incorrectClip;
        /// <summary>Sound played when the mission is completed successfully.</summary>
        public AudioClip victoryClip;
        /// <summary>Sound played when the mission timer expires.</summary>
        public AudioClip timeoutClip;
        /// <summary>Coins registered for this scene and validated before they can reward the player.</summary>
        public CollectibleCoin[] coins = new CollectibleCoin[0];
        /// <summary>Sound played when a registered coin is collected.</summary>
        public AudioClip coinClip;
        /// <summary>Tracks the unique coin collection state for the current run.</summary>
        public CoinCollectionProgress Coins { get; private set; }
        /// <summary>Combines mission points with the bonus earned from collected coins.</summary>
        public int TotalScore => Progress.Score + (Coins != null ? Coins.BonusScore : 0);
        /// <summary>Current screen that controls available input and interface content.</summary>
        public SessionScreen Screen { get; private set; } = SessionScreen.Home;
        /// <summary>Tracks resolved situations, score, timer, and mission completion.</summary>
        public MissionProgress Progress { get; private set; }
        /// <summary>Situation currently displayed for the player's decision.</summary>
        public HouseholdSituation Selected { get; private set; }
        /// <summary>Latest audio description shown to the player as text.</summary>
        public string Caption { get; private set; } = "[Música ambiental suave]";
        /// <summary>Latest textual feedback describing progress, rewards, or mistakes.</summary>
        public string Feedback { get; private set; } = "Encuentra las 7 situaciones y cuida tu hogar.";
        /// <summary>Enables the larger interface text accessibility option.</summary>
        public bool LargeText { get; private set; }
        /// <summary>Disables motion-heavy visual effects when enabled.</summary>
        public bool ReducedMotion { get; private set; }
        /// <summary>Prevents the mission timer from expiring when enabled.</summary>
        public bool UnlimitedTime { get; private set; }
        /// <summary>Disables music and feedback audio when enabled.</summary>
        public bool Muted { get; private set; }
        /// <summary>Current persisted music volume.</summary>
        public float MusicVolume { get; private set; }
        /// <summary>Current persisted effects and ambient situation volume.</summary>
        public float EffectsVolume { get; private set; }
        /// <summary>Current persisted mouse look sensitivity.</summary>
        public float Sensitivity { get; private set; }
        /// <summary>Unscaled time until the current caption should remain prominent.</summary>
        public float CaptionUntil { get; private set; }

        // Establishes the shared session, restores preferences, creates progress trackers, and applies audio settings.
        private void Awake()
        {
            Instance = this;
            LargeText = PlayerPrefs.GetInt("Desconecta.LargeText", 0) == 1;
            ReducedMotion = PlayerPrefs.GetInt("Desconecta.ReducedMotion", 0) == 1;
            UnlimitedTime = PlayerPrefs.GetInt("Desconecta.Unlimited", 0) == 1;
            Muted = PlayerPrefs.GetInt("Desconecta.Muted", 0) == 1;
            MusicVolume = PlayerPrefs.GetFloat("Desconecta.Music", .25f);
            EffectsVolume = PlayerPrefs.GetFloat("Desconecta.Effects", .65f);
            Sensitivity = PlayerPrefs.GetFloat("Desconecta.Sensitivity", .7f);
            Progress = new MissionProgress(situations.Length) { Unlimited = UnlimitedTime };
            Coins = new CoinCollectionProgress(coins.Where(coin => coin != null).Select(coin => coin.coinId));
            ApplyAudio();
        }

        // Displays the initial home screen after all scene components have awakened.
        private void Start() { SetScreen(SessionScreen.Home); }

        // Advances mission time, handles global keyboard commands, and routes shortcut actions.
        private void Update()
        {
            Progress.Tick(Time.deltaTime, Screen == SessionScreen.Playing);
            if (Screen == SessionScreen.Playing && Progress.Expired) Finish(false);
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (GameInput.Pressed(keyboard.escapeKey, KeyCode.Escape))
            {
                if (Screen == SessionScreen.Playing) SetScreen(SessionScreen.Paused);
                else if (Screen == SessionScreen.Decision || Screen == SessionScreen.Paused) Resume();
            }
            if (Screen == SessionScreen.Decision)
            {
                if (GameInput.Pressed(keyboard.digit1Key, KeyCode.Alpha1)) Answer(0);
                else if (GameInput.Pressed(keyboard.digit2Key, KeyCode.Alpha2)) Answer(1);
            }
            if (Screen == SessionScreen.Playing && GameInput.Pressed(keyboard.hKey, KeyCode.H)) ShowHint();
        }

        /// <summary>Starts a fresh run by resetting situations, coins, explorer position, feedback, and mission progress.</summary>
        public void Begin()
        {
            Progress = new MissionProgress(situations.Length) { Unlimited = UnlimitedTime };
            foreach (var situation in situations) situation.ResetSituation();
            Coins.Reset();
            foreach (var coin in coins) if (coin != null) coin.ResetCoin();
            explorer.ResetPosition();
            Feedback = "Sigue las monedas: recógelas al caminar (+10). Revisa los objetivos con Enter, Espacio o clic.";
            Caption = "[Música ambiental suave]";
            Selected = null;
            SetScreen(SessionScreen.Playing);
        }

        /// <summary>Opens a decision screen for an unresolved situation while the player is exploring.</summary>
        public void Inspect(HouseholdSituation situation)
        {
            if (Screen != SessionScreen.Playing || situation == null || situation.IsResolved) return;
            Selected = situation;
            SetScreen(SessionScreen.Decision);
        }

        /// <summary>Evaluates the selected answer, updates score and feedback, and returns whether it was correct.</summary>
        public bool Answer(int option)
        {
            if (Screen != SessionScreen.Decision || Selected == null) return false;
            bool correct = option == Selected.correctOption;
            if (correct)
            {
                if (!Progress.Resolve(Selected.situationId)) return false;
                Selected.Resolve();
                Feedback = "+100 puntos · " + Selected.successMessage;
                Announce("[Campanilla de acierto] " + Selected.shortLabel + ": resuelto.", correctClip);
                SetScreen(SessionScreen.Playing);
                if (Progress.Won) Finish(true);
            }
            else
            {
                Progress.Penalize();
                Feedback = "−25 puntos · " + Selected.retryMessage;
                Announce("[Tono suave de aviso] Revisa la situación y prueba otra vez.", incorrectClip);
                interfaceView.RefreshScreen();
            }
            return correct;
        }

        public void Resume() { SetScreen(SessionScreen.Playing); }

        public bool CollectCoin(CollectibleCoin coin)
        {
            if (Screen != SessionScreen.Playing || Progress.Finished || coin == null ||
                !System.Array.Exists(coins, registered => registered == coin) || !Coins.TryCollect(coin.coinId)) return false;
            Feedback = Coins.Complete ? "¡Todas las monedas! Continúa cuidando el hogar." : "+10 puntos · Moneda " + Coins.Collected + " de " + Coins.Total + ".";
            Announce("[Sonido de moneda] " + Coins.Collected + " / " + Coins.Total + " monedas recolectadas.", coinClip);
            return true;
        }
        /// <summary>Pauses the active run without advancing mission time.</summary>
        public void Pause() { if (Screen == SessionScreen.Playing) SetScreen(SessionScreen.Paused); }
        /// <summary>Returns to the home screen and exits active exploration.</summary>
        public void Home() { SetScreen(SessionScreen.Home); }

        // Announces the outcome and transitions to the results screen.
        private void Finish(bool won)
        {
            Announce(won ? "[Melodía de logro] Completaste las 7 situaciones." : "[Aviso de fin] El tiempo terminó; puedes volver a intentarlo.", won ? victoryClip : timeoutClip);
            SetScreen(SessionScreen.Results);
        }

        /// <summary>Publishes a timed hint pointing the player toward the first unresolved situation.</summary>
        public void ShowHint()
        {
            foreach (var situation in situations)
            {
                if (situation.IsResolved) continue;
                Feedback = "Pista: " + situation.room + " · " + situation.shortLabel + ". Sigue su número en la casa.";
                CaptionUntil = Time.unscaledTime + 12f;
                return;
            }
        }

        /// <summary>Displays an audio description as text and optionally plays its sound when effects are enabled.</summary>
        public void Announce(string description, AudioClip clip)
        {
            Caption = description;
            CaptionUntil = Time.unscaledTime + 8f;
            if (clip != null && !Muted) feedbackSource.PlayOneShot(clip, EffectsVolume);
        }

        // Synchronizes cursor state, ambient sounds, and the generated interface with the new screen.
        private void SetScreen(SessionScreen next)
        {
            Screen = next;
            bool playing = next == SessionScreen.Playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            foreach (var situation in situations) situation.SetAmbientPlaying(playing);
            if (interfaceView != null) interfaceView.RefreshScreen();
        }

        /// <summary>Updates and persists the large-text accessibility setting.</summary>
        public void SetLargeText(bool value) { LargeText = value; SaveOptions(); interfaceView.RefreshScreen(); }
        /// <summary>Updates and persists the reduced-motion accessibility setting.</summary>
        public void SetReducedMotion(bool value) { ReducedMotion = value; SaveOptions(); }
        /// <summary>Updates and persists whether the mission timer is unlimited.</summary>
        public void SetUnlimited(bool value) { UnlimitedTime = value; Progress.Unlimited = value; SaveOptions(); }
        /// <summary>Updates the mute state, applies it to audio sources, and persists the setting.</summary>
        public void SetMuted(bool value) { Muted = value; ApplyAudio(); SaveOptions(); }
        /// <summary>Updates, applies, and persists the music volume.</summary>
        public void SetMusic(float value) { MusicVolume = value; ApplyAudio(); SaveOptions(); }
        /// <summary>Updates, applies, and persists the effects volume.</summary>
        public void SetEffects(float value) { EffectsVolume = value; ApplyAudio(); SaveOptions(); }
        /// <summary>Updates and persists the mouse look sensitivity.</summary>
        public void SetSensitivity(float value) { Sensitivity = value; SaveOptions(); }

        // Applies mute and volume settings to music, feedback, and household ambience.
        private void ApplyAudio()
        {
            if (musicSource != null) musicSource.volume = Muted ? 0 : MusicVolume;
            if (feedbackSource != null) feedbackSource.mute = Muted;
            foreach (var situation in situations) if (situation != null) situation.UpdateVolume(Muted ? 0 : EffectsVolume);
        }

        // Persists accessibility, audio, and sensitivity settings for the next session.
        private void SaveOptions()
        {
            PlayerPrefs.SetInt("Desconecta.LargeText", LargeText ? 1 : 0);
            PlayerPrefs.SetInt("Desconecta.ReducedMotion", ReducedMotion ? 1 : 0);
            PlayerPrefs.SetInt("Desconecta.Unlimited", UnlimitedTime ? 1 : 0);
            PlayerPrefs.SetInt("Desconecta.Muted", Muted ? 1 : 0);
            PlayerPrefs.SetFloat("Desconecta.Music", MusicVolume);
            PlayerPrefs.SetFloat("Desconecta.Effects", EffectsVolume);
            PlayerPrefs.SetFloat("Desconecta.Sensitivity", Sensitivity);
            PlayerPrefs.Save();
        }

        // Releases cursor state and the singleton reference when the session object is destroyed.
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Instance == this) Instance = null;
        }
    }
}
