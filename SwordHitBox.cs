using UnityEngine;
using Unity.Cinemachine;

public class SwordHitbox : MonoBehaviour
{
    public int saldiriHasari = 100;
    public Transform oyuncuTransform; // Inspector'dan Player objeni buraya sürükle
    private CinemachineImpulseSource sarsintiKaynagi;

    void Start()
    {
        if (oyuncuTransform != null)
            sarsintiKaynagi = oyuncuTransform.GetComponent<CinemachineImpulseSource>();
    }

    private void OnTriggerEnter2D(Collider2D temas)
    {
        // Temas eden objede IDamageable var mı kontrol et
        IDamageable hasarAlabilirObje = temas.GetComponent<IDamageable>();
        if (hasarAlabilirObje != null)
        {
            // Hasarı ver
            hasarAlabilirObje.HasarAl(saldiriHasari, oyuncuTransform.position.x);

            // Vuruş Hissi: Ekran sarsıntısı ve Hitstop (Zaman duraksaması)
            if (sarsintiKaynagi != null) sarsintiKaynagi.GenerateImpulseWithForce(1f);
            if (TimeManager.instance != null) TimeManager.instance.HitstopTetikle(0.1f);
        }
    }
}