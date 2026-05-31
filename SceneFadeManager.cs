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
        // Eğer oyun İLK KEZ açılıyorsa (Ana menüye masaüstünden girildiyse) siyah ekranı tamamen gizle
        if (ilkAcilis)
        {
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            ilkAcilis = false;
        }
        else // Eğer başka bir sahneden (oyundan) geliyorsak animasyonu oynat
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
        // 1. GÜVENLİK: Obje kapalıysa (sen editörde kapattıysan) ZORLA AÇ!
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true; // Arkadaki butonlara tıklanmayı engelle
        }

        // Sahnedeki müziği bul (Müzik de ekranla birlikte yavaşça kısılacak)
        AudioSource sahneMuzigi = FindFirstObjectByType<AudioSource>();
        float baslangicSesi = sahneMuzigi != null ? sahneMuzigi.volume : 0f;

        // 2. SİNEMATİK KARARMA (Fade Out)
        float gecenSure = 0f;
        while (gecenSure < gecisSuresi)
        {
            gecenSure += Time.unscaledDeltaTime;

            // Düz (lineer) bir geçiş yerine, verdiğimiz eğriyi (AnimationCurve) kullanarak yumuşat
            float yuzde = gecenSure / gecisSuresi;
            float egriselYuzde = gecisEgrisi.Evaluate(yuzde);

            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, egriselYuzde));

            if (sahneMuzigi != null)
                sahneMuzigi.volume = Mathf.Lerp(baslangicSesi, 0f, egriselYuzde);

            yield return null;
        }

        // Zaman durmuşsa sıfırla ve yeni sahneyi yükle
        Time.timeScale = 1f;
        SceneManager.LoadScene(sahneAdi);

        // Sahnenin, ışıkların ve objelerin tam oturması için 1 kare bekle
        yield return null;

        // 3. Yeni sahnede aydınlanma sürecini başlat
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

        // 4. TEMİZLİK: Geçiş bitince objeyi TAMAMEN KAPAT. 
        // Böylece hem performansı yemez hem de ekranın önünde görünmez bir duvar bırakmaz.
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }
}