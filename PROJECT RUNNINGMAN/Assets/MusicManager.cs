using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Assign your songs here")]
    public AudioClip[] songs;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // important: don't loop a single track forever
    }

    void Start()
    {
        PlayRandomSong();
    }

    void Update()
    {
        // If nothing is playing, pick another song
        if (!audioSource.isPlaying && songs.Length > 0)
        {
            PlayRandomSong();
        }
    }

    void PlayRandomSong()
    {
        if (songs == null || songs.Length == 0)
        {
            Debug.LogWarning("No songs assigned to RandomMusicPlayer.");
            return;
        }

        int randomIndex = Random.Range(0, songs.Length);
        audioSource.clip = songs[randomIndex];
        audioSource.Play();
    }
}
