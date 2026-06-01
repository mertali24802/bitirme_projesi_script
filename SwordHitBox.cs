using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic; 

public class SwordHitbox : MonoBehaviour
{
    public int saldiriHasari = 100;
    public Transform oyuncuTransform;
    private CinemachineImpulseSource sarsintiKaynagi;

    private List<Collider2D> vurulanlarListesi = new List<Collider2D>();

    void Start()
    {
        if (oyuncuTransform != null)
            sarsintiKaynagi = oyuncuTransform.GetComponent<CinemachineImpulseSource>();
    }

    void OnEnable()
    {
        vurulanlarListesi.Clear();
    }

    private void OnTriggerEnter2D(Collider2D temas)
    {
        if (vurulanlarListesi.Contains(temas)) return;

        IDamageable hasarAlabilirObje = temas.GetComponent<IDamageable>();
        if (hasarAlabilirObje != null)
        {
            vurulanlarListesi.Add(temas);

            hasarAlabilirObje.HasarAl(saldiriHasari, oyuncuTransform.position.x);

            if (sarsintiKaynagi != null) sarsintiKaynagi.GenerateImpulseWithForce(1f);
            if (TimeManager.instance != null) TimeManager.instance.HitstopTetikle(0.1f);
        }
    }
}