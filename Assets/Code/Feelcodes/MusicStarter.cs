using UnityEngine;
using MoreMountains.Tools;

public class MusicStarter : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

    private void Start()
    {
        MMSoundManagerPlayOptions options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
        options.Loop = true;
        options.Volume = musicVolume;
        options.Persistent = true; // keeps playing across scene loads
        options.ID = 1;            // so you can stop/pause it later

        MMSoundManagerSoundPlayEvent.Trigger(musicClip, options);
    }
}
