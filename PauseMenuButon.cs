using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuButon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Görsel Referanslar")]
    public CanvasGroup simsekCanvasGroup;

    private Animator anim;
    private Button btn;
    private bool kilitliMi = false;

    // SimsekKontrol'den transfer edilen mantık
    private bool simsekKullanildi = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        btn = GetComponent<Button>();
    }

    void OnEnable()
    {
        kilitliMi = false;
        simsekKullanildi = false; // Menü her açıldığında sıfırlanır

        if (simsekCanvasGroup != null)
            simsekCanvasGroup.alpha = 1f;

        if (anim != null)
            anim.Play("Normal");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (kilitliMi || !btn.interactable) return;
        if (Input.GetMouseButton(0)) return;

        anim.Play("Highlighted");

        // ŞİMŞEK MANTIĞI: İlk girişte görünür (zaten OnEnable'da 1 oldu), sonraki girişlerde görünmez.
        if (!simsekKullanildi)
        {
            simsekKullanildi = true;
        }
        else if (simsekCanvasGroup != null)
        {
            simsekCanvasGroup.alpha = 0f;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (kilitliMi || !btn.interactable) return;
        anim.Play("Normal");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (kilitliMi || !btn.interactable) return;

        kilitliMi = true;

        if (simsekCanvasGroup != null)
            simsekCanvasGroup.alpha = 0f;

        if (anim != null)
            anim.Play("Clicked");
    }
}