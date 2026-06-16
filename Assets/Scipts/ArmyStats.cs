using System.Collections.Generic; // YENİ
using UnityEngine;
using TMPro; // YAZI SİSTEMİ İÇİN EKLENDİ

public class ArmyStats : MonoBehaviour
{
    [Header("Ordu Verileri")]
    // YENİ: Makro haritadaki bu piyonun sırt çantası. Savaşa girdiğinde içinden hangi askerlerin çıkacağını tutar.
    public List<string> icindekiBirlikler = new List<string>();

    public int maxCan = 3; // Çanta Kapasitesi
    public int mevcutCan = 3;
    public int hareketMenzili = 3; // 1 İntikal harcayarak kaç hex gideceği
    public int hasarGucu = 1;
    
    // YENİ: Civilization Kuralı - Her birim turda 1 kez yürüyebilir
    public bool buTurHareketEttiMi = false;

    // YENİ: Kart efektleri için kimlik
    public bool dusmanMi = false;

    [Header("Arayüz (UI)")]
    public TMP_Text orduCanYazisi; // Unity'den sürükleyeceğimiz yazı

    void Start()
    {
        // Eğer ordu bir savaştan 3'ten az kişiyle döndüyse, mevcut canı içerideki hayatta kalan askerlere eşitle
        if (icindekiBirlikler.Count > 0)
        {
            mevcutCan = icindekiBirlikler.Count;
        }
        else
        {
            mevcutCan = maxCan; // İlk doğduğunda kapasiteyi fulle
        }
        
        // YENİ EKLENEN KORUMA: Eğer Unity Editor'den sahneye test amaçlı manuel bir asker koyduysan, 
        // kartla üretilmediği için çantasının boş kalmasını ve anında yenilmeni önlemek için yedek listeyi doldur.
        if (icindekiBirlikler.Count == 0 || (icindekiBirlikler.Count == 0 && mevcutCan == 3))
        {
            icindekiBirlikler.Add("Milis");
            icindekiBirlikler.Add("Milis");
            icindekiBirlikler.Add("Milis");
            mevcutCan = 3;
        }

        CanYazisiniGuncelle();
    }

    public void CanYazisiniGuncelle()
    {
        if (orduCanYazisi != null)
        {
            orduCanYazisi.text = mevcutCan + "/" + maxCan;
        }
    }
}