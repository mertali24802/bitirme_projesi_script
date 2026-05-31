using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxLayer
    {
        [Tooltip("Kaydırılacak katman")]
        public RectTransform katman;

        [Tooltip("Fare ile sağa/sola ne kadar kayacak?")]
        public float hareketMenzili;

        [Tooltip("Kendi kendine yukarı/aşağı ne kadar dalgalanacak? (Örn: 2 ile 5 arası)")]
        public float dalgaSiddeti;
    }

    [Header("Fare Parallax Ayarları")]
    public ParallaxLayer[] katmanlar;
    public bool tersYoneKaydir = true;
    public float yumusamaHizi = 5f;

    [Header("Otomatik Nefes Alma (Dalgalanma)")]
    public bool dalgalanmaAktif = true;
    [Tooltip("Şehrin inip çıkma hızı (Çok yavaş olmalı, Örn: 1.5)")]
    public float dalgaHizi = 1.5f;

    private Vector2[] baslangicPozisyonlari;

    void Start()
    {
        baslangicPozisyonlari = new Vector2[katmanlar.Length];
        for (int i = 0; i < katmanlar.Length; i++)
        {
            if (katmanlar[i].katman != null)
                baslangicPozisyonlari[i] = katmanlar[i].katman.anchoredPosition;
        }
    }

    void Update()
    {
        // 1. FARE HESAPLAMASI (Eski Parallax sistemimiz)
        float fareX = Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f);
        float fareY = Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f);
        int yon = tersYoneKaydir ? -1 : 1;

        // 2. NEFES ALMA HESAPLAMASI (Yeni Dalgalanma Sistemimiz)
        // Mathf.Sin, zamana göre sürekli -1 ile +1 arasında gidip gelen pürüzsüz bir sayı üretir.
        float nefesKatsayisi = 0f;
        if (dalgalanmaAktif)
        {
            nefesKatsayisi = Mathf.Sin(Time.time * dalgaHizi);
        }

        // 3. İKİSİNİ BİRLEŞTİR VE UYGULA
        for (int i = 0; i < katmanlar.Length; i++)
        {
            if (katmanlar[i].katman == null) continue;

            // X Ekseninde sadece fare etkili olsun
            float eklenecekX = fareX * katmanlar[i].hareketMenzili * yon;

            // Y Ekseninde hem fare hem de nefes alma (dalgalanma) etkili olsun
            float eklenecekY = (fareY * katmanlar[i].hareketMenzili * yon) + (nefesKatsayisi * katmanlar[i].dalgaSiddeti);

            Vector2 hedefPozisyon = baslangicPozisyonlari[i] + new Vector2(eklenecekX, eklenecekY);

            // Yumuşak geçiş ile hedefe kaydır
            katmanlar[i].katman.anchoredPosition = Vector2.Lerp(katmanlar[i].katman.anchoredPosition, hedefPozisyon, Time.deltaTime * yumusamaHizi);
        }
    }
}