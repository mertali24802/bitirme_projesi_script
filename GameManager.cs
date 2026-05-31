using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool isPaused = false;
    public bool isGameOver = false;

    [Header("Arayüz ve Efekt Referansları")]
    public GameObject pauseMenuGenel;
    public Volume globalVolume;

    [Header("Sahne Ayarları")]
    public string anaMenuSahnesiAdi = "AnaMenu";

    private DepthOfField bulaniklikEfekti;
    private EventSystem oyunEventSistemi;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        oyunEventSistemi = EventSystem.current;
        if (pauseMenuGenel != null) pauseMenuGenel.SetActive(false);

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out bulaniklikEfekti);
            if (bulaniklikEfekti != null) bulaniklikEfekti.active = false;
        }

        // --- ENTEGRE SAVE-LOAD BAŞLANGICI ---
        if (SaveManager.KayitVarMi())
        {
            SaveData yuklenenVeri = SaveManager.Yukle();
            GameObject oyuncu = GameObject.FindGameObjectWithTag("Player");

            if (oyuncu != null)
            {
                oyuncu.transform.position = new Vector2(yuklenenVeri.playerPosX, yuklenenVeri.playerPosY);

                PlayerHealth oyuncuCanKodu = oyuncu.GetComponent<PlayerHealth>();
                if (oyuncuCanKodu != null)
                {
                    oyuncuCanKodu.mevcutCan = yuklenenVeri.mevcutCan;
                    if (UIManager.instance != null) UIManager.instance.CanGuncelle(yuklenenVeri.mevcutCan);
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
        {
            if (isPaused) OyunaDevamEt();
            else OyunuDurdur();
        }
    }

    public void OyunuDurdur()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuGenel != null) pauseMenuGenel.SetActive(true);
        if (bulaniklikEfekti != null) bulaniklikEfekti.active = true;
        if (oyunEventSistemi != null) oyunEventSistemi.enabled = true;
    }

    public void OyunaDevamEt()
    {
        isPaused = false;

        if (pauseMenuGenel != null) pauseMenuGenel.SetActive(false);
        if (bulaniklikEfekti != null) bulaniklikEfekti.active = false;

        Time.timeScale = 1f;
    }

    public void ButonDevamEt()
    {
        if (oyunEventSistemi != null) oyunEventSistemi.enabled = false;
        StartCoroutine(GecikmeliKapatRoutine());
    }

    private System.Collections.IEnumerator GecikmeliKapatRoutine()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        OyunaDevamEt();
    }

    public void ButonYenidenBasla()
    {
        if (oyunEventSistemi != null) oyunEventSistemi.enabled = false;
        StartCoroutine(GecikmeliYenidenBaslaRoutine());
    }

    private System.Collections.IEnumerator GecikmeliYenidenBaslaRoutine()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        Time.timeScale = 1f;

        // Kural: Oyundayken yeniden başla dediğinde save silinecek
        SaveManager.KaydiSil();

        if (SceneFadeManager.instance != null)
            SceneFadeManager.instance.SahneYukle(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- KURAL: ANA MENÜYE DÖN DERKEN OTOMATİK KAYDEDER ---
    public void AnaMenuyeDonVeKaydet()
    {
        GameObject oyuncu = GameObject.FindGameObjectWithTag("Player");
        if (oyuncu != null)
        {
            SaveData data = new SaveData();
            data.playerPosX = oyuncu.transform.position.x;
            data.playerPosY = oyuncu.transform.position.y;

            PlayerHealth oyuncuCanKodu = oyuncu.GetComponent<PlayerHealth>();
            data.mevcutCan = oyuncuCanKodu != null ? oyuncuCanKodu.mevcutCan : 6;

            SaveManager.Kaydet(data); // Bilgisayara yazdır
        }

        if (oyunEventSistemi != null) oyunEventSistemi.enabled = false;
        Time.timeScale = 1f;

        if (SceneFadeManager.instance != null)
            SceneFadeManager.instance.SahneYukle(anaMenuSahnesiAdi);
        else
            SceneManager.LoadScene(anaMenuSahnesiAdi);
    }

    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("Karakter öldü!");
        // Karakter ölünce save dosyasını silmek istersen buraya da SaveManager.KaydiSil(); ekleyebilirsin
    }
}