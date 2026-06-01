using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; 

public class LevelEndDoor : MonoBehaviour
{
    [Tooltip("Az önce HUDGenel içine oluşturduğumuz uyarı yazısını buraya sürükle")]
    public TextMeshProUGUI uyariMetniObjesi;

    private void OnTriggerEnter2D(Collider2D temas)
    {
        if (temas.CompareTag("Player"))
        {
            GameObject[] hayattakiDusmanlar = GameObject.FindGameObjectsWithTag("Enemy");
            int kalanDusmanSayisi = hayattakiDusmanlar.Length;

            if (kalanDusmanSayisi > 0)
            {
                StartCoroutine(UyariGoster(kalanDusmanSayisi));
            }
            else
            {
                Time.timeScale = 1f;

                PlayerMovement pm = temas.GetComponent<PlayerMovement>();
                if (pm != null) pm.enabled = false;

                PlayerCombat pc = temas.GetComponent<PlayerCombat>();
                if (pc != null) pc.enabled = false;

                if (SceneFadeManager.instance != null)
                {
                    SceneFadeManager.instance.SahneYukle("EndScene");
                }
                else
                {
                    SceneManager.LoadScene("EndScene");
                }
            }
        }
    }

    private IEnumerator UyariGoster(int sayi)
    {
        if (uyariMetniObjesi != null)
        {
            uyariMetniObjesi.text = "Gecit kapali! Temizlenmesi gereken " + sayi + " dusman var!";
            uyariMetniObjesi.gameObject.SetActive(true);

            yield return new WaitForSeconds(2.5f);

            uyariMetniObjesi.gameObject.SetActive(false);
        }
    }
}