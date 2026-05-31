using UnityEngine;
using System.Threading.Tasks;

// Endüstri Standardı Yapay Zeka Durumları
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

    [Header("Gelişmiş Görüş ve Şüphe (Warning)")]
    public float onGorusMesafesi = 15f;
    public float arkaGorusMesafesi = 3.5f;
    public float uyariBeklemeSuresi = 3f;
    public float genisletilmisGorusCarpani = 2f;
    private float orijinalGorusMesafesi;

    [Header("Kesin Nişancı Saldırısı (Lock-on Dash)")]
    public float saldiriTetiklemeMenzili = 1.8f;
    public float saldiriBeklemeSuresi = 1.5f;
    public float saldiriVurusGecikmesi = 0.4f;
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
    private bool kizginMi = false;
    private bool ziplamayaHazirlaniyor = false;

    // YENİ: Eski if blokları bozulmasın diye eklenen pratik köprüler
    private bool olduMu => mevcutDurum == SlimeState.Olu;
    private bool sersemlediMi => mevcutDurum == SlimeState.Sersem;
    private bool saldiriyorMu => mevcutDurum == SlimeState.Saldiri;

    private float sonZiplamaZamani = 0f;
    private float sonSaldiriZamani = 0f;
    private float sonOyuncuyuGormeZamani = 0f;
    private float uyariBaslangicZamani = 0f;

    private Rigidbody2D rb;
    private Vector3 orijinalBoyut;
    private Transform oyuncuHedef;
    private Vector3 canBariSarsintiOffset = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mevcutCan = maxCan;
        orijinalBoyut = transform.localScale;
        orijinalGorusMesafesi = onGorusMesafesi;

        GameObject oyuncuObjesi = GameObject.FindGameObjectWithTag("Player");
        if (oyuncuObjesi != null) oyuncuHedef = oyuncuObjesi.transform;

        if (unlemObjesi != null) unlemObjesi.SetActive(false);

        DurumDegistir(SlimeState.Devriye);
    }

    private void DurumDegistir(SlimeState yeniDurum)
    {
        if (mevcutDurum == SlimeState.Olu) return;

        mevcutDurum = yeniDurum;

        switch (yeniDurum)
        {
            case SlimeState.Devriye:
                kizginMi = false;
                if (anaAnimator != null) anaAnimator.SetBool("KizginMi", false);
                if (unlemObjesi != null) unlemObjesi.SetActive(false);
                break;

            case SlimeState.Agresif:
                kizginMi = true;
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
        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri || ziplamayaHazirlaniyor) return;

        Vector2 bakisYonu = sagaMiBakiyor ? Vector2.right : Vector2.left;
        RaycastHit2D zeminKontrol = Physics2D.Raycast(algilayiciNokta.position, Vector2.down, 1.2f, zeminKatmani);
        bool zemindeMi = (zeminKontrol.collider != null);

        if (zemindeMi && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        bool oyuncuyuGoruyor = false;
        float aktifMenzil = (mevcutDurum == SlimeState.Uyari) ? onGorusMesafesi * genisletilmisGorusCarpani : onGorusMesafesi;

        if (oyuncuHedef != null)
        {
            float oyuncuMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);
            bool oyuncuSagdaMi = (oyuncuHedef.position.x > transform.position.x);
            bool bakisYonuDogruMu = (sagaMiBakiyor == oyuncuSagdaMi);

            if (oyuncuMesafe <= arkaGorusMesafesi)
            {
                oyuncuyuGoruyor = true;
            }
            else if (oyuncuMesafe <= aktifMenzil && bakisYonuDogruMu)
            {
                Vector2 oyuncuyaDogruYön = (oyuncuHedef.position - algilayiciNokta.position).normalized;
                RaycastHit2D duvarEngel = Physics2D.Raycast(algilayiciNokta.position, oyuncuyaDogruYön, oyuncuMesafe, zeminKatmani);
                if (duvarEngel.collider == null) oyuncuyuGoruyor = true;
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
                if (!oyuncuyuGoruyor && Time.time > sonOyuncuyuGormeZamani + 0.5f)
                {
                    DurumDegistir(SlimeState.Uyari);
                    return;
                }

                float hedefeMesafe = Vector2.Distance(transform.position, oyuncuHedef.position);
                if (hedefeMesafe <= saldiriTetiklemeMenzili)
                {
                    if (Time.time >= sonSaldiriZamani + saldiriBeklemeSuresi)
                    {
                        SaldiriUygula();
                    }
                    else
                    {
                        bool oyuncuSagdaMi = (oyuncuHedef.position.x > transform.position.x);
                        if (oyuncuSagdaMi != sagaMiBakiyor) YonCevir();
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
        if (canBariTransform != null && !olduMu)
        {
            float hedefY = transform.position.y + canBariYukseklikOffset;
            Vector3 hedefPozisyon = new Vector3(transform.position.x, hedefY, transform.position.z) + canBariSarsintiOffset;
            canBariTransform.position = Vector3.Lerp(canBariTransform.position, hedefPozisyon, Time.deltaTime * 15f);
        }
    }

    private async void SaldiriUygula()
    {
        DurumDegistir(SlimeState.Saldiri);

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        bool oyuncuSagdaMi = (oyuncuHedef.position.x > transform.position.x);
        if (oyuncuSagdaMi != sagaMiBakiyor) YonCevir();

        Vector2 kilitlenenPozisyon = oyuncuHedef.position;

        if (anaAnimator != null) anaAnimator.SetTrigger("Saldiri");

        await Task.Delay(Mathf.RoundToInt(saldiriVurusGecikmesi * 1000));

        if (this == null || !gameObject.activeInHierarchy) return;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem) return;

        float oyuncununMevcutXFarki = Mathf.Abs(oyuncuHedef.position.x - kilitlenenPozisyon.x);
        float vurmaYonu = sagaMiBakiyor ? 1f : -1f;

        if (oyuncununMevcutXFarki <= kacisToleransiX)
        {
            transform.position = new Vector3(oyuncuHedef.position.x - (vurmaYonu * 0.7f), transform.position.y, transform.position.z);

            PlayerHealth oyuncuCan = oyuncuHedef.GetComponent<PlayerHealth>();
            if (oyuncuCan != null) oyuncuCan.HasarAl(1, transform.position.x);

            await Task.Yield();

            rb.linearVelocity = new Vector2(-vurmaYonu * saldiriSonrasiSekmeX, saldiriSonrasiSekmeY);
        }
        else
        {
            transform.position = new Vector3(kilitlenenPozisyon.x, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(vurmaYonu * devriyeZiplamaX, 2f);
        }

        sonSaldiriZamani = Time.time;
        sonZiplamaZamani = Time.time + 0.5f;

        await Task.Delay(500);
        if (this == null || !gameObject.activeInHierarchy) return;

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
        await Task.Delay(Mathf.RoundToInt(hazirlikSuresi * 1000));

        if (this == null || !gameObject.activeInHierarchy) return;

        if (mevcutDurum == SlimeState.Olu || mevcutDurum == SlimeState.Sersem || mevcutDurum == SlimeState.Saldiri)
        {
            ziplamayaHazirlaniyor = false;
            return;
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
        // HARF HATASI DÜZELTİLDİ: orijinalBoyut
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

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.black;

        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        await Task.Delay(80);
        if (this == null || !gameObject.activeInHierarchy) return;

        if (gövdeSpriteRenderer != null) gövdeSpriteRenderer.color = Color.white;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float yon = transform.position.x < saldirganX ? -1f : 1f;
        rb.linearVelocity = new Vector2(yon * hasarYemeGeriTepmeX, hasarYemeGeriTepmeY);

        await Task.Delay(250);
        if (this == null || !gameObject.activeInHierarchy) return;

        rb.linearVelocity = Vector2.zero;
        DurumDegistir(SlimeState.Agresif);
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

        await Task.Delay(600);
        if (this == null || !gameObject.activeInHierarchy) return;

        if (patlamaEfekti != null) Instantiate(patlamaEfekti, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}