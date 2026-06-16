using System.Collections.Generic;
using UnityEngine;

// YENİ: Kartımızın türünü seçebileceğimiz bir açılır liste (Dropdown) yaratıyoruz
public enum KartTuru { Hedefli, Aninda } 

[CreateAssetMenu(fileName = "YeniKart", menuName = "GoT_KartOyunu/Kart")]
public class CardData : ScriptableObject
{
    [Header("Kart Özellikleri")]
    public string kartAdi;
    public KartTuru tur; // Unity Inspector'da Hedefli veya Anında seçebileceğiz
    [TextArea(3, 5)] public string aciklama;
    
    [Header("Birlik / Ordu Çıkarma (Sadece Ordu Kartları İçin)")]
    // Eğer bu kart oynandığında haritaya ordu çıkarıyorsa bunu işaretle
    public bool orduKartiMi = false; 
    // Unity'de bu kartın Inspector listesini açıp "Piyade", "Okcu", "Lekesiz" vs diyerek bu kartın ne çıkaracağını dizebilirsin.
    public List<string> uretilecekBirlikler = new List<string>(); 

    [Header("Bina / İnşaat (Sadece Yapı Kartları İçin)")]
    public bool binaKartiMi = false; 
    [Tooltip("Örn: 'Kale' veya 'Koy' yazın")]
    public string insaEdilecekBina = ""; 

    [Header("Bedeller (Harcamalar)")]
    public int apBedeli;
    public int altinBedeli;
    public int tasBedeli;
    public int yemekBedeli;

    [Header("Kazanımlar (Sadece Anında Kartlar İçin)")]
    public int kazanilanAltin;
    public int kazanilanTas;
    public int kazanilanYemek;
    public int kazanilanIntikal;

    [Header("Toprak Tabanlı Kazanımlar (Yeni Ekonomi)")]
    public bool sinirdanAltinKazan = false;
    public bool sinirdanTasKazan = false;
    public bool sinirdanYemekKazan = false;

    [Header("=======================================")]
    [Header("YENİ MEKANİKLER (ASİMETRİK HANEDAN)")]
    [Header("Kart Çekme ve Atılma")]
    [Tooltip("Tüketim: Oynandıktan sonra atık destesine değil, oyundan silinir.")]
    public bool isTuketimKarti = false; 
    public int cekilecekKartSayisi = 0;

    [Header("Dost Buff (Hedefli Kartlar)")]
    public int orduHasarArtisi = 0; // Ordudaki tüm birliklerin hasarını artırır
    public int orduCanArtisi = 0; // Ordudaki tüm birliklerin canını/maks canını artırır
    public int orduHareketHiziArtisi = 0; // Hareket hızını kalıcı artırır (İntikal)
    public int kaleKapiCaniArtisi = 0; // Maksimum canı kalıcı artırır
    public int kaleOkcuHasariArtisi = 0; // Kuşatma savunma hasarını artırır

    [Header("Dost Özel Yetenekler (Hedefli)")]
    [Tooltip("Örn: Devşir veya Acımasız Emirler için kendi ordunu kalıcı yok etme.")]
    public bool kendiOrdunuFedaEt = false; 
    [Tooltip("Kuzeyin Kralı: Bir ordunun birebir kopyasını eğitir.")]
    public bool ordununKopyasiniUret = false; 
    [Tooltip("Örn: İleri! veya Devriye kartları")]
    public int ekHareketHakki = 0; 
    
    [Header("Düşman Sabotaj (Hedefli)")]
    public bool dusmanHareketEngelle = false; 
    public bool dusmanIntikalSifirla = false;
    public int dusmanBirlikYokEt = 0; // Rüşvet: Ordudan X birlik sil
    public bool dusmanOrduyuCal = false; // Taraf Değiştir: Düşman ordusunu kendi safına çek
    public int kaleyeHasarVer = 0; // Makro haritadayken kuleye doğrudan hasar vur

    [Header("Şartlı Etkiler (Condition - Anında Kartlar)")]
    [Tooltip("Eğer bu tur en az 3 altın harcandıysa (Örn: Borçların Ödenmesi) ekstra kaynak verir")]
    public bool sart3AltinHarcandiysaBonus = false; 
    [Tooltip("Haritada canı 4 veya fazla olan bir kalen varsa (Örn: Kış Hazırlığı) ekstra kaynak verir")]
    public bool sartKuvvetliKaleVarsaBonus = false;
    [Tooltip("Zorla Tahsilat: Kendi ordunu hareket ettirir, Şehir/Köy üzerindeyse altın verir ama can yakar.")]
    public bool zorlaTahsilatEfekti = false;
    
    [Header("İleriki Aşama Mekanikleri (Pasif Bırakılacak)")]
    public bool relicVerir = false;
    public bool desteyeLanetKartiEkle = false;
    public bool rakipDesteSabotaji = false;
}