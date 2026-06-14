using UnityEngine;
using TMPro; // TextMeshPro kullanmak için gerekli

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI kaynakYazisi;

    void Update()
    {
        // Her karede GameManager'daki verileri ekrana yazdırıyoruz
        kaynakYazisi.text = string.Format(
            "AP: {0} | İntikal: {1} | Altın: {2} | Taş: {3} | Yemek: {4}",
            GameManager.Instance.aksiyonPuani,
            GameManager.Instance.intikalPuani,
            GameManager.Instance.altin,
            GameManager.Instance.tas,
            GameManager.Instance.yemek
        );
    }
}