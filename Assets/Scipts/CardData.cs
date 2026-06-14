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
}