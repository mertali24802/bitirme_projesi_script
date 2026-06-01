using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    public int maxCan = 100;
    public int mevcutCan;

    [Header("Hareket Ayarları")]
    public float devriyeHizi = 3f;
    public float kovalamaHizi = 6f;
    private bool sagaMiBakiyor = true;

    [Header("Yapay Zeka Sensörleri")]
    public Transform algilayiciNokta;
    public float zeminKontrolMesafesi = 0.5f;
    public float duvarKontrolMesafesi = 0.2f;
    public float gorusMesafesi = 5f;
    public float saldiriMenzili = 1.2f;

    [Header("Geri Tepme (Knockback) Ayarları")]
    public float geriTepmeGucuX = 8f;
    public float geriTepmeGucuY = 3f;
    public float sersemlemeSuresi = 0.3f;
    private bool sersemlediMi = false;
    private bool olduMu = false; 

    [Header("Katmanlar (Layers)")]
    public LayerMask zeminKatmani;
    public LayerMask oyuncuKatmani;

    [Header("Efektler")]
    public GameObject patlamaEfekti;
    private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private bool oyuncuyuGorduMu;
    private bool oyuncuSaldiriMenzilindeMi;

    private float sonDonmeZamani;
    private float donmeBeklemeSuresi = 0.2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        mevcutCan = maxCan;
    }

    void Update()
    {
        if (sersemlediMi || olduMu) return; 

        Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;

        RaycastHit2D oyuncuVurgunu = Physics2D.Raycast(algilayiciNokta.position, bakisYonu, gorusMesafesi, oyuncuKatmani);
        oyuncuyuGorduMu = (oyuncuVurgunu.collider != null && oyuncuVurgunu.collider.CompareTag("Player"));

        RaycastHit2D saldiriVurgunu = Physics2D.Raycast(algilayiciNokta.position, bakisYonu, saldiriMenzili, oyuncuKatmani);
        oyuncuSaldiriMenzilindeMi = (saldiriVurgunu.collider != null && saldiriVurgunu.collider.CompareTag("Player"));

        RaycastHit2D zeminVurgunu = Physics2D.Raycast(algilayiciNokta.position, Vector2.down, zeminKontrolMesafesi, zeminKatmani);
        RaycastHit2D duvarVurgunu = Physics2D.Raycast(algilayiciNokta.position, bakisYonu, duvarKontrolMesafesi, zeminKatmani);

        if (!oyuncuyuGorduMu && (zeminVurgunu.collider == null || duvarVurgunu.collider != null))
        {
            if (Time.time > sonDonmeZamani + donmeBeklemeSuresi)
            {
                YonCevir();
            }
        }
    }

    void FixedUpdate()
    {
        if (sersemlediMi || olduMu) return; 

        if (oyuncuSaldiriMenzilindeMi)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            float guncelHiz = oyuncuyuGorduMu ? kovalamaHizi : devriyeHizi;
            float yonCarpani = sagaMiBakiyor ? 1f : -1f;

            rb.linearVelocity = new Vector2(yonCarpani * guncelHiz, rb.linearVelocity.y);
        }
    }

    void YonCevir()
    {
        sagaMiBakiyor = !sagaMiBakiyor;
        transform.localScale = new Vector3(sagaMiBakiyor ? 1 : -1, 1, 1);
        sonDonmeZamani = Time.time;
    }

    public void HasarAl(int hasarMiktari, float saldirganXPos)
    {
        if (olduMu) return; 

        mevcutCan -= hasarMiktari;

        if (mevcutCan <= 0)
        {
            olduMu = true;
            StartCoroutine(OlumUygula(saldirganXPos));
        }
        else
        {
            StartCoroutine(GeriTepmeUygula(saldirganXPos));
        }
    }

    private IEnumerator GeriTepmeUygula(float saldirganXPos)
    {
        sersemlediMi = true;
        if (spriteRenderer != null) spriteRenderer.color = Color.red;

        float tepmeYonu = transform.position.x < saldirganXPos ? -1f : 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(tepmeYonu * geriTepmeGucuX, geriTepmeGucuY), ForceMode2D.Impulse);

        yield return new WaitForSeconds(sersemlemeSuresi);

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        sersemlediMi = false;
    }

    private IEnumerator OlumUygula(float saldirganXPos)
    {
        sersemlediMi = true;

        if (spriteRenderer != null) spriteRenderer.color = Color.gray;

        float tepmeYonu = transform.position.x < saldirganXPos ? -1f : 1f;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(tepmeYonu * geriTepmeGucuX, geriTepmeGucuY), ForceMode2D.Impulse);

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(sersemlemeSuresi);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D temas)
    {
        if (temas.gameObject.CompareTag("Player") && !olduMu)
        {
            PlayerHealth oyuncuCan = temas.gameObject.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);
        }
    }

    void OnDrawGizmos()
    {
        if (algilayiciNokta == null) return;
        Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;

        Gizmos.color = Color.green; Gizmos.DrawLine(algilayiciNokta.position, algilayiciNokta.position + Vector3.down * zeminKontrolMesafesi);
        Gizmos.color = Color.blue; Gizmos.DrawLine(algilayiciNokta.position, algilayiciNokta.position + (Vector3)bakisYonu * duvarKontrolMesafesi);
        Gizmos.color = Color.red; Gizmos.DrawLine(algilayiciNokta.position, algilayiciNokta.position + (Vector3)bakisYonu * gorusMesafesi);
        Gizmos.color = Color.yellow; Gizmos.DrawLine(algilayiciNokta.position, algilayiciNokta.position + (Vector3)bakisYonu * saldiriMenzili);
    }
}