using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Player")]
    public AudioClip playerShoot;
    public AudioClip playerHit;
    public AudioClip playerDie;

    [Header("Enemy")]
    public AudioClip enemyHit;
    public AudioClip enemyDie;
    public AudioClip enemyShoot;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Play(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }
}