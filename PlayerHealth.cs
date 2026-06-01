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

    // Dash sırasında diğer kodlardan (PlayerMovement) erişip değiştirebilmek için public yaptık
    [HideInInspector] public bool dashDokunulmazligi = false;

    private bool hasarAlabilirMi = true;
    private Rigidbody2D rb;
    private PlayerMovement hareketKodu;
    private SpriteRenderer spriteRenderer;
    private Color orijinalRenk;

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
    }

    public void HasarAl(int hasarMiktari, float dusmanXPos)
    {
        // Eğer normal yenilmezlik süresindeysek VEYA dash atıyorsak hasar alma!
        if (!hasarAlabilirMi || dashDokunulmazligi) return;

        mevcutCan -= hasarMiktari;
        UIManager.instance.CanGuncelle(mevcutCan);
        Debug.Log("Hasar Alındı! Kalan Can: " + mevcutCan);

        if (mevcutCan <= 0)
        {
            GameManager.instance.GameOver();
            // Karakterin hareketini ve görselini kapatarak öldüğünü belli edelim
            spriteRenderer.enabled = false;
            hareketKodu.enabled = false;
        }
        else
        {
            StartCoroutine(GeriTepmeUygula(dusmanXPos));
        }
    }

    private IEnumerator GeriTepmeUygula(float dusmanXPos)
    {
        hasarAlabilirMi = false;
        hareketKodu.enabled = false;

        // --- YENİ: FİZİKSEL ÇARPIŞMAYI GEÇİCİ KAPAT ---
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float tepmeYonu = transform.position.x < dusmanXPos ? -1f : 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(tepmeYonu * geriTepmeGucuX, geriTepmeGucuY), ForceMode2D.Impulse);

        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(sarsintiSuresi);

        if (spriteRenderer != null) spriteRenderer.color = orijinalRenk;
        hareketKodu.enabled = true;

        // Sarsıntı süresi bitti, şimdi kalan yenilmezlik süresini bekle
        yield return new WaitForSeconds(yenilmezlikSuresi - sarsintiSuresi);

        hasarAlabilirMi = true;

        // --- YENİ: FİZİKSEL ÇARPIŞMAYI GERİ AÇ ---
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
    }
}