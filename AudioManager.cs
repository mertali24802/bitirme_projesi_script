using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    private AudioSource kaynak;

    [Header("Oyuncu Sesleri")]
    public AudioClip ziplama;
    public AudioClip ciftZiplama;
    public AudioClip dash;
    public AudioClip playerSaldiri;
    public AudioClip playerOlum;
    public AudioClip elektrik; 

    [Header("Düşman Sesleri")]
    public AudioClip slimeHasar;
    public AudioClip slimeOlum;
    public AudioClip alarm;

    void Awake()
    {
        if (instance == null) instance = this;
        kaynak = gameObject.AddComponent<AudioSource>();
    }

    public void SesCal(AudioClip klip, float sesSeviyesi = 1f)
    {
        if (klip != null) kaynak.PlayOneShot(klip, sesSeviyesi);
    }

    public void OyuncuHasarSesiCal()
    {
        if (slimeHasar != null)
        {
            kaynak.pitch = 0.7f; 
            kaynak.PlayOneShot(slimeHasar, 1f);
            Invoke("PitchSifirla", 0.5f); 
        }
    }

    private void PitchSifirla()
    {
        kaynak.pitch = 1f;
    }
}