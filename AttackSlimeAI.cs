
using UnityEngine;
using System.Collections;

public enum SlimeState { Devriye, Agresif, Uyari, Saldiri, Sersem, Olu }

public class AttackSlimeAI : MonoBehaviour
{
    [Header("Temel Ayarlar")]
    public int maxCan = 3;
    private int mevcutCan;

    [Header("Zıplama Güçleri")]
    public float devriyeZiplamaX = 3f;
    public float kizginZiplamaX = 6.5f;
    public float devriyeZiplamaY = 11f;
    public float kizginZiplamaY = 5f;

    [Header("Animasyon Gecikmeleri")]
    public float devriyeComelmeSuresi = 0.375f;
    public float kizginComelmeSuresi = 0.25f;
    public float devriyeToparlanmaSuresi = 1.5f;
    public float kizginToparlanmaSuresi = 0.5f;
    public float olumBeklemeSuresi = 1.2f;

    [Header("Gelişmiş Görüş ve Şüphe (Warning)")]
    public float onGorusMesafesi = 15f;
    public float arkaGorusMesafesi = 3.5f;
    public float uyariBeklemeSuresi = 3f;
    public float genisletilmisGorusCarpani = 2f;
    private float orijinalGorusMesafesi;

    [Header("Kesin Nişancı Saldırısı (Fiziksel Atlama)")]
    public float saldiriTetiklemeMenzili = 2.5f;
    public float saldiriBeklemeSuresi = 1.5f;
    public float saldiriVurusGecikmesi = 0.25f;
    public float saldiriAtilmaHizi = 18f;
    public float kacisToleransiX = 2.5f;

    [Header("Sensörler")]
    public Transform algilayiciNokta;
    public LayerMask zeminKatmani;
    public LayerMask oyuncuKatmani;

    [Header("Geri Tepme (Knockback & Bounce)")]
    public float hasarYemeGeriTepmeX = 15f;
    public float hasarYemeGeriTepmeY = 5f;
    public float saldiriSonrasiSekmeX = 12f;
    public float saldiriSonrasiSekmeY = 8f;

    [Header("Efektler ve Alt Objeler")]
    public Animator anaAnimator;
    public GameObject unlemObjesi;
    public Animator unlemAnimator;
    public Animator canBariAnimator;
    public Transform canBariTransform;
    public float canBariYukseklikOffset = 0.8f;
    public SpriteRenderer gövdeSpriteRenderer;
    public GameObject patlamaEfekti;

    [Header("Yapay Zeka Durum Takibi")]
    public SlimeState mevcutDurum = SlimeState.Devriye;

    public bool sagaMiBakiyor = true;
    private bool ziplamayaHazirlaniyor = false;

    [HideInInspector] public bool alarmIleUyandirildi = false;
    private PatrolSlimeAI aktifAlarmKaynagi = null;

    private float sonZiplamaZamani = 0f;
    private float sonSaldiriZamani = 0f;
    private float sonOyuncuyuGormeZamani = 0f;
    private float uyariBaslangicZamani = 0f;

    private Rigidbody2D rb;
    private Collider2D kolajdir;
    private Vector3 orijinalBoyut;
    private Transform oyuncuHedef;
    private Vector3 canBariSarsintiOffset = Vector3.zero;

    private Coroutine ziplamaRutini;
    private Coroutine saldiriRutini;
    private Coroutine hasarRutini;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kolajdir = GetComponent<Collider2D>();
        mevcutCan = maxCan;
        orijinalBoyut = transform.localScale;
        orijinalGorusMesafesi = onGorusMesafesi;

        GameObject oyuncuObjesi = GameObject.FindGameObjectWithTag("Player");
        if (oyuncuObjesi != null) oyuncuHedef = oyuncuObjesi.transform;

        if (unlemObjesi != null) unlemObjesi.SetActive(false);

