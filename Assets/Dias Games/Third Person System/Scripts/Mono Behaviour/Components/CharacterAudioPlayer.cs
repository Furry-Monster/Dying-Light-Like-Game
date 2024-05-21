using UnityEngine;

namespace DiasGames.Components
{
    public class CharacterAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioSource effectsSource;

        public void PlayVoice(AudioClip clip)
        {
            if (voiceSource == null) return;

            voiceSource.clip = clip;
            voiceSource.Play();
        }
        public void PlayVoice(AudioClip[] clips)
        {
            if (voiceSource == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            voiceSource.clip = clip;
            voiceSource.Play();
        }

        public void PlayEffect(AudioClip clip)
        {
            if (effectsSource == null) return;

            effectsSource.clip = clip;
            effectsSource.Play();
        }

        public void PlayEffect(AudioClip[] clips)
        {
            if (effectsSource == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];

            effectsSource.clip = clip;
            effectsSource.Play();
        }
    }
}