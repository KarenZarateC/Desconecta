using UnityEngine;
using UnityEngine.InputSystem;

namespace Desconecta
{
    public enum SessionScreen { Home, Playing, Decision, Paused, Results }

    /// <summary>Coordinates the seven household situations and redundant feedback.</summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public HouseholdSituation[] situations;
        public FirstPersonExplorer explorer;
        public GameInterface interfaceView;
        public AudioSource musicSource;
        public AudioSource feedbackSource;
        public AudioClip correctClip;
        public AudioClip incorrectClip;
        public AudioClip victoryClip;
        public AudioClip timeoutClip;
        public SessionScreen Screen { get; private set; } = SessionScreen.Home;
        public MissionProgress Progress { get; private set; }
        public HouseholdSituation Selected { get; private set; }
        public string Caption { get; private set; } = "[Música ambiental suave]";
        public string Feedback { get; private set; } = "Encuentra las 7 situaciones y cuida tu hogar.";
        public bool LargeText { get; private set; }
        public bool ReducedMotion { get; private set; }
        public bool UnlimitedTime { get; private set; }
        public bool Muted { get; private set; }
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public float Sensitivity { get; private set; }
        public float CaptionUntil { get; private set; }

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
            ApplyAudio();
        }

        private void Start() { SetScreen(SessionScreen.Home); }

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

        public void Begin()
        {
            Progress = new MissionProgress(situations.Length) { Unlimited = UnlimitedTime };
            foreach (var situation in situations) situation.ResetSituation();
            explorer.ResetPosition();
            Feedback = "Recorre la casa. Mira un marcador y pulsa Enter, Espacio o clic.";
            Caption = "[Música ambiental suave]";
            Selected = null;
            SetScreen(SessionScreen.Playing);
        }

        public void Inspect(HouseholdSituation situation)
        {
            if (Screen != SessionScreen.Playing || situation == null || situation.IsResolved) return;
            Selected = situation;
            SetScreen(SessionScreen.Decision);
        }

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
        public void Pause() { if (Screen == SessionScreen.Playing) SetScreen(SessionScreen.Paused); }
        public void Home() { SetScreen(SessionScreen.Home); }

        private void Finish(bool won)
        {
            Announce(won ? "[Melodía de logro] Completaste las 7 situaciones." : "[Aviso de fin] El tiempo terminó; puedes volver a intentarlo.", won ? victoryClip : timeoutClip);
            SetScreen(SessionScreen.Results);
        }

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

        public void Announce(string description, AudioClip clip)
        {
            Caption = description;
            CaptionUntil = Time.unscaledTime + 8f;
            if (clip != null && !Muted) feedbackSource.PlayOneShot(clip, EffectsVolume);
        }

        private void SetScreen(SessionScreen next)
        {
            Screen = next;
            bool playing = next == SessionScreen.Playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            foreach (var situation in situations) situation.SetAmbientPlaying(playing);
            if (interfaceView != null) interfaceView.RefreshScreen();
        }

        public void SetLargeText(bool value) { LargeText = value; SaveOptions(); interfaceView.RefreshScreen(); }
        public void SetReducedMotion(bool value) { ReducedMotion = value; SaveOptions(); }
        public void SetUnlimited(bool value) { UnlimitedTime = value; Progress.Unlimited = value; SaveOptions(); }
        public void SetMuted(bool value) { Muted = value; ApplyAudio(); SaveOptions(); }
        public void SetMusic(float value) { MusicVolume = value; ApplyAudio(); SaveOptions(); }
        public void SetEffects(float value) { EffectsVolume = value; ApplyAudio(); SaveOptions(); }
        public void SetSensitivity(float value) { Sensitivity = value; SaveOptions(); }

        private void ApplyAudio()
        {
            if (musicSource != null) musicSource.volume = Muted ? 0 : MusicVolume;
            if (feedbackSource != null) feedbackSource.mute = Muted;
            foreach (var situation in situations) if (situation != null) situation.UpdateVolume(Muted ? 0 : EffectsVolume);
        }

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

        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Instance == this) Instance = null;
        }
    }
}
