using UnityEngine;

namespace BlockCrush.UI
{
    public enum GameSfx
    {
        BlockPlace,
        LineClear,
        MultipleLineClear,
        Combo,
        InvalidPlacement,
        ButtonClick,
        LevelComplete,
        GameOver
    }

    public sealed class GameAudioService : MonoBehaviour
    {
        [SerializeField]
        AudioSource soundSource, musicSource;
        [SerializeField]
        AudioSource accentSource;
        [SerializeField]
        AudioClip[] effects;
        int feedbackFrame = -1;
        bool soundPaused, accentPaused;
        public AudioSource AccentSource => accentSource;
        public AudioSource SoundSource => soundSource;
        public AudioSource MusicSource => musicSource;

        public AudioClip Clip(GameSfx cue) => effects != null && (int)cue < effects.Length ? effects[(int)cue] : null;
#if UNITY_EDITOR
        readonly int[] played = new int[8];
        public int Played(GameSfx cue) => played[(int)cue];
#endif
        public void Configure(AudioSource sound, AudioSource music)
        {
            soundSource = sound;
            musicSource = music;
        }

        public void ConfigureEffects(AudioSource accent, AudioClip[] clips)
        {
            accentSource = accent;
            effects = clips;
        }

        void Awake() => ApplyPreferences();
        public void ApplyPreferences()
        {
            soundSource.mute = !GamePreferences.Sound;
            if (accentSource)
                accentSource.mute = !GamePreferences.Sound;
            musicSource.mute = !GamePreferences.Bgm;
            if (!GamePreferences.Sound)
                StopEffects();
        }

        public void Feedback()
        {
            // Existing callbacks and saved button listeners may both request the same click.
            if (feedbackFrame == Time.frameCount)
                return;
            feedbackFrame = Time.frameCount;
            Play(GameSfx.ButtonClick);
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            if (GamePreferences.Vibration && Application.isMobilePlatform)
                Handheld.Vibrate();
#endif
        }

        public void Play(GameSfx cue, int combo = 1)
        {
            var clip = Clip(cue);
            if (!clip || !GamePreferences.Sound)
                return;
            if (cue == GameSfx.LevelComplete || cue == GameSfx.GameOver)
                StopEffects();
            // Two bounded voices: short place/UI/invalid/combo; clear/terminal accent.
            bool accent = cue == GameSfx.LineClear || cue == GameSfx.MultipleLineClear || cue == GameSfx.LevelComplete || cue == GameSfx.GameOver;
            var source = accent ? accentSource : soundSource;
            if (!source)
                return;
            source.Stop();
            source.clip = clip;
            source.volume = accent ? .5f : .55f;
            source.pitch = cue == GameSfx.Combo ? 1 + .045f * Mathf.Clamp(combo - 2, 0, 6) : 1;
            if (accent)
                accentPaused = false;
            else
                soundPaused = false;
            source.Play();
#if UNITY_EDITOR
            played[(int)cue]++;
#endif
        }

        public void StopEffects()
        {
            soundSource.Stop();
            if (accentSource)
                accentSource.Stop();
            soundPaused = accentPaused = false;
        }

        public void ResetGameplay()
        {
            // Keep only the click from the current Replay/Retry gesture.
            if (feedbackFrame != Time.frameCount || soundSource.clip != Clip(GameSfx.ButtonClick))
                soundSource.Stop();
            if (accentSource)
                accentSource.Stop();
            soundPaused = accentPaused = false;
        }

        public void PauseGameplay(bool pause)
        {
            PauseVoice(soundSource, pause, ref soundPaused);
            PauseVoice(accentSource, pause, ref accentPaused);
        }

        void PauseVoice(AudioSource source, bool pause, ref bool paused)
        {
            if (!source)
                return;
            bool gameplay = source.clip == Clip(GameSfx.BlockPlace) || source.clip == Clip(GameSfx.LineClear) || source.clip == Clip(GameSfx.MultipleLineClear) || source.clip == Clip(GameSfx.Combo);
            if (pause && gameplay && source.isPlaying)
            {
                source.Pause();
                paused = true;
            }
            else if (!pause && paused)
            {
                if (GamePreferences.Sound)
                    source.UnPause();
                paused = false;
            }
        }

        public void PlaySound(AudioClip clip)
        {
            if (!clip || !GamePreferences.Sound)
                return;
            soundSource.Stop();
            soundSource.clip = clip;
            soundSource.pitch = 1;
            soundSource.Play();
        }

        public void PlayMusic(AudioClip clip)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            if (clip)
                musicSource.Play();
            ApplyPreferences();
        }

        void OnDisable() => StopEffects();
    }
}
