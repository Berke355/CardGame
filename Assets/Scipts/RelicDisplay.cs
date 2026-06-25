using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RelicDisplay : MonoBehaviour
{
    public RelicData relicVerisi;

    [Header("UI Elemanları")]
    public TextMeshProUGUI adYazisi;
    public TextMeshProUGUI aciklamaYazisi;
    public Image relicGorselImaj; 
    
    // UI Panelinde Buton bileşenine (OnClick) bu fonksiyon bağlanacak
    public void RelicSecildi()
    {
        if (GameManager.Instance != null && relicVerisi != null)
        {
            GameManager.Instance.RelicOdulunuAl(relicVerisi);
        }
    }

    public void EkraniGuncelle()
    {
        if (relicVerisi != null)
        {
            if (adYazisi != null) adYazisi.text = relicVerisi.relicAdi;
            if (aciklamaYazisi != null) aciklamaYazisi.text = relicVerisi.aciklama;
            if (relicGorselImaj != null && relicVerisi.relicGorseli != null) 
            {
                relicGorselImaj.sprite = relicVerisi.relicGorseli;
                relicGorselImaj.color = Color.white;
            }
        }
    }

    void Start()
    {
        EkraniGuncelle();
    }
}
