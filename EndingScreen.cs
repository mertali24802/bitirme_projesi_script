using UnityEngine;
using System.Collections;

public class EndingScreen : MonoBehaviour
{
    public float dalgaHizi = 2f;
    public float dalgaSiddeti = 15f; 

    private RectTransform yaziRect;
    private float baslangicY;

    void Start()
    {
        yaziRect = GetComponent<RectTransform>();
        if (yaziRect != null) baslangicY = yaziRect.anchoredPosition.y;

        StartCoroutine(AnaMenuyeDon());
    }

    void Update()
    {
        if (yaziRect != null)
        {
            float yeniY = baslangicY + Mathf.Sin(Time.time * dalgaHizi) * dalgaSiddeti;
            yaziRect.anchoredPosition = new Vector2(yaziRect.anchoredPosition.x, yeniY);
        }
    }

    private IEnumerator AnaMenuyeDon()
    {
        yield return new WaitForSeconds(5f);

        if (SceneFadeManager.instance != null)
        {
            SceneFadeManager.instance.SahneYukle("AnaMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("AnaMenu");
        }
    }
}