using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Can UI Animatörü")]
    public Animator canBariAnimator;

    [Header("Canvas Grupları (Saydamlık İçin)")]
    public CanvasGroup canBariCanvasGroup;
    public CanvasGroup skillerCanvasGroup;

    [Header("Saydamlık Ayarları")]
    public float saydamlikMiktari = 0.1f;
    public float gecisHizi = 7f;

    [Header("Arka Plan Zamanlayıcıları")]
    private float oyunBaslangicZamanlayici = 5f;
    private float hareketsizSure = 0f;
    private float canHasarZamanlayici = 0f;
    private float skillBasildiZamanlayici = 0f;

    [Header("Can UI Sarsıntı Efektleri")]
    public RectTransform canBariRect;
    public Image canBariKanResmi;
    public float sarsintiSiddeti = 22f;
    public float sarsintiSuresi = 0.35f;
    private Vector2 orijinalPozisyon;

    [Header("Yetenek UI - Karanlık Radial İmajlar")]
    public Image blinkStrikeKaranlik;
    public Image zamanYavaslatmaKaranlik;
    public Image dashKaranlik;

    [Header("Yetenek UI - Basılma Animatörleri")]
    public Animator blinkStrikeAnimator;
    public Animator zamanYavaslatmaAnimator;
    public Animator dashAnimator;

    [Header("Oyun Kodları Referansları")]
    public TimeManager timeManager;
    public PlayerCombat playerCombat;
    public PlayerMovement playerMovement;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (blinkStrikeKaranlik != null) blinkStrikeKaranlik.gameObject.SetActive(false);
        if (zamanYavaslatmaKaranlik != null) zamanYavaslatmaKaranlik.gameObject.SetActive(false);
        if (dashKaranlik != null) dashKaranlik.gameObject.SetActive(false);

        if (canBariRect != null)
        {
            orijinalPozisyon = canBariRect.anchoredPosition;
        }
    }

    void Update()
    {
        if (oyunBaslangicZamanlayici > 0) oyunBaslangicZamanlayici -= Time.unscaledDeltaTime;
        if (canHasarZamanlayici > 0) canHasarZamanlayici -= Time.unscaledDeltaTime;
        if (skillBasildiZamanlayici > 0) skillBasildiZamanlayici -= Time.unscaledDeltaTime;

        bool hareketEdiyorMu = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 || Input.GetKey(KeyCode.Space);

        if (hareketEdiyorMu) hareketsizSure = 0f;
        else hareketsizSure += Time.unscaledDeltaTime;

        if (playerCombat != null && blinkStrikeKaranlik != null)
        {
            float blinkKalanSure = playerCombat.ozelYetenekZamani - Time.time;
            if (blinkKalanSure > 0) { blinkStrikeKaranlik.gameObject.SetActive(true); blinkStrikeKaranlik.fillAmount = blinkKalanSure / playerCombat.ozelYetenekBeklemeSuresi; }
            else blinkStrikeKaranlik.gameObject.SetActive(false);
        }

        if (timeManager != null && zamanYavaslatmaKaranlik != null)
        {
            float zamanDoluluk = timeManager.mevcutEnerji / timeManager.maxEnerji;
            if (zamanDoluluk < 0.99f) { zamanYavaslatmaKaranlik.gameObject.SetActive(true); zamanYavaslatmaKaranlik.fillAmount = 1f - zamanDoluluk; }
            else zamanYavaslatmaKaranlik.gameObject.SetActive(false);
        }

        if (playerMovement != null && dashKaranlik != null)
        {
            float dashKalanSure = (playerMovement.sonDashZamani + playerMovement.dashBeklemeSuresi) - Time.time;
            if (dashKalanSure > 0) { dashKaranlik.gameObject.SetActive(true); dashKaranlik.fillAmount = dashKalanSure / playerMovement.dashBeklemeSuresi; }
            else dashKaranlik.gameObject.SetActive(false);
        }

        bool canGorunurMu = (oyunBaslangicZamanlayici > 0f) || (hareketsizSure >= 1f) || (canHasarZamanlayici > 0f);
        float hedefCanAlpha = canGorunurMu ? 1f : saydamlikMiktari;
        if (canBariCanvasGroup != null)
            canBariCanvasGroup.alpha = Mathf.Lerp(canBariCanvasGroup.alpha, hedefCanAlpha, Time.unscaledDeltaTime * gecisHizi);

        bool skillGorunurMu = (oyunBaslangicZamanlayici > 0f) || (hareketsizSure >= 1f) || (skillBasildiZamanlayici > 0f);
        float hedefSkillAlpha = skillGorunurMu ? 1f : saydamlikMiktari;
        if (skillerCanvasGroup != null)
            skillerCanvasGroup.alpha = Mathf.Lerp(skillerCanvasGroup.alpha, hedefSkillAlpha, Time.unscaledDeltaTime * gecisHizi);
    }

    public void AnimasyonBlinkTetikle()
    {
        if (blinkStrikeAnimator != null) blinkStrikeAnimator.Play("Skill_Basildi");
        skillBasildiZamanlayici = 5f; 
    }

    public void AnimasyonZamanTetikle()
    {
        if (zamanYavaslatmaAnimator != null) zamanYavaslatmaAnimator.Play("Skill_Basildi");
        skillBasildiZamanlayici = 5f;
    }

    public void AnimasyonDashTetikle()
    {
        if (dashAnimator != null) dashAnimator.Play("Skill_Basildi");
        skillBasildiZamanlayici = 5f;
    }

    public void CanGuncelle(int guncelCan)
    {
        if (canBariAnimator != null) canBariAnimator.SetInteger("MevcutCan", guncelCan);

        canHasarZamanlayici = 5f; 

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CanBariSarsintiRoutine());
        }
    }

    private IEnumerator CanBariSarsintiRoutine()
    {
        if (canBariRect == null) yield break;

        float gecenSure = 0f;
        Color orijinalRenk = Color.white;

        if (canBariKanResmi != null) orijinalRenk = canBariKanResmi.color;

        while (gecenSure < sarsintiSuresi)
        {
            float xOffset = Random.Range(-1f, 1f) * sarsintiSiddeti;
            float yOffset = Random.Range(-1f, 1f) * sarsintiSiddeti;
            canBariRect.anchoredPosition = orijinalPozisyon + new Vector2(xOffset, yOffset);

            if (canBariKanResmi != null)
            {
                canBariKanResmi.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(gecenSure * 15f, 1f));
            }

            gecenSure += Time.unscaledDeltaTime;
            yield return null;
        }

        canBariRect.anchoredPosition = orijinalPozisyon;
        if (canBariKanResmi != null) canBariKanResmi.color = orijinalRenk;
    }
}