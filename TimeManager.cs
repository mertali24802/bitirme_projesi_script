using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    [Header("Zaman Ayarları")]
    public float yavasZamanCarpani = 0.3f;
    private float normalZamanCarpani = 1f;

    [Header("Enerji (Chronos) Ayarları")]
    public float maxEnerji = 3f;
    public float mevcutEnerji;
    public float harcamaHizi = 1f;
    public float yenilenmeHizi = 0.5f;
    public static TimeManager instance;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private bool yavasCekimAktifMi = false;

    void Start()
    {
        mevcutEnerji = maxEnerji;
    }

    void Update()
    {
        if (GameManager.instance != null && (GameManager.instance.isPaused || GameManager.instance.isGameOver)) return;

        if (Input.GetMouseButtonDown(1) && mevcutEnerji > 0)
        {
            if (UIManager.instance != null) UIManager.instance.AnimasyonZamanTetikle();
        }

        if (Input.GetMouseButton(1) && mevcutEnerji > 0)
        {
            ZamaniYavaslat();
            mevcutEnerji -= Time.unscaledDeltaTime * harcamaHizi;
        }

        else
        {
            ZamaniNormaleDondur();

            if (mevcutEnerji < maxEnerji)
            {
                mevcutEnerji += Time.unscaledDeltaTime * yenilenmeHizi;
            }
        }

        mevcutEnerji = Mathf.Clamp(mevcutEnerji, 0, maxEnerji);
    }

    private void ZamaniYavaslat()
    {
        if (yavasCekimAktifMi) return;

        Time.timeScale = yavasZamanCarpani;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yavasCekimAktifMi = true;
    }

    public void ZamaniNormaleDondur()
    {
        if (!yavasCekimAktifMi) return;

        Time.timeScale = normalZamanCarpani;
        Time.fixedDeltaTime = 0.02f;
        yavasCekimAktifMi = false;
    }

    public bool hitstopAktifMi = false;

    public async void HitstopTetikle(float durmaSuresi = 0.05f)
    {
        if (hitstopAktifMi) return;
        hitstopAktifMi = true;

        float oncekiZamanCarpani = Time.timeScale;
        Time.timeScale = 0f;

        await System.Threading.Tasks.Task.Delay(Mathf.RoundToInt(durmaSuresi * 1000f));

        Time.timeScale = yavasCekimAktifMi ? yavasZamanCarpani : 1f;
        hitstopAktifMi = false;
    }
}