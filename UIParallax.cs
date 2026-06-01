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
        float fareX = Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f);
        float fareY = Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f);
        int yon = tersYoneKaydir ? -1 : 1;

        float nefesKatsayisi = 0f;
        if (dalgalanmaAktif)
        {
            nefesKatsayisi = Mathf.Sin(Time.time * dalgaHizi);
        }

        for (int i = 0; i < katmanlar.Length; i++)
        {
            if (katmanlar[i].katman == null) continue;

            float eklenecekX = fareX * katmanlar[i].hareketMenzili * yon;

            float eklenecekY = (fareY * katmanlar[i].hareketMenzili * yon) + (nefesKatsayisi * katmanlar[i].dalgaSiddeti);

            Vector2 hedefPozisyon = baslangicPozisyonlari[i] + new Vector2(eklenecekX, eklenecekY);

            katmanlar[i].katman.anchoredPosition = Vector2.Lerp(katmanlar[i].katman.anchoredPosition, hedefPozisyon, Time.deltaTime * yumusamaHizi);
        }
    }
}