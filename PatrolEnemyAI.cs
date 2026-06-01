using UnityEngine;
using System.Threading.Tasks;

public enum PatrolState { Devriye, Suphe, Alarm, Olu }

public class PatrolEnemyAI : MonoBehaviour, IDamageable
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
    [Tooltip("Alarmın kaç kez büyüyeceğini belirler (Örn: 3)")]
    public int maxAlarmBuyumeSayisi = 3;
    private float guncelAlarmMenzili;
    private float sonAlarmGenislemeZamani;
    private int mevcutBuyumeSayisi = 0;

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
    private int hasarToken = 0;
    private int panikToken = 0;

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

    public void DurumDegistir(PatrolState yeniDurum)
    {
        if (mevcutDurum == PatrolState.Olu) return;
        mevcutDurum = yeniDurum;

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

                // Alarm sıfırlanır ve limit sayacı baştan başlar
                guncelAlarmMenzili = baslangicAlarmMenzili;
                mevcutBuyumeSayisi = 0;

                AlarmiDalgasiYarat();
                sonAlarmGenislemeZamani = Time.time;

                panikToken++;
                PanikZiplamasiBaslat(panikToken);
                break;
        }
    }

    void Update()
    {
        if (mevcutDurum == PatrolState.Olu || kolajdir == null || algilayiciNokta == null) return;

        Vector2 merkez = kolajdir.bounds.center;
        float yukseklik = kolajdir.bounds.extents.y;
        RaycastHit2D zeminKontrol = Physics2D.Raycast(merkez, Vector2.down, yukseklik + 0.1f, zeminKatmani);
        bool zemindeMi = (zeminKontrol.collider != null);

        if (zemindeMi && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        bool oyuncuyuGoruyor = false;
        if (oyuncuHedef != null)
        {
            Vector3 hedefMerkez = oyuncuHedef.position + Vector3.up * 0.5f;
            float mesafe = Vector2.Distance(merkez, hedefMerkez);
            bool dogruYon = (sagaMiBakiyor == (hedefMerkez.x > merkez.x));

            if (mesafe <= gorusMesafesi && dogruYon)
            {
                Vector2 yon = (hedefMerkez - (Vector3)merkez).normalized;
                RaycastHit2D duvarEngel = Physics2D.Raycast(merkez, yon, mesafe, zeminKatmani);

                if (duvarEngel.collider == null)
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
                NormalZiplama();
            }
        }
        else if (mevcutDurum == PatrolState.Alarm)
        {
            // YENİ: Sadece limite ulaşana kadar alarm dalgası genişler
            if (mevcutBuyumeSayisi < maxAlarmBuyumeSayisi)
            {
                if (Time.time >= sonAlarmGenislemeZamani + alarmGenislemeSuresi)
                {
                    guncelAlarmMenzili += alarmGenislemeMiktari;
                    mevcutBuyumeSayisi++; // Büyüme sayacını artır
                    AlarmiDalgasiYarat();
                    sonAlarmGenislemeZamani = Time.time;
                }
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
            // Kırmızı slime'ları uyandır
            AttackSlimeAI saldirgan = hit.GetComponent<AttackSlimeAI>();
            if (saldirgan != null) saldirgan.AlarmiDuy(this);

            // ZİNCİRLEME ALARM: Diğer devriye slime'ları da uyar
            PatrolEnemyAI baskaGozcu = hit.GetComponent<PatrolEnemyAI>();
            if (baskaGozcu != null && baskaGozcu != this)
            {
                // Zaten ölü veya alarm durumundaysa karışma
                if (baskaGozcu.mevcutDurum != PatrolState.Olu && baskaGozcu.mevcutDurum != PatrolState.Alarm)
                {
                    // Diğer gözcü şüphe barını beklemeden direkt Alarm'a (kırmızıya) geçer ve kendi dalgasını yaymaya başlar
                    baskaGozcu.DurumDegistir(PatrolState.Alarm);
                }
            }
        }
    }

    private async void PanikZiplamasiBaslat(int suAnkiPanikToken)
    {
        while (mevcutDurum == PatrolState.Alarm)
        {
            if (!this || !gameObject.activeInHierarchy || panikToken != suAnkiPanikToken) return;

            if (anaAnimator != null) anaAnimator.Play("slime_panik", -1, 0f);

            float rastgeleYon = Random.value > 0.5f ? 1f : -1f;
            rb.linearVelocity = new Vector2(rastgeleYon * 1.5f, 6f);

            await Awaitable.WaitForSecondsAsync(0.4f);
            if (!this || !gameObject.activeInHierarchy || panikToken != suAnkiPanikToken) return;

            rb.linearVelocity = Vector2.zero;

            await Awaitable.WaitForSecondsAsync(0.3f);
            if (!this || !gameObject.activeInHierarchy || panikToken != suAnkiPanikToken) return;
        }
    }

    private async void NormalZiplama()
    {
        ziplamayaHazirlaniyor = true;

        Vector2 bakis = sagaMiBakiyor ? Vector2.right : Vector2.left;
        RaycastHit2D duvar = Physics2D.Raycast(algilayiciNokta.position, bakis, 0.8f, zeminKatmani);
        RaycastHit2D ucurum = Physics2D.Raycast(algilayiciNokta.position + (Vector3)bakis * 0.5f, Vector2.down, 1.5f, zeminKatmani);

        if (duvar.collider != null || ucurum.collider == null)
        {
            ziplamayaHazirlaniyor = false;
            KenardaDusunVeDon();
            return;
        }

        if (anaAnimator != null) anaAnimator.Play("slime_hareket", -1, 0f);

        await Awaitable.WaitForSecondsAsync(0.4f);
        if (!this || !gameObject.activeInHierarchy || mevcutDurum != PatrolState.Devriye)
        {
            ziplamayaHazirlaniyor = false;
            return;
        }

        float yonCarpani = sagaMiBakiyor ? 1f : -1f;
        rb.linearVelocity = new Vector2(yonCarpani * devriyeZiplamaX, devriyeZiplamaY);
        sonZiplamaZamani = Time.time;
        ziplamayaHazirlaniyor = false;
    }

    private async void KenardaDusunVeDon()
    {
        kenardaBekliyor = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (anaAnimator != null)
        {
            anaAnimator.Play("slime_hareket", -1, 0f);
            anaAnimator.Update(0f);
            anaAnimator.speed = 0f;
        }

        await Awaitable.WaitForSecondsAsync(kenarBeklemeSuresi);
        if (!this || !gameObject.activeInHierarchy || mevcutDurum != PatrolState.Devriye) return;

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
        CanBariSarsinti();

        if (mevcutCan <= 0)
        {
            OlumUygula(saldirganXPos);
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

    private async void CanBariSarsinti()
    {
        if (canBariTransform == null) return;
        float gecenSure = 0f;
        while (gecenSure < 0.15f)
        {
            if (!this || !gameObject.activeInHierarchy || canBariTransform == null) return;
            canBariSarsintiOffset = (Vector3)(Random.insideUnitCircle * 0.15f);
            gecenSure += Time.deltaTime;
            await Task.Yield();
        }
        canBariSarsintiOffset = Vector3.zero;
    }

    private async void OlumUygula(float saldirganXPos)
    {
        DurumDegistir(PatrolState.Olu);

        if (kolajdir != null) kolajdir.enabled = false;

        hasarToken++;
        int suAnkiToken = hasarToken;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.black;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        await Awaitable.WaitForSecondsAsync(0.08f);
        if (!this || !gameObject.activeInHierarchy || hasarToken != suAnkiToken) return;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.white;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float tepmeYonu = transform.position.x < saldirganXPos ? -1f : 1f;
        rb.AddForce(new Vector2(tepmeYonu * hasarYemeGeriTepmeX, hasarYemeGeriTepmeY), ForceMode2D.Impulse);

        if (anaAnimator != null) anaAnimator.Play("slime_olum", -1, 0f);
        if (unlemObjesi != null) unlemObjesi.SetActive(false);
        if (supheBariObjesi != null) supheBariObjesi.SetActive(false);
        if (canBariTransform != null) canBariTransform.gameObject.SetActive(false);

        await Awaitable.WaitForSecondsAsync(1.2f);
        if (!this || !gameObject.activeInHierarchy) return;

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, Application.isPlaying ? guncelAlarmMenzili : baslangicAlarmMenzili);
    }
}