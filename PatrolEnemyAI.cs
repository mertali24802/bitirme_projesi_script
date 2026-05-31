using UnityEngine;
using System.Collections;

public enum PatrolState { Devriye, Suphe, Alarm, Olu }

public class PatrolSlimeAI : MonoBehaviour
{
    [Header("Temel Ayarlar ve Can")]
    public int maxCan = 1;
    private int mevcutCan;

    [Header("Hareket ve Zıplama (Tembel Ayarlar)")]
    public float devriyeZiplamaX = 1.5f;
    public float devriyeZiplamaY = 7f;
    public float devriyeBeklemeSuresi = 2.5f;
    public float kenarBeklemeSuresi = 2.5f;

    [Header("Şüphe Sistemi (Stealth)")]
    public float gorusMesafesi = 10f;
    public float supheDolmaSuresi = 3f;
    public float supheBosalmaHizi = 1.5f;
    private float mevcutSuphe = 0f;

    [Header("Alarm Sistemi (Wave Propagation)")]
    public float baslangicAlarmMenzili = 10f;
    public float alarmGenislemeMiktari = 10f;
    public float alarmGenislemeSuresi = 3f;
    private float guncelAlarmMenzili;
    private float sonAlarmGenislemeZamani;

    [Header("Sensörler")]
    public Transform algilayiciNokta;
    public LayerMask zeminKatmani;
    public LayerMask oyuncuKatmani;

    [Header("Vuruş Hissi ve Geri Tepme")]
    public float hasarYemeGeriTepmeX = 8f;
    public float hasarYemeGeriTepmeY = 3f;
    public SpriteRenderer gövdeSpriteRenderer;

    [Header("Görsel Referanslar")]
    public Animator anaAnimator;
    public Animator unlemAnimator;
    public GameObject unlemObjesi;
    public Animator supheBariAnimator;
    public GameObject supheBariObjesi;
    public GameObject patlamaEfekti;

    [Header("Can Barı Referansları")]
    public Animator canBariAnimator;
    public Transform canBariTransform;
    public float canBariYukseklikOffset = 0.8f;
    private Vector3 canBariSarsintiOffset = Vector3.zero;

    [Header("Takip")]
    public Transform oyuncuHedef;

    public PatrolState mevcutDurum = PatrolState.Devriye;

    private Rigidbody2D rb;
    private Collider2D kolajdir;
    private bool sagaMiBakiyor = true;
    private Vector3 orijinalBoyut;
    private float sonZiplamaZamani;

    private bool ziplamayaHazirlaniyor = false;
    private bool kenardaBekliyor = false;

    private Coroutine kenarRutini;
    private Coroutine ziplamaRutini;
    private Coroutine panikRutini;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kolajdir = GetComponent<Collider2D>();

        rb.mass = 1000f;

        orijinalBoyut = transform.localScale;
        guncelAlarmMenzili = baslangicAlarmMenzili;
        mevcutCan = maxCan;

        if (oyuncuHedef == null)
        {
            GameObject py = GameObject.FindGameObjectWithTag("Player");
            if (py != null) oyuncuHedef = py.transform;
        }

        if (unlemObjesi != null) unlemObjesi.SetActive(false);
        if (supheBariObjesi != null) supheBariObjesi.SetActive(false);

