using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UIElements;

public enum SlimeState { Devriye, Agresif, Uyari, Saldiri, Sersem, Olu }

public class AttackSlimeAI : MonoBehaviour, IDamageable
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
    public float dikeyVurusToleransiY = 2.5f;

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
    private PatrolEnemyAI aktifAlarmKaynagi = null;

    private float sonZiplamaZamani = 0f;
    private float sonSaldiriZamani = 0f;
    private float sonOyuncuyuGormeZamani = 0f;
    private float uyariBaslangicZamani = 0f;

    private Rigidbody2D rb;
    private Vector3 orijinalBoyut;
    private Transform oyuncuHedef;
    private Vector3 canBariSarsintiOffset = Vector3.zero;
    private Collider2D kolajdir;

    private int hasarToken = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kolajdir = GetComponent<Collider2D>();

        rb.mass = 40f;
        mevcutCan = maxCan;
        orijinalBoyut = transform.localScale;
        orijinalGorusMesafesi = onGorusMesafesi;

        GameObject oyuncuObjesi = GameObject.FindGameObjectWithTag("Player");
        if (oyuncuObjesi != null) oyuncuHedef = oyuncuObjesi.transform;

        if (unlemObjesi != null) unlemObjesi.SetActive(false);

        DurumDegistir(SlimeState.Devriye);
    }

    private void OnCollisionEnter2D(Collision2D temas)
    {
        if (mevcutDurum == SlimeState.Olu) return;
        if (temas.gameObject.CompareTag("Player"))
        {
            PlayerHealth oyuncuCan = temas.gameObject.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);
        }
    }

    private void OnCollisionStay2D(Collision2D temas)
    {
        if (mevcutDurum == SlimeState.Olu) return;
        if (temas.gameObject.CompareTag("Player"))
        {
            PlayerHealth oyuncuCan = temas.gameObject.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);
        }
    }

    public void AlarmiDuy(PatrolEnemyAI gozcu)
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
        if (mevcutDurum == yeniDurum) return;

        mevcutDurum = yeniDurum;

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

                if (anaAnimator != null)
                {
                    anaAnimator.SetBool("KizginMi", false);
                    anaAnimator.Play("slime_hareket", -1, 0f);
                }
                break;

            case SlimeState.Saldiri:
                break;
        }
    }

    void Update()
    {
        if (kolajdir == null || algilayiciNokta == null) return;

        // Alarmın kesilip kesilmediğini kontrol et
        if (alarmIleUyandirildi)
        {
            if (aktifAlarmKaynagi == null || !aktifAlarmKaynagi || !aktifAlarmKaynagi.gameObject.activeInHierarchy || aktifAlarmKaynagi.mevcutDurum != PatrolState.Alarm)
            {
                alarmIleUyandirildi = false;
                aktifAlarmKaynagi = null;

                // YENİ EKLENEN KISIM: İSTEDİĞİN MEKANİK
                // Patrol öldüğü veya sustuğu an, alarmdan beslenen slime ANINDA şüpheye düşer ve donma döngüsü kırılır.
                if (mevcutDurum == SlimeState.Agresif || mevcutDurum == SlimeState.Saldiri)
                {
                    DurumDegistir(SlimeState.Uyari);
                }
            }
        }

        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri || ziplamayaHazirlaniyor) return;

        Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;
        Vector2 merkez = kolajdir.bounds.center; // Merkeze alındı (Yere çarpma fix)
        float yukseklik = kolajdir.bounds.extents.y;

        RaycastHit2D zeminKontrol = Physics2D.Raycast(merkez, Vector2.down, yukseklik + 0.1f, zeminKatmani);
        bool zemindeMi = (zeminKontrol.collider != null);

        if (zemindeMi && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        bool oyuncuyuGoruyor = false;

        if (oyuncuHedef != null)
        {
            Vector3 hedefMerkez = oyuncuHedef.position + Vector3.up * 0.5f;
            float oyuncuMesafe = Vector2.Distance(merkez, hedefMerkez);
            bool oyuncuSagdaMi = (hedefMerkez.x > merkez.x);
            bool bakisYonuDogruMu = (sagaMiBakiyor == oyuncuSagdaMi);

            float aktifMenzil = (mevcutDurum == SlimeState.Uyari || mevcutDurum == SlimeState.Agresif) ? onGorusMesafesi * genisletilmisGorusCarpani : onGorusMesafesi;
            float aktifArkaGorus = (mevcutDurum == SlimeState.Agresif || mevcutDurum == SlimeState.Uyari) ? 6f : arkaGorusMesafesi;

            // KUSURSUZ GÖRÜŞ VE WALLHACK MOTORU
            if (alarmIleUyandirildi)
            {
                // Eğer alarm çalıyorsa, mesafe/duvar umursamaz direkt görür (Wallhack)
                oyuncuyuGoruyor = true;
            }
            else
            {
                // Alarm yoksa düzgünce raycast at, ancak yere çarpmaması için merkezden at
                Vector2 oyuncuyaDogruYön = (hedefMerkez - (Vector3)merkez).normalized;
                RaycastHit2D duvarEngel = Physics2D.Raycast(merkez, oyuncuyaDogruYön, oyuncuMesafe, zeminKatmani);

                if (duvarEngel.collider == null)
                {
                    if (oyuncuMesafe <= aktifArkaGorus)
                        oyuncuyuGoruyor = true;
                    else if (oyuncuMesafe <= aktifMenzil && bakisYonuDogruMu)
                        oyuncuyuGoruyor = true;
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
                    ZiplamaHazirlikTetikle(bakisYonu);
                }
                break;

            case SlimeState.Agresif:
                // Kaynak kesildiğinde (veya saklanınca) 0.1 saniyelik toleransla direkt şüpheye düşer (Uyarı)
                if (!oyuncuyuGoruyor && Time.time > sonOyuncuyuGormeZamani + 0.5f)
                {
                    DurumDegistir(SlimeState.Uyari);
                    return;
                }

                YuzunuOyuncuyaDon();

                float hedefeMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);
                float yMesafeUpdate = Mathf.Abs(transform.position.y - oyuncuHedef.position.y);

                if (hedefeMesafe <= saldiriTetiklemeMenzili && yMesafeUpdate <= dikeyVurusToleransiY && zemindeMi)
                {
                    if (Time.time >= sonSaldiriZamani + saldiriBeklemeSuresi)
                    {
                        SaldiriUygula();
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
                    ZiplamaHazirlikTetikle(bakisYonu);
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
        if (canBariTransform != null && mevcutDurum != SlimeState.Olu)
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

    private async void SaldiriUygula()
    {
        DurumDegistir(SlimeState.Saldiri);

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        YuzunuOyuncuyaDon();

        if (anaAnimator != null) anaAnimator.SetTrigger("Saldiri");
        await Awaitable.WaitForSecondsAsync(saldiriVurusGecikmesi);

        if (!this || !gameObject.activeInHierarchy) return;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Uyari) return;

        float vurmaYonu = sagaMiBakiyor ? 1f : -1f;
        rb.linearVelocity = new Vector2(vurmaYonu * saldiriAtilmaHizi, 3f);

        await Awaitable.WaitForSecondsAsync(0.2f);

        if (!this || !gameObject.activeInHierarchy) return;

        float xMesafe = Mathf.Abs(transform.position.x - oyuncuHedef.position.x);
        float yMesafe = Mathf.Abs(transform.position.y - oyuncuHedef.position.y);

        if (xMesafe <= kacisToleransiX && yMesafe <= dikeyVurusToleransiY)
        {
            PlayerHealth oyuncuCan = oyuncuHedef.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);

            await Task.Yield();
            rb.linearVelocity = new Vector2(-vurmaYonu * saldiriSonrasiSekmeX, saldiriSonrasiSekmeY);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, -5f);
        }

        sonSaldiriZamani = Time.time;
        sonZiplamaZamani = Time.time + 0.5f;

        await Awaitable.WaitForSecondsAsync(0.4f);
        if (!this || !gameObject.activeInHierarchy) return;

        if (mevcutDurum == SlimeState.Saldiri) DurumDegistir(SlimeState.Agresif);
    }

    private async void ZiplamaHazirlikTetikle(Vector2 bakisYonu)
    {
        ziplamayaHazirlaniyor = true;

        if (anaAnimator != null)
        {
            string animAdi = (mevcutDurum == SlimeState.Agresif) ? "slime_kizgin_hareket" : "slime_hareket";
            anaAnimator.Play(animAdi, -1, 0f);
        }

        float hazirlikSuresi = (mevcutDurum == SlimeState.Agresif) ? kizginComelmeSuresi : devriyeComelmeSuresi;
        await Awaitable.WaitForSecondsAsync(hazirlikSuresi);

        if (!this || !gameObject.activeInHierarchy) return;

        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri)
        {
            ziplamayaHazirlaniyor = false;
            return;
        }

        RaycastHit2D onZeminKontrol = Physics2D.Raycast(algilayiciNokta.position + (Vector3)bakisYonu * 0.5f, Vector2.down, 1.5f, zeminKatmani);
        RaycastHit2D duvarKontrol = Physics2D.Raycast(algilayiciNokta.position, bakisYonu, 0.8f, zeminKatmani);

        if (onZeminKontrol.collider == null || duvarKontrol.collider != null)
        {
            if (mevcutDurum == SlimeState.Devriye)
            {
                YonCevir();
                ziplamayaHazirlaniyor = false;
                return;
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
        CanBariSarsinti();

        if (mevcutCan <= 0) OlumUygula();
        else HasarTepkisiUygula(saldirganX);
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

    private async void CanBariSarsinti()
    {
        if (canBariTransform == null) return;
        float gecenSure = 0f;
        while (gecenSure < 0.15f)
        {
            canBariSarsintiOffset = (Vector3)(Random.insideUnitCircle * 0.15f);
            gecenSure += Time.deltaTime;
            await Task.Yield();
        }
        canBariSarsintiOffset = Vector3.zero;
    }

    private async void HasarTepkisiUygula(float saldirganX)
    {
        DurumDegistir(SlimeState.Sersem);
        ziplamayaHazirlaniyor = false;

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

        float yon = transform.position.x < saldirganX ? -1f : 1f;
        rb.linearVelocity = new Vector2(yon * hasarYemeGeriTepmeX, hasarYemeGeriTepmeY);

        await Awaitable.WaitForSecondsAsync(0.25f);
        if (!this || !gameObject.activeInHierarchy || hasarToken != suAnkiToken) return;

        rb.linearVelocity = Vector2.zero;
        if (mevcutDurum == SlimeState.Sersem) DurumDegistir(SlimeState.Agresif);
    }

    private async void OlumUygula()
    {
        DurumDegistir(SlimeState.Olu);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (anaAnimator != null) anaAnimator.SetTrigger("Olum");
        if (unlemObjesi != null) unlemObjesi.SetActive(false);
        if (canBariTransform != null) canBariTransform.gameObject.SetActive(false);

        await Awaitable.WaitForSecondsAsync(0.6f);
        if (!this || !gameObject.activeInHierarchy) return;

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}