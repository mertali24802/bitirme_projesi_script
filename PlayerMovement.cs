using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float karakterHizi = 8f;

    private float zeminKontrolKapanmaZamani;

    private bool ziplamayaHazirlaniyor = false;

    [Header("Jump Ayarları")]
    public float ziplamaKuvveti = 12f;
    private bool ciftZiplamaYapabilirMi;
    public float DoubleJumpBeklemeSuresi = 1f;
    private bool ziplamaKilitliMi = false;

    [Header("Duvar Ayarları")]
    public float duvardanKaymaHizi = 2f;
    public Vector2 duvardanZiplamaKuvveti = new Vector2(10f, 12f);
    public float duvarGirdiKilitSuresi = 0.25f;
    private bool duvardaMi, duvardanKayiyorMu, duvardanZipliyorMu;
    private float duvarZiplamaZamanlayicisi;
    private int sonZiplamaDuvarYonu = 0;

    [Header("Coyote Time & Yerçekimi")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeSayaci;
    public float dususYercekimiCarpani = 2.5f;
    private float orijinalYercekimi;

    [Header("Dash Ayarları")]
    public TrailRenderer dashIzi;
    public float dashKuvveti = 24f;
    public float dashSuresi = 0.2f;
    public float dashBeklemeSuresi = 5f;
    [HideInInspector] public bool dashAtiyorMu;
    [HideInInspector] public float sonDashZamani = -5f;
    private float sonYon = 1f;

    [Header("Zemin & Efekt Ayarları")]
    public LayerMask zeminKatmani;
    public bool zemindeMi;

    [Tooltip("Project klasöründeki Ziplama_Toz prefabını sürükle")]
    public GameObject ziplamaEfektiPrefab;
    [Tooltip("Project klasöründeki DoubleJump_Toz prefabını sürükle")]
    public GameObject ciftZiplamaEfektiPrefab;
    [Tooltip("Karakterin içindeki Ayak_Hizasi objesini sürükle")]
    public Transform ayakPozisyonu;

    private Rigidbody2D rb;
    private Collider2D kolajdir;
    private PlayerHealth canKodu;
    private float yatayGirdi;
    private Animator anim; 

    void Start()
    {
        if (dashIzi != null) dashIzi.emitting = false;
        rb = GetComponent<Rigidbody2D>();
        kolajdir = GetComponent<Collider2D>();
        canKodu = GetComponent<PlayerHealth>();
        anim = GetComponent<Animator>();
        orijinalYercekimi = rb.gravityScale;

        PhysicsMaterial2D kayganMateryal = new PhysicsMaterial2D("KayganZemin");
        kayganMateryal.friction = 0f;
        kayganMateryal.bounciness = 0f;
        kolajdir.sharedMaterial = kayganMateryal;
    }

    void Update()
    {
        if (dashAtiyorMu) return;
        if (GameManager.instance != null && GameManager.instance.isPaused) return;
        if (dashAtiyorMu) return;

        yatayGirdi = Input.GetAxisRaw("Horizontal");

        if (duvardanZipliyorMu)
        {
            duvarZiplamaZamanlayicisi -= Time.deltaTime;
            if (duvarZiplamaZamanlayicisi <= 0f) duvardanZipliyorMu = false;
        }

        if (yatayGirdi != 0 && !duvardanZipliyorMu)
        {
            sonYon = yatayGirdi;

            float mevcutBoyut = Mathf.Abs(transform.localScale.y);

            transform.localScale = new Vector3(sonYon * mevcutBoyut, mevcutBoyut, transform.localScale.z);
        }

        if (anim != null)
        {
            anim.SetFloat("Hiz", Mathf.Abs(yatayGirdi));
            anim.SetBool("ZemindeMi", zemindeMi);
            anim.SetFloat("Y_Hizi", rb.linearVelocity.y);
        }

        Vector2 merkez = kolajdir.bounds.center;
        float genislik = kolajdir.bounds.extents.x;
        float yukseklik = kolajdir.bounds.extents.y;

        RaycastHit2D zeminOrta = Physics2D.Raycast(merkez, Vector2.down, yukseklik + 0.1f, zeminKatmani);
        RaycastHit2D zeminSol = Physics2D.Raycast(merkez - new Vector2(genislik - 0.05f, 0), Vector2.down, yukseklik + 0.1f, zeminKatmani);
        RaycastHit2D zeminSag = Physics2D.Raycast(merkez + new Vector2(genislik - 0.05f, 0), Vector2.down, yukseklik + 0.1f, zeminKatmani);

        if (Time.time > zeminKontrolKapanmaZamani)
        {
            zemindeMi = zeminOrta || zeminSol || zeminSag;
        }
        else
        {
            zemindeMi = false;
        }

        if (zemindeMi)
        {
            coyoteTimeSayaci = coyoteTime;
            ciftZiplamaYapabilirMi = true;
            sonZiplamaDuvarYonu = 0;
            if (rb.linearVelocity.y <= 0.1f) ziplamaKilitliMi = false;
        }
        else
        {
            coyoteTimeSayaci -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && !ziplamaKilitliMi && !duvardanZipliyorMu && !ziplamayaHazirlaniyor)
        {
            if (duvardanKayiyorMu)
            {
                DuvardanZiplamaBaslat();
            }
            else if (coyoteTimeSayaci > 0f) 
            {
                ZiplamaHazirlikUygula();
            }
            else if (ciftZiplamaYapabilirMi) 
            {
                if (AudioManager.instance != null) AudioManager.instance.SesCal(AudioManager.instance.ciftZiplama);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, ziplamaKuvveti);
                ciftZiplamaYapabilirMi = false;

                if (anim != null) anim.SetTrigger("CiftZiplama");

                if (ciftZiplamaEfektiPrefab != null && ayakPozisyonu != null)
                    Instantiate(ciftZiplamaEfektiPrefab, ayakPozisyonu.position, Quaternion.identity);

                StartCoroutine(ZiplamaBeklemeSuresiBaslat());
            }
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= sonDashZamani + dashBeklemeSuresi)
            StartCoroutine(DashUygula());
    }

    void FixedUpdate()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused) return;
        if (dashAtiyorMu) return;

        if (!duvardanZipliyorMu) rb.linearVelocity = new Vector2(yatayGirdi * karakterHizi, rb.linearVelocity.y);

        if (duvardanKayiyorMu && rb.linearVelocity.y < 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -duvardanKaymaHizi, float.MaxValue));

        if (duvardanKayiyorMu) rb.gravityScale = orijinalYercekimi;
        else if (Mathf.Abs(rb.linearVelocity.y) < 1f && !zemindeMi) rb.gravityScale = orijinalYercekimi * 0.5f;
        else if (rb.linearVelocity.y < 0) rb.gravityScale = orijinalYercekimi * dususYercekimiCarpani;
        else rb.gravityScale = orijinalYercekimi;
    }

    private void DuvardanZiplamaBaslat()
    {
        int suAnkiDuvarYonu = (int)Mathf.Sign(transform.localScale.x);
        if (suAnkiDuvarYonu == sonZiplamaDuvarYonu) return;
        duvardanZipliyorMu = true;
        duvarZiplamaZamanlayicisi = duvarGirdiKilitSuresi;
        sonZiplamaDuvarYonu = suAnkiDuvarYonu;
        float ziplamaYonu = -suAnkiDuvarYonu;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(ziplamaYonu * duvardanZiplamaKuvveti.x, duvardanZiplamaKuvveti.y), ForceMode2D.Impulse);
        sonYon = ziplamaYonu;
        transform.localScale = new Vector3(sonYon, 1, 1);
        ciftZiplamaYapabilirMi = true;
    }

    private IEnumerator ZiplamaBeklemeSuresiBaslat()
    {
        ziplamaKilitliMi = true;
        yield return new WaitForSeconds(DoubleJumpBeklemeSuresi);
        ziplamaKilitliMi = false;
    }

    private IEnumerator DashUygula()
    {
        if (AudioManager.instance != null) AudioManager.instance.SesCal(AudioManager.instance.dash);
        if (UIManager.instance != null) UIManager.instance.AnimasyonDashTetikle();
        dashAtiyorMu = true;

        if (anim != null) anim.SetBool("DashAtiyorMu", true);
        if (canKodu != null) canKodu.dashDokunulmazligi = true;

        if (dashIzi != null)
        {
            dashIzi.Clear(); 
            dashIzi.emitting = true;
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float orijinalYercekimiGecici = rb.gravityScale;
        rb.gravityScale = 0f;

        RigidbodyConstraints2D orijinalKilit = rb.constraints;
        rb.constraints = orijinalKilit | RigidbodyConstraints2D.FreezePositionY;

        rb.linearVelocity = new Vector2(sonYon * dashKuvveti, 0f);

        try
        {
            yield return new WaitForSeconds(dashSuresi);
        }
        finally
        {
            rb.gravityScale = orijinalYercekimiGecici;

            rb.constraints = orijinalKilit;

            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

            if (canKodu != null) canKodu.dashDokunulmazligi = false;

            if (dashIzi != null) dashIzi.emitting = false;

            if (anim != null) anim.SetBool("DashAtiyorMu", false); 
            dashAtiyorMu = false;
            sonDashZamani = Time.time;
        }
    }

    [Header("Zıplama Gecikmesi (Anticipation)")]
    [Tooltip("Karakterin zıplamadan önce ne kadar çömeleceği (Saniye)")]
    public float hazirlikSuresi = 0.15f;
    private async void ZiplamaHazirlikUygula()
    {
        if (AudioManager.instance != null) AudioManager.instance.SesCal(AudioManager.instance.ziplama);

        ziplamayaHazirlaniyor = true;

        if (anim != null) anim.Play("jump_anticipation");

        await Awaitable.WaitForSecondsAsync(hazirlikSuresi);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, ziplamaKuvveti);
        zeminKontrolKapanmaZamani = Time.time + 0.1f;
        coyoteTimeSayaci = 0f;

        if (ziplamaEfektiPrefab != null && ayakPozisyonu != null)
            Instantiate(ziplamaEfektiPrefab, ayakPozisyonu.position, Quaternion.identity);

        ziplamayaHazirlaniyor = false;
    }
}