        CanBariGorseliGuncelle();
        DurumDegistir(PatrolState.Devriye);
    }

    private void OnCollisionEnter2D(Collision2D temas)
    {
        if (mevcutDurum == PatrolState.Olu) return;

        if (temas.gameObject.CompareTag("Player"))
        {
            PlayerHealth oyuncuCan = temas.gameObject.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);
        }
    }

    private void OnCollisionStay2D(Collision2D temas)
    {
        if (mevcutDurum == PatrolState.Olu) return;

        if (temas.gameObject.CompareTag("Player"))
        {
            PlayerHealth oyuncuCan = temas.gameObject.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);
        }
    }

    private void DurumDegistir(PatrolState yeniDurum)
    {
        if (mevcutDurum == PatrolState.Olu) return;
        mevcutDurum = yeniDurum;

        if (kenarRutini != null) { StopCoroutine(kenarRutini); kenarRutini = null; }
        if (ziplamaRutini != null) { StopCoroutine(ziplamaRutini); ziplamaRutini = null; }
        if (panikRutini != null) { StopCoroutine(panikRutini); panikRutini = null; }

        kenardaBekliyor = false;
        ziplamayaHazirlaniyor = false;
        if (anaAnimator != null) anaAnimator.speed = 1f;

        switch (yeniDurum)
        {
            case PatrolState.Devriye:
                if (anaAnimator != null) anaAnimator.Play("slime_hareket", -1, 0f);
                if (unlemObjesi != null) unlemObjesi.SetActive(false);
                break;

            case PatrolState.Suphe:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (anaAnimator != null) anaAnimator.Play("slime_farketme", -1, 0f);

                if (unlemObjesi != null) unlemObjesi.SetActive(true);
                if (unlemAnimator != null) unlemAnimator.Play("unlem_suphe", -1, 0f);

                if (supheBariObjesi != null) supheBariObjesi.SetActive(true);
                break;

            case PatrolState.Alarm:
                if (supheBariObjesi != null) supheBariObjesi.SetActive(false);
                if (unlemObjesi != null) unlemObjesi.SetActive(true);
                if (unlemAnimator != null) unlemAnimator.Play("unlem_loop", -1, 0f);

                // YENİ DÜZELTME: Alarm çalar çalmaz beklemeden anında ilk sinyali ver!
                guncelAlarmMenzili = baslangicAlarmMenzili;
                AlarmiDalgasiYarat();

                sonAlarmGenislemeZamani = Time.time;
                panikRutini = StartCoroutine(PanikZiplamasiRoutine());
                break;
        }
    }

    void Update()
    {
        if (mevcutDurum == PatrolState.Olu) return;

        Vector2 merkez = kolajdir.bounds.center;
        float yukseklik = kolajdir.bounds.extents.y;
        RaycastHit2D zeminKontrol = Physics2D.Raycast(merkez, Vector2.down, yukseklik + 0.1f, zeminKatmani);
        bool zemindeMi = (zeminKontrol.collider != null);

        if (zemindeMi && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        bool oyuncuyuGoruyor = false;
        if (oyuncuHedef != null)
        {
            Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;
            float mesafe = Vector2.Distance(transform.position, oyuncuHedef.position);
            bool dogruYon = (sagaMiBakiyor == (oyuncuHedef.position.x > transform.position.x));

            if (mesafe <= gorusMesafesi && dogruYon)
            {
                Vector2 yon = (oyuncuHedef.position - algilayiciNokta.position).normalized;
                if (Physics2D.Raycast(algilayiciNokta.position, yon, mesafe, zeminKatmani).collider == null)
                {
                    oyuncuyuGoruyor = true;
                }
            }
        }

        if (mevcutDurum == PatrolState.Devriye || mevcutDurum == PatrolState.Suphe)
        {
            if (oyuncuyuGoruyor)
            {
                if (mevcutDurum == PatrolState.Devriye) DurumDegistir(PatrolState.Suphe);

                mevcutSuphe += (100f / supheDolmaSuresi) * Time.deltaTime;
                if (mevcutSuphe >= 100f)
                {
                    mevcutSuphe = 100f;
                    DurumDegistir(PatrolState.Alarm);
                }
            }
            else
            {
                if (mevcutSuphe > 0f)
                {
                    mevcutSuphe -= supheBosalmaHizi * 20f * Time.deltaTime;
                    if (mevcutSuphe <= 0f)
                    {
                        mevcutSuphe = 0f;
                        if (supheBariObjesi != null) supheBariObjesi.SetActive(false);
                        if (mevcutDurum == PatrolState.Suphe) DurumDegistir(PatrolState.Devriye);
                    }
                }
            }

            if (supheBariObjesi != null && supheBariObjesi.activeSelf && supheBariAnimator != null)
            {
                float yuzde = mevcutSuphe / 100f;
                supheBariAnimator.Play("suphe_yukselme", -1, yuzde);
            }
        }

        if (mevcutDurum == PatrolState.Devriye)
        {
            if (kenardaBekliyor || ziplamayaHazirlaniyor) return;

            if (zemindeMi && Time.time >= sonZiplamaZamani + devriyeBeklemeSuresi)
            {
                ziplamaRutini = StartCoroutine(NormalZiplamaRoutine());
            }
        }
        else if (mevcutDurum == PatrolState.Alarm)
        {
            if (Time.time >= sonAlarmGenislemeZamani + alarmGenislemeSuresi)
            {
                guncelAlarmMenzili += alarmGenislemeMiktari;
                AlarmiDalgasiYarat();
                sonAlarmGenislemeZamani = Time.time;
            }
        }
    }

    void LateUpdate()
    {
        if (canBariTransform != null && mevcutDurum != PatrolState.Olu)
        {
            float hedefY = transform.position.y + canBariYukseklikOffset;
            Vector3 hedefPozisyon = new Vector3(transform.position.x, hedefY, transform.position.z) + canBariSarsintiOffset;
            canBariTransform.position = Vector3.Lerp(canBariTransform.position, hedefPozisyon, Time.deltaTime * 15f);
        }
    }

    private void AlarmiDalgasiYarat()
    {
        Collider2D[] yakindakiDusmanlar = Physics2D.OverlapCircleAll(transform.position, guncelAlarmMenzili);
        foreach (Collider2D hit in yakindakiDusmanlar)
        {
            AttackSlimeAI saldirgan = hit.GetComponent<AttackSlimeAI>();
            if (saldirgan != null) saldirgan.AlarmiDuy(this);
        }
    }

    private IEnumerator PanikZiplamasiRoutine()
    {
        while (mevcutDurum == PatrolState.Alarm)
        {
            if (anaAnimator != null) anaAnimator.Play("slime_panik", -1, 0f);

            float rastgeleYon = Random.value > 0.5f ? 1f : -1f;
            rb.linearVelocity = new Vector2(rastgeleYon * 1.5f, 6f);

            yield return new WaitForSeconds(0.4f);
            if (this == null) yield break;

            rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(0.3f);
            if (this == null) yield break;
        }
    }

    private IEnumerator NormalZiplamaRoutine()
    {
        ziplamayaHazirlaniyor = true;

        Vector2 bakis = sagaMiBakiyor ? Vector2.right : Vector2.left;
        RaycastHit2D duvar = Physics2D.Raycast(algilayiciNokta.position, bakis, 0.8f, zeminKatmani);
        RaycastHit2D ucurum = Physics2D.Raycast(algilayiciNokta.position + (Vector3)bakis * 0.5f, Vector2.down, 1.5f, zeminKatmani);

        if (duvar.collider != null || ucurum.collider == null)
        {
            ziplamayaHazirlaniyor = false;
            kenarRutini = StartCoroutine(KenardaDusunVeDonRoutine());
            yield break;
        }

        if (anaAnimator != null) anaAnimator.Play("slime_hareket", -1, 0f);

        yield return new WaitForSeconds(0.4f);
        if (this == null) yield break;

        float yonCarpani = sagaMiBakiyor ? 1f : -1f;
        rb.linearVelocity = new Vector2(yonCarpani * devriyeZiplamaX, devriyeZiplamaY);
        sonZiplamaZamani = Time.time;
        ziplamayaHazirlaniyor = false;
    }

    private IEnumerator KenardaDusunVeDonRoutine()
    {
        kenardaBekliyor = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anaAnimator != null)
        {
            anaAnimator.Play("slime_hareket", -1, 0f);
            anaAnimator.Update(0f);
            anaAnimator.speed = 0f;
        }

        yield return new WaitForSeconds(kenarBeklemeSuresi);
        if (this == null) yield break;

        YonCevir();

        if (anaAnimator != null) anaAnimator.speed = 1f;

        sonZiplamaZamani = Time.time - devriyeBeklemeSuresi;
        kenardaBekliyor = false;
    }

    private void YonCevir()
    {
        sagaMiBakiyor = !sagaMiBakiyor;
        float x = Mathf.Abs(orijinalBoyut.x) * (sagaMiBakiyor ? 1f : -1f);
        transform.localScale = new Vector3(x, orijinalBoyut.y, orijinalBoyut.z);
    }

    public void HasarAl(int hasarMiktari, float saldirganXPos)
    {
        if (mevcutDurum == PatrolState.Olu) return;

        mevcutCan -= hasarMiktari;
        CanBariGorseliGuncelle();

        StartCoroutine(CanBariSarsintiRoutine());

        if (mevcutCan <= 0)
        {
            StartCoroutine(OlumRoutine(saldirganXPos));
        }
    }

    private void CanBariGorseliGuncelle()
    {
        if (canBariAnimator != null)
        {
            canBariAnimator.speed = 0f;
            float yuzde = 1f - ((float)mevcutCan / (float)maxCan);
            canBariAnimator.Play("canbari_dusme", -1, yuzde);
            canBariAnimator.Update(0f);
        }
    }

    private IEnumerator CanBariSarsintiRoutine()
    {
        if (canBariTransform == null) yield break;
        float gecenSure = 0f;
        while (gecenSure < 0.15f)
        {
            if (this == null || canBariTransform == null) yield break;
            canBariSarsintiOffset = (Vector3)(Random.insideUnitCircle * 0.15f);
            gecenSure += Time.deltaTime;
            yield return null;
        }
        canBariSarsintiOffset = Vector3.zero;
    }

    private IEnumerator OlumRoutine(float saldirganXPos)
    {
        DurumDegistir(PatrolState.Olu);
        if (kolajdir != null) kolajdir.enabled = false;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.black;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(0.08f);
        if (this == null) yield break;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.white;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float tepmeYonu = transform.position.x < saldirganXPos ? -1f : 1f;
        rb.AddForce(new Vector2(tepmeYonu * hasarYemeGeriTepmeX, hasarYemeGeriTepmeY), ForceMode2D.Impulse);

        if (anaAnimator != null) anaAnimator.Play("slime_olum", -1, 0f);
        if (unlemObjesi != null) unlemObjesi.SetActive(false);
        if (supheBariObjesi != null) supheBariObjesi.SetActive(false);
        if (canBariTransform != null) canBariTransform.gameObject.SetActive(false);

        yield return new WaitForSeconds(1.2f);
        if (this == null) yield break;

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, Application.isPlaying ? guncelAlarmMenzili : baslangicAlarmMenzili);
    }
}