using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Saldırı Ayarları")]
    public Transform saldiriNoktasi;
    public float saldiriMenzili = 1.2f;
    public LayerMask dusmanKatmani;
    private ContactFilter2D dusmanFiltresi;
    public int saldiriHasari = 100;
    public float saldiriBeklemeSuresi = 0.3f;
    private float sonrakiSaldiriZamani = 0f;

    [Header("Özel Yetenek: Blink Strike")]
    public float ozelYetenekBeklemeSuresi = 20f;
    [HideInInspector] public float ozelYetenekZamani = 0f;
    public float zincirAramaMenzili = 7f;
    public int maxZincirSayisi = 4;
    private bool ozelSaldiriYapiyorMu = false;

    [Header("Efektler")]
    public GameObject kilicIzi;

    private CinemachineImpulseSource sarsintiKaynagi;
    private PlayerMovement hareketKodu;
    private PlayerHealth canKodu;
    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D[] vurulanDusmanlarHafizasi = new Collider2D[10];

    void Start()
    {
        sarsintiKaynagi = GetComponent<CinemachineImpulseSource>();
        hareketKodu = GetComponent<PlayerMovement>();
        canKodu = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        dusmanFiltresi.useTriggers = false;
        dusmanFiltresi.SetLayerMask(dusmanKatmani);
        dusmanFiltresi.useLayerMask = true;
    }

    void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused) return;
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= sonrakiSaldiriZamani && !hareketKodu.dashAtiyorMu && !ozelSaldiriYapiyorMu)
        {
            Saldir();
            sonrakiSaldiriZamani = Time.time + saldiriBeklemeSuresi;
        }

        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= ozelYetenekZamani && !ozelSaldiriYapiyorMu)
        {
            StartCoroutine(ZincirlemeSaldiriUygula());
        }
    }

    void Saldir()
    {
        if (anim != null) anim.SetTrigger("Saldir");
        StartCoroutine(KilicIziniGoster());
    }

    private IEnumerator KilicIziniGoster()
    {
        if (kilicIzi != null)
        {
            kilicIzi.SetActive(true);
            yield return new WaitForSecondsRealtime(0.25f);
            kilicIzi.SetActive(false);
        }
    }

    private IEnumerator ZincirlemeSaldiriUygula()
    {
        Collider2D ilkDusman = EnYakinDusmaniBul(transform.position, zincirAramaMenzili, new List<Collider2D>());
        if (ilkDusman == null) yield break;

        if (UIManager.instance != null) UIManager.instance.AnimasyonBlinkTetikle();
        ozelSaldiriYapiyorMu = true;
        canKodu.dashDokunulmazligi = true;

        float orijinalYercekimi = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        List<Collider2D> vurulanlarListesi = new List<Collider2D>();
        Vector3 aramaMerkezi = transform.position;
        int yapilanVurusSayisi = 0;

        while (yapilanVurusSayisi < maxZincirSayisi)
        {
            Collider2D hedefDusman = EnYakinDusmaniBul(aramaMerkezi, zincirAramaMenzili, vurulanlarListesi);
            if (hedefDusman == null) break;

            vurulanlarListesi.Add(hedefDusman);
            transform.position = hedefDusman.transform.position + new Vector3(0.5f, 0, 0);

            if (anim != null) anim.SetTrigger("Saldir");
            StartCoroutine(KilicIziniGoster());

            sarsintiKaynagi.GenerateImpulse();
            TimeManager.instance.HitstopTetikle(0.1f);

            // --- DEĞİŞEN KISIM BURASI ---
            // Burada kime vurduğunun bir önemi yok, objede IDamageable varsa hasarı yer.
            IDamageable hasarAlabilirObje = hedefDusman.GetComponent<IDamageable>();
            hasarAlabilirObje?.HasarAl(saldiriHasari, transform.position.x);
            // ------------------------------------------

            yapilanVurusSayisi++;
            aramaMerkezi = hedefDusman.transform.position;

            yield return new WaitForSeconds(0.15f);
        }

        rb.gravityScale = orijinalYercekimi;
        canKodu.dashDokunulmazligi = false;
        ozelSaldiriYapiyorMu = false;
        ozelYetenekZamani = Time.time + ozelYetenekBeklemeSuresi;
    }

    private Collider2D EnYakinDusmaniBul(Vector3 merkez, float menzil, List<Collider2D> gormezdenGelinecekler)
    {
        Collider2D[] yakindakiDusmanlar = Physics2D.OverlapCircleAll(merkez, menzil, dusmanKatmani);
        Collider2D enYakinHedef = null;
        float minMesafe = Mathf.Infinity;

        foreach (Collider2D dusman in yakindakiDusmanlar)
        {
            // YENİ EKLENEN ÇÖKME KALKANI: Obje silinmişse veya yoksa direkt es geç!
            if (dusman == null || dusman.gameObject == null) continue;

            if (gormezdenGelinecekler.Contains(dusman)) continue;
            float mesafe = Vector2.Distance(merkez, dusman.transform.position);
            if (mesafe < minMesafe)
            {
                minMesafe = mesafe;
                enYakinHedef = dusman;
            }
        }
        return enYakinHedef;
    }
}