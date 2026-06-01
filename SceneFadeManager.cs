using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager instance;

    [Header("Sinematik Geçiş Ayarları")]
    public Image fadeImage;

    [Tooltip("Geçişin saniye cinsinden uzunluğu. (Daha uzun ve tok bir his için 2 veya 2.5 yap)")]
    public float gecisSuresi = 2.0f;

    [Tooltip("Geçişin yumuşaklığını belirleyen matematiksel eğri")]
    public AnimationCurve gecisEgrisi = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static bool ilkAcilis = true;

    void Start()
    {
        if (ilkAcilis)
        {
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            ilkAcilis = false;
        }
        else 
        {
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                fadeImage.color = new Color(0, 0, 0, 1f);
                StartCoroutine(SiyahEkrandanAydinliga());
            }
        }
    }

    public void SahneYukle(string sahneAdi)
    {
        StartCoroutine(KaranlikSahneGecisi(sahneAdi));
    }

    private IEnumerator KaranlikSahneGecisi(string sahneAdi)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true; 
        }

        AudioSource sahneMuzigi = FindFirstObjectByType<AudioSource>();
        float baslangicSesi = sahneMuzigi != null ? sahneMuzigi.volume : 0f;

        float gecenSure = 0f;
        while (gecenSure < gecisSuresi)
        {
            gecenSure += Time.unscaledDeltaTime;

            float yuzde = gecenSure / gecisSuresi;
            float egriselYuzde = gecisEgrisi.Evaluate(yuzde);

            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, egriselYuzde));

            if (sahneMuzigi != null)
                sahneMuzigi.volume = Mathf.Lerp(baslangicSesi, 0f, egriselYuzde);

            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sahneAdi);

        yield return null;

        StartCoroutine(SiyahEkrandanAydinliga());
    }

    private IEnumerator SiyahEkrandanAydinliga()
    {
        float gecenSure = 0f;
        while (gecenSure < gecisSuresi)
        {
            gecenSure += Time.unscaledDeltaTime;

            float yuzde = gecenSure / gecisSuresi;
            float egriselYuzde = gecisEgrisi.Evaluate(yuzde);

            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, egriselYuzde));

            yield return null;
        }

        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }
}