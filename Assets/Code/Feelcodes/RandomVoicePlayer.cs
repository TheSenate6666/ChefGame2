using System.Collections;
using UnityEngine;
using MoreMountains.Tools;

public class RandomVoicePlayer : MonoBehaviour
{
    [Header("Voices")]
    [SerializeField] private AudioClip[] voiceClips;

    [Header("Timing (seconds)")]
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 10f;
    [SerializeField] private bool playOnStart = true;

    [Header("Sound Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField] private bool avoidImmediateRepeat = true;

    private Coroutine voiceRoutine;
    private int lastIndex = -1;

    private void OnEnable()
    {
        if (playOnStart) StartVoices();
    }

    private void OnDisable()
    {
        StopVoices();
    }

    public void StartVoices()
    {
        if (voiceRoutine != null) return;
        voiceRoutine = StartCoroutine(VoiceLoop());
    }

    public void StopVoices()
    {
        if (voiceRoutine == null) return;
        StopCoroutine(voiceRoutine);
        voiceRoutine = null;
    }

    private IEnumerator VoiceLoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
            yield return new WaitForSeconds(wait);

            PlayRandomVoice();
        }
    }

    private void PlayRandomVoice()
    {
        if (voiceClips == null || voiceClips.Length == 0) return;

        int index = Random.Range(0, voiceClips.Length);

        if (avoidImmediateRepeat && voiceClips.Length > 1)
        {
            while (index == lastIndex)
            {
                index = Random.Range(0, voiceClips.Length);
            }
        }
        lastIndex = index;

        AudioClip clip = voiceClips[index];
        if (clip == null) return;

        MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        options.Loop = false;
        options.Volume = volume;
        options.Pitch = Random.Range(pitchRange.x, pitchRange.y);
        options.Persistent = false;

        MMSoundManagerSoundPlayEvent.Trigger(clip, options);
    }
}