        DurumDegistir(SlimeState.Devriye);
    }

    public void AlarmiDuy(PatrolSlimeAI gozcu)
    {
        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem) return;

        aktifAlarmKaynagi = gozcu;
        alarmIleUyandirildi = true;

        if (mevcutDurum != SlimeState.Agresif && mevcutDurum != SlimeState.Saldiri)
        {
            DurumDegistir(SlimeState.Agresif);
        }
    }

    private void DurumDegistir(SlimeState yeniDurum)
    {
        if (mevcutDurum == SlimeState.Olu) return;
        mevcutDurum = yeniDurum;

        if (ziplamaRutini != null) { StopCoroutine(ziplamaRutini); ziplamaRutini = null; }
        if (saldiriRutini != null) { StopCoroutine(saldiriRutini); saldiriRutini = null; }

        ziplamayaHazirlaniyor = false;
        if (anaAnimator != null) anaAnimator.speed = 1f;

        switch (yeniDurum)
        {
            case SlimeState.Devriye:
                if (anaAnimator != null) anaAnimator.SetBool("KizginMi", false);
                if (unlemObjesi != null) unlemObjesi.SetActive(false);
                break;

            case SlimeState.Agresif:
                sonZiplamaZamani = 0f;
                YuzunuOyuncuyaDon();

                if (anaAnimator != null) anaAnimator.SetBool("KizginMi", true);
                if (unlemObjesi != null)
                {
                    unlemObjesi.SetActive(true);
                    if (unlemAnimator != null) unlemAnimator.Play("Unlem_Baslangic", -1, 0f);
                }
                break;

            case SlimeState.Uyari:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                uyariBaslangicZamani = Time.time;
                if (unlemAnimator != null) unlemAnimator.Play("unlem_warning", -1, 0f);
                break;

            case SlimeState.Saldiri:
                break;
        }
    }

    void Update()
    {
        if (this == null || !gameObject.activeInHierarchy || mevcutDurum == SlimeState.Olu) return;
        if (mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri || ziplamayaHazirlaniyor) return;
        if (algilayiciNokta == null || kolajdir == null) return;

        // ALARM İPTAL KONTROLÜ (Gözcü öldüyse veya normale döndüyse Wallhack kapanır)
        if (alarmIleUyandirildi)
        {
            if (aktifAlarmKaynagi == null || aktifAlarmKaynagi.mevcutDurum != PatrolState.Alarm)
            {
                alarmIleUyandirildi = false;
                aktifAlarmKaynagi = null;
            }
        }

        Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;
        Vector2 merkez = kolajdir.bounds.center;
        float yukseklik = kolajdir.bounds.extents.y;
        RaycastHit2D zeminKontrol = Physics2D.Raycast(merkez, Vector2.down, yukseklik + 0.1f, zeminKatmani);
        bool zemindeMi = (zeminKontrol.collider != null);

        if (zemindeMi && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // ==========================================
        // ESKİ KUSURSUZ GÖRÜŞ MANTIĞINA DÖNÜŞ
        // ==========================================
        bool oyuncuyuGoruyor = false;

        if (oyuncuHedef != null)
        {
            float oyuncuMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);
            bool oyuncuSagdaMi = (oyuncuHedef.position.x > transform.position.x);
            bool bakisYonuDogruMu = (sagaMiBakiyor == oyuncuSagdaMi);

            if (alarmIleUyandirildi)
            {
                // SADECE ALARM ÇALIYORSA DUVAR DİNLEMEZ (Wallhack)
                if (oyuncuMesafe <= onGorusMesafesi * 1.5f) oyuncuyuGoruyor = true;
            }
            else
            {
                // NORMAL KENDİ GÖRÜŞÜ
                float aktifMenzil = (mevcutDurum == SlimeState.Uyari) ? onGorusMesafesi * genisletilmisGorusCarpani : onGorusMesafesi;
                float aktifArkaGorus = (mevcutDurum == SlimeState.Agresif || mevcutDurum == SlimeState.Uyari) ? 6f : arkaGorusMesafesi;

                if (oyuncuMesafe <= aktifArkaGorus)
                {
                    oyuncuyuGoruyor = true; // Yakın arkasındaysa kesin görür
                }
                else if (oyuncuMesafe <= aktifMenzil && bakisYonuDogruMu)
                {
                    // Lazerin yere çarpmaması için oyuncunun gövdesine (biraz yukarısına) lazer atıyoruz
                    Vector3 hedefMerkez = oyuncuHedef.position + Vector3.up * 0.5f;
                    Vector2 oyuncuyaDogruYön = (hedefMerkez - algilayiciNokta.position).normalized;
                    RaycastHit2D duvarEngel = Physics2D.Raycast(algilayiciNokta.position, oyuncuyaDogruYön, oyuncuMesafe, zeminKatmani);

                    if (duvarEngel.collider == null)
                    {
                        oyuncuyuGoruyor = true;
                    }
                }
            }

            if (oyuncuyuGoruyor) sonOyuncuyuGormeZamani = Time.time;
        }

        switch (mevcutDurum)
        {
            case SlimeState.Devriye:
                if (oyuncuyuGoruyor)
                {
                    DurumDegistir(SlimeState.Agresif);
                    return;
                }

                if (zemindeMi && Time.time >= sonZiplamaZamani + devriyeToparlanmaSuresi)
                {
                    ziplamaRutini = StartCoroutine(ZiplamaRoutine(bakisYonu));
                }
                break;

            case SlimeState.Agresif:
                if (!oyuncuyuGoruyor && Time.time > sonOyuncuyuGormeZamani + 0.5f)
                {
                    DurumDegistir(SlimeState.Uyari);
                    return;
                }

                float hedefeMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);

                if (hedefeMesafe <= saldiriTetiklemeMenzili && zemindeMi)
                {
                    if (Time.time >= sonSaldiriZamani + saldiriBeklemeSuresi)
                    {
                        saldiriRutini = StartCoroutine(SaldiriRoutine());
                    }
                    else
                    {
                        YuzunuOyuncuyaDon();
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    }
                    return;
                }

                if (zemindeMi && Time.time >= sonZiplamaZamani + kizginToparlanmaSuresi)
                {
                    ziplamaRutini = StartCoroutine(ZiplamaRoutine(bakisYonu));
                }
                break;

            case SlimeState.Uyari:
                if (oyuncuyuGoruyor)
                {
                    DurumDegistir(SlimeState.Agresif);
                    return;
                }

                if (Time.time > uyariBaslangicZamani + uyariBeklemeSuresi)
                {
                    DurumDegistir(SlimeState.Devriye);
                }
                break;
        }
    }

    void LateUpdate()
    {
        if (this == null || !gameObject.activeInHierarchy || mevcutDurum == SlimeState.Olu) return;

        if (canBariTransform != null)
        {
            float hedefY = transform.position.y + canBariYukseklikOffset;
            Vector3 hedefPozisyon = new Vector3(transform.position.x, hedefY, transform.position.z) + canBariSarsintiOffset;
            canBariTransform.position = Vector3.Lerp(canBariTransform.position, hedefPozisyon, Time.deltaTime * 15f);
        }
    }

    private void YuzunuOyuncuyaDon()
    {
        if (oyuncuHedef == null || mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem) return;
        bool oyuncuSagdaMi = (oyuncuHedef.position.x > transform.position.x);
        if (oyuncuSagdaMi != sagaMiBakiyor) YonCevir();
    }

    private IEnumerator SaldiriRoutine()
    {
        DurumDegistir(SlimeState.Saldiri);

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        YuzunuOyuncuyaDon();

        if (anaAnimator != null) anaAnimator.SetTrigger("Saldiri");

        yield return new WaitForSeconds(saldiriVurusGecikmesi);
        if (this == null || mevcutDurum == SlimeState.Olu) yield break;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float vurmaYonu = sagaMiBakiyor ? 1f : -1f;
        rb.linearVelocity = new Vector2(vurmaYonu * saldiriAtilmaHizi, 3f);

        yield return new WaitForSeconds(0.2f);
        if (this == null || mevcutDurum == SlimeState.Olu) yield break;

        float oyuncuyaMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);

        if (oyuncuyaMesafe <= kacisToleransiX)
        {
            PlayerHealth oyuncuCan = oyuncuHedef.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);

            yield return null;
            rb.linearVelocity = new Vector2(-vurmaYonu * saldiriSonrasiSekmeX, saldiriSonrasiSekmeY);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, -5f);
        }

        sonSaldiriZamani = Time.time;
        sonZiplamaZamani = Time.time + 0.5f;

        yield return new WaitForSeconds(0.4f);
        if (this == null || mevcutDurum == SlimeState.Olu) yield break;

        if (mevcutDurum == SlimeState.Saldiri) DurumDegistir(SlimeState.Agresif);
    }

    private IEnumerator ZiplamaRoutine(Vector2 bakisYonu)
    {
        ziplamayaHazirlaniyor = true;

        if (anaAnimator != null)
        {
            string animAdi = (mevcutDurum == SlimeState.Agresif) ? "slime_kizgin_hareket" : "slime_hareket";
            anaAnimator.Play(animAdi, -1, 0f);
        }

        float hazirlikSuresi = (mevcutDurum == SlimeState.Agresif) ? kizginComelmeSuresi : devriyeComelmeSuresi;
        yield return new WaitForSeconds(hazirlikSuresi);

        if (this == null || mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri)
        {
            ziplamayaHazirlaniyor = false;
            yield break;
        }

        if (algilayiciNokta == null) { ziplamayaHazirlaniyor = false; yield break; }

        RaycastHit2D onZeminKontrol = Physics2D.Raycast(algilayiciNokta.position + (Vector3)bakisYonu * 0.5f, Vector2.down, 1.5f, zeminKatmani);
        RaycastHit2D duvarKontrol = Physics2D.Raycast(algilayiciNokta.position, bakisYonu, 0.8f, zeminKatmani);

        if (onZeminKontrol.collider == null || duvarKontrol.collider != null)
        {
            if (mevcutDurum == SlimeState.Devriye)
            {
                YonCevir();
                ziplamayaHazirlaniyor = false;
                yield break;
            }
        }

        float ziplamaX = (mevcutDurum == SlimeState.Agresif) ? kizginZiplamaX : devriyeZiplamaX;
        float ziplamaY = (mevcutDurum == SlimeState.Agresif) ? kizginZiplamaY : devriyeZiplamaY;
        float yonCarpani = sagaMiBakiyor ? 1f : -1f;

        rb.linearVelocity = new Vector2(yonCarpani * ziplamaX, ziplamaY);
        sonZiplamaZamani = Time.time;
        ziplamayaHazirlaniyor = false;
    }

    private void YonCevir()
    {
        sagaMiBakiyor = !sagaMiBakiyor;
        float xBoyutu = Mathf.Abs(orijinalBoyut.x) * (sagaMiBakiyor ? 1f : -1f);
        transform.localScale = new Vector3(xBoyutu, orijinalBoyut.y, orijinalBoyut.z);
    }

    public void HasarAl(int hasar, float saldirganX)
    {
        if (mevcutDurum == SlimeState.Olu) return;

        mevcutCan -= 1;
        CanBariGorseliGuncelle();

        StartCoroutine(CanBariSarsintiRoutine());

        if (mevcutCan <= 0)
        {
            StartCoroutine(OlumRoutine(saldirganX));
        }
        else
        {
            if (hasarRutini != null) StopCoroutine(hasarRutini);
            hasarRutini = StartCoroutine(HasarTepkisiRoutine(saldirganX));
        }
    }

    private void CanBariGorseliGuncelle()
    {
        if (canBariAnimator != null)
        {
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

    private IEnumerator HasarTepkisiRoutine(float saldirganX)
    {
        DurumDegistir(SlimeState.Sersem);
        ziplamayaHazirlaniyor = false;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.black;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        yield return new WaitForSeconds(0.08f);
        if (this == null || mevcutDurum == SlimeState.Olu) yield break;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.white;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float yon = transform.position.x < saldirganX ? -1f : 1f;
        rb.linearVelocity = new Vector2(yon * hasarYemeGeriTepmeX, hasarYemeGeriTepmeY);

        yield return new WaitForSeconds(0.25f);
        if (this == null || mevcutDurum == SlimeState.Olu) yield break;

        rb.linearVelocity = Vector2.zero;
        if (mevcutDurum == SlimeState.Sersem) DurumDegistir(SlimeState.Agresif);
    }

    private IEnumerator OlumRoutine(float saldirganX)
    {
        DurumDegistir(SlimeState.Olu);

        if (kolajdir != null) kolajdir.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (anaAnimator != null) anaAnimator.SetTrigger("Olum");
        if (unlemObjesi != null) unlemObjesi.SetActive(false);
        if (canBariTransform != null) canBariTransform.gameObject.SetActive(false);

        yield return new WaitForSeconds(olumBeklemeSuresi);
        if (this == null) yield break;

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}