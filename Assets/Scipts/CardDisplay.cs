using UnityEngine;
using TMPro; // Yazıları kontrol etmek için gerekli

public class CardDisplay : MonoBehaviour
{
    [Header("Kart Verisi")]
    public CardData kartVerisi; // ScriptableObject dosyamızı buraya koyacağız

    [Header("Ödül Sistemi")]
    public bool odulKartiMi = false; 

    [Header("Arayüz Bağlantıları")]
    public TextMeshProUGUI isimYazisi;
    public TextMeshProUGUI aciklamaYazisi;
    public TextMeshProUGUI bedelYazisi;

    void Start()
    {
        // Oyun başladığında kart verilerini ekrandaki yazılara aktar
        if (kartVerisi != null)
        {
            isimYazisi.text = kartVerisi.kartAdi;
            aciklamaYazisi.text = kartVerisi.aciklama;
            
            // Dinamik Bedel Metni Oluşturma
            string bedelMetni = kartVerisi.apBedeli + " AP";
            
            if (kartVerisi.yemekBedeli > 0) bedelMetni += " | " + kartVerisi.yemekBedeli + " Yemek";
            if (kartVerisi.tasBedeli > 0) bedelMetni += " | " + kartVerisi.tasBedeli + " Taş";
            if (kartVerisi.altinBedeli > 0) bedelMetni += " | " + kartVerisi.altinBedeli + " Altın";
            
            bedelYazisi.text = bedelMetni;
        }
    }

    public void KartiSec()
    {
        // YENİ EKLENEN: Eğer bu kart ödül ekranında seçilen bir kartsa, oynamaya çalışma, sakla!
        if (odulKartiMi)
        {
            GameManager.Instance.OdulKartiniSec(kartVerisi);
            return;
        }

        // DURUM 1: HEDEFLİ KART (Eski sistem - Haritaya tıklamayı bekler)
        if (kartVerisi.tur == KartTuru.Hedefli)
        {
            GameManager.Instance.secilenKart = kartVerisi;
            GameManager.Instance.secilenKartinObjesi = gameObject; 
            Debug.Log(kartVerisi.kartAdi + " kartı seçildi! Haritaya tıkla.");
        }
        // DURUM 2: ANINDA KART (Ekonomi kartları vb.)
        else if (kartVerisi.tur == KartTuru.Aninda)
        {
            // Sadece AP kontrolü yapıyoruz (Ekonomi kartları genelde sadece AP harcar)
            if (GameManager.Instance.aksiyonPuani >= kartVerisi.apBedeli)
            {
                // YENİ EKONOMİ: Toprak tabanlı kazanç var mı kontrol et
                MapController mapController = Object.FindAnyObjectByType<MapController>();
                int topraktanAltin = 0;
                int topraktanTas = 0;
                int topraktanYemek = 0;

                if (mapController != null)
                {
                    if (kartVerisi.sinirdanAltinKazan) topraktanAltin = mapController.SinirlarIcindekiTileSayisi(mapController.altinTile);
                    if (kartVerisi.sinirdanTasKazan) topraktanTas = mapController.SinirlarIcindekiTileSayisi(mapController.tasTile);
                    if (kartVerisi.sinirdanYemekKazan) topraktanYemek = mapController.SinirlarIcindekiTileSayisi(mapController.ormanTile);
                }

                // 1. Bedeli Öde
                GameManager.Instance.aksiyonPuani -= kartVerisi.apBedeli;
                
                // 2. Kazanımları Al (Eski sabit kazanımlar + Yeni toprak kazanımları)
                GameManager.Instance.altin += kartVerisi.kazanilanAltin + topraktanAltin;
                GameManager.Instance.tas += kartVerisi.kazanilanTas + topraktanTas;
                GameManager.Instance.yemek += kartVerisi.kazanilanYemek + topraktanYemek;
                GameManager.Instance.intikalPuani += kartVerisi.kazanilanIntikal;

                // 3. Kartı Oynandı Say ve Atığa Gönder
                GameManager.Instance.secilenKart = kartVerisi;
                GameManager.Instance.secilenKartinObjesi = gameObject;
                GameManager.Instance.KartOynandi();

                Debug.Log(kartVerisi.kartAdi + " oynandı! Kaynaklar eklendi.");
            }
            else
            {
                Debug.Log("Yetersiz AP! " + kartVerisi.kartAdi + " oynamak için " + kartVerisi.apBedeli + " AP lazım.");
            }
        }
    }
}