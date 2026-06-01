using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    private AudioSource kaynak;
    private AudioSource alarmKaynak; 

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

        alarmKaynak = gameObject.AddComponent<AudioSource>();
        alarmKaynak.loop = true;
    }

    void Start()
    {
        if (alarm != null) alarmKaynak.clip = alarm;
    }

    public void SesCal(AudioClip klip, float sesSeviyesi = 1f)
    {
        if (klip != null) kaynak.PlayOneShot(klip, sesSeviyesi);
    }

    public void AlarmCal(bool acikMi)
    {
        if (acikMi && !alarmKaynak.isPlaying) alarmKaynak.Play();
        else if (!acikMi && alarmKaynak.isPlaying) alarmKaynak.Stop();
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