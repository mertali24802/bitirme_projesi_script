using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Buton Referansları")]
    public Button btnYeniOyun;
    public Button btnDevamEt;
    public Button btnCikis;

    [Header("Ayarlar")]
    public string anaOyunSahnesiAdi = "SampleScene";

    // BAŞLANGIÇ KALKANI: İlk saliselerde oluşan fare/glitch hatalarını engeller
    private bool sistemHazir = false;

    void Start()
    {
        if (btnDevamEt != null)
        {
            btnDevamEt.interactable = SaveManager.KayitVarMi();
        }

        ButonAnimasyonlariniKur(btnYeniOyun);
        ButonAnimasyonlariniKur(btnDevamEt);
        ButonAnimasyonlariniKur(btnCikis);

        // Oyun açıldıktan 0.2 saniye sonra fare dinlemeyi aktif et (Glitch'i %100 önler)
        Invoke("SistemiAktifEt", 0.2f);
    }

    private void SistemiAktifEt()
    {
        sistemHazir = true;
    }

    private void ButonAnimasyonlariniKur(Button btn)
    {
        if (btn == null) return;

        Animator anim = btn.GetComponent<Animator>();
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // ÜZERİNE GELİNCE
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => {
            if (btn.interactable && sistemHazir) anim.Play("Highlighted");
        });
        trigger.triggers.Add(enter);

        // ÇIKINCA
        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => {
            if (btn.interactable && sistemHazir) anim.Play("Button_Hover_Exit");
        });
        trigger.triggers.Add(exit);

        // TIKLANINCA (Hatayı çözdüğümüz "Pressed" kısmı)
        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((data) => {
            if (btn.interactable && sistemHazir) anim.Play("Pressed");
        });
        trigger.triggers.Add(down);
    }

    public void YeniOyun()
    {
        SaveManager.KaydiSil();
        SceneFadeManager.instance.SahneYukle(anaOyunSahnesiAdi);
    }

    public void DevamEt()
    {
        if (SaveManager.KayitVarMi())
        {
            SceneFadeManager.instance.SahneYukle(anaOyunSahnesiAdi);
        }
    }

    public void OyundanCikis()
    {
        Application.Quit();
    }
}