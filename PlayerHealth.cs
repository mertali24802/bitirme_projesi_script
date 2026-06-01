using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can Ayarları")]
    public int maxCan = 6;
    public int mevcutCan;

    [Header("Hasar Alma (Knockback)")]
    public float geriTepmeGucuX = 10f;
    public float geriTepmeGucuY = 5f;
    public float sarsintiSuresi = 0.3f;
    public float yenilmezlikSuresi = 1f;

    [HideInInspector] public bool dashDokunulmazligi = false;

    private bool hasarAlabilirMi = true;
    private Rigidbody2D rb;
    private PlayerMovement hareketKodu;
    private SpriteRenderer spriteRenderer;
    private Color orijinalRenk;
    private Vector3 orijinalBoyut; 

    void Start()
    {
        mevcutCan = maxCan;
        rb = GetComponent<Rigidbody2D>();
        hareketKodu = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            orijinalRenk = spriteRenderer.color;
        }
        orijinalBoyut = transform.localScale;

        StartCoroutine(YenidenDogmaEfekti());
    }

    public void HasarAl(int hasarMiktari, float dusmanXPos)
    {
        if (!hasarAlabilirMi || dashDokunulmazligi) return;

        mevcutCan -= hasarMiktari;
        if (UIManager.instance != null) UIManager.instance.CanGuncelle(mevcutCan);

        if (mevcutCan <= 0)
        {
            StartCoroutine(OlumUygula());
        }
        else
        {
            StartCoroutine(GeriTepmeUygula(dusmanXPos));
        }
    }

    private IEnumerator OlumUygula()
    {
        hasarAlabilirMi = false;
        if (hareketKodu != null) hareketKodu.enabled = false;

        GetComponent<Collider2D>().enabled = false;

        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        rb.gravityScale = 3f; 
        rb.linearVelocity = new Vector2(0f, 15f); 

        yield return new WaitForSeconds(1.5f);

        if (SceneFadeManager.instance != null)
            SceneFadeManager.instance.SahneYukle(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator GeriTepmeUygula(float dusmanXPos)
    {
        hasarAlabilirMi = false;
        if (hareketKodu != null) hareketKodu.enabled = false;

        float tepmeYonu = transform.position.x < dusmanXPos ? -1f : 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(tepmeYonu * geriTepmeGucuX, geriTepmeGucuY), ForceMode2D.Impulse);

        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(sarsintiSuresi);

        if (spriteRenderer != null) spriteRenderer.color = orijinalRenk;
        if (hareketKodu != null) hareketKodu.enabled = true;

        yield return new WaitForSeconds(yenilmezlikSuresi - sarsintiSuresi);
        hasarAlabilirMi = true;
    }

    private IEnumerator YenidenDogmaEfekti()
    {
        hasarAlabilirMi = false; 
        float flashSuresi = 2f;
        float gecenSure = 0f;
        bool gorunur = true;

        while (gecenSure < flashSuresi)
        {
            gorunur = !gorunur; 
            if (spriteRenderer != null)
            {
                spriteRenderer.color = gorunur ? orijinalRenk : new Color(1f, 1f, 1f, 0.3f);
            }

            yield return new WaitForSeconds(0.15f); 
            gecenSure += 0.15f;
        }

        if (spriteRenderer != null) spriteRenderer.color = orijinalRenk;
        hasarAlabilirMi = true; 
    }
}