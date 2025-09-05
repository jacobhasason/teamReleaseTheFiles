using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioClip[] musicTracks;   // assign your music files in Inspector
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (musicTracks.Length > 0)
        {
            // pick a random index
            int randomIndex = Random.Range(0, musicTracks.Length);
            PlayTrack(randomIndex);
        }
    }

    public void PlayTrack(int index)
    {
        if (index >= 0 && index < musicTracks.Length)
        {
            audioSource.clip = musicTracks[index];
            audioSource.loop = true; // keep looping
            audioSource.Play();
        }
    }

    public void PlayRandomTrack()
    {
        if (musicTracks.Length > 0)
        {
            int randomIndex = Random.Range(0, musicTracks.Length);
            PlayTrack(randomIndex);
        }
    }
}
