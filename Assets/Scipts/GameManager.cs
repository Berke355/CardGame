using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum HanedanTipi { Belirsiz, Stark, Lannister }
    
    [Header("Hanedan Ayarları")]
    public HanedanTipi seciliHanedan = HanedanTipi.Belirsiz;
    public GameObject hanedanSecimPaneli;

    public static GameManager Instance;

    [Header("Oyuncu Kaynakları")]
    public int aksiyonPuani = 3;  
    public int intikalPuani = 3;  
    public int altin = 10;
    public int tas = 10;
    public int yemek = 10;

    [Header("Oyuncu Ordusu")]
    // ESKİDEN BURADAYDI, ARTIK KULLANILMIYOR: Tüm orduları buraya toplamak hataydı, piyonların içindeki ArmyStats'a taşıdık.
    // public List<string> makroOrduListesi = new List<string>();

    [Header("Kart Sistemi Altyapısı")]
    public GameObject cardPrefab;      
    public Transform handArea;         // Kartların dizileceği yer (HandArea)
    public List<CardData> desteListesi; // Oyuna başlarken sahip olduğun tüm kartlar
    private List<CardData> cekmeDestesi = new List<CardData>();
    private List<CardData> atikDestesi = new List<CardData>();
    private List<GameObject> eldekiKartObjeleri = new List<GameObject>();

    [Header("Panic Button Ayarları")]
    public bool panicKullanildi = false;
    public Button panicButton; // UI'daki butonu buraya bağlayacağız

    [Header("Ödül Sistemi Altyapısı")]
    public List<CardData> tumKartHavuzu; // Oyundaki düşürülünebilecek tüm kartların deposu
    public GameObject odulPaneli; // Savaş bitince ekrana çıkacak siyah kararık alan
    public Transform odulKartlariAlani; // İçinde 3 tane kart Prefab'ının belireceği "Layout Group" alanı

    public CardData secilenKart;
    public GameObject secilenKartinObjesi;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // YENİ: Oyun başladığında UI panelini aç, oyun döngüsünü durdur.
        if (hanedanSecimPaneli != null)
        {
            hanedanSecimPaneli.SetActive(true);
        }
    }

    public void HanedanSec(int hanedanIndex)
    {
        seciliHanedan = (HanedanTipi)hanedanIndex;
        
        if (hanedanSecimPaneli != null)
        {
            hanedanSecimPaneli.SetActive(false);
        }

        if (seciliHanedan == HanedanTipi.Lannister)
        {
            altin += 5; // Lannister pasifi: Diğerlerinden 5 altın fazla başlar
            Debug.Log("[HANEDAN] Lannister seçildi: +5 Bonus Altın!");
        }
        else if (seciliHanedan == HanedanTipi.Stark)
        {
            Debug.Log("[HANEDAN] Stark seçildi: Ormanlardan (Hasat) x2 Yemek Pasifi aktif!");
        }

        // 1- Haritayı Üret
        MapController mapCtrl = Object.FindAnyObjectByType<MapController>();
        if (mapCtrl != null)
        {
            mapCtrl.OyunuBaslat();
        }

        // 2- Oyun başında desteyi hazırla ve turu başlat
        cekmeDestesi.AddRange(desteListesi);
        DesteKaristir(cekmeDestesi);
        YeniTurBaslat(); // İlk tur başlasın
    }

    public void YeniTurBaslat()
    {
        // 0. (YENİ EKLENEN) Eğer oyunda Tur atlarsak önce Karşı Tarafın (AI) oynamasına izin ver.
        if (EnemyAIManager.Instance != null)
        {
            EnemyAIManager.Instance.DusmanTuru();
        }

        // 1. Bizim turumuz başlasın
        aksiyonPuani = 3; 

        // YENİ: Civilization Kuralı - Her turun başında tüm birliklerin hareket hakkını sıfırla
        GameObject[] tumBirlikler = GameObject.FindGameObjectsWithTag("Unit");
        foreach (GameObject birlik in tumBirlikler)
        {
            ArmyStats stats = birlik.GetComponent<ArmyStats>();
            if (stats != null)
            {
                stats.buTurHareketEttiMi = false;
            }
        }

        EliAtigaGonder(); // Elindeki eski kartları temizle
        KartCek(4);       // Yeni 4 kart çek
    }

    public void KartCek(int miktar)
    {
        for (int i = 0; i < miktar; i++)
        {
            if (cekmeDestesi.Count == 0)
            {
                if (atikDestesi.Count == 0) return; // Çekecek kart kalmadı!
                DestedenAtigaTransfer();
            }

            CardData cekilenVeri = cekmeDestesi[0];
            cekmeDestesi.RemoveAt(0);

            // Kartı ekranda oluştur
            GameObject yeniKart = Instantiate(cardPrefab, handArea);
            yeniKart.GetComponent<CardDisplay>().kartVerisi = cekilenVeri;
            eldekiKartObjeleri.Add(yeniKart);
        }
    }

    public void EliAtigaGonder()
    {
        foreach (GameObject kart in eldekiKartObjeleri)
        {
            if (kart != null) // GÜVENLİK KİLİDİ: Eğer kart sahnede hala yaşıyorsa işlem yap
            {
                atikDestesi.Add(kart.GetComponent<CardDisplay>().kartVerisi);
                Destroy(kart);
            }
        }
        eldekiKartObjeleri.Clear();
    }

    void DestedenAtigaTransfer()
    {
        cekmeDestesi.AddRange(atikDestesi);
        atikDestesi.Clear();
        DesteKaristir(cekmeDestesi);
    }

    void DesteKaristir(List<CardData> liste)
    {
        for (int i = 0; i < liste.Count; i++)
        {
            CardData gecici = liste[i];
            int rastgeleIndex = Random.Range(i, liste.Count);
            liste[i] = liste[rastgeleIndex];
            liste[rastgeleIndex] = gecici;
        }
    }

    public void PanicButtonBasildi()
    {
        // Eğer daha önce kullanıldıysa hiçbir şey yapma
        if (panicKullanildi) return;

        // AP'ye hiç dokunmuyoruz. 
        // Mathf.Max(0, deger) fonksiyonu, değer eksiye düşerse onu 0'a sabitler.
        // Yani elimizde 1 Altın varsa (1 - 2 = -1) olacağı için bunu 0 yapar. 
        altin = Mathf.Max(0, altin - 2);
        tas = Mathf.Max(0, tas - 2);
        yemek = Mathf.Max(0, yemek - 2);

        // 3 yeni kart çekiyoruz
        KartCek(3);

        // Butonu pasif (gri) yap ve kilit vur
        panicKullanildi = true;
        panicButton.interactable = false; 
        
        Debug.Log("ACİL DURUM! Eldeki tüm imkanlar feda edildi, 3 yeni kart çekildi.");
    }

    public void KartOynandi()
    {
        if (secilenKart != null && secilenKartinObjesi != null)
        {
            // Eski "Milis Eğit" if() bloğunu ve makroOrduListesi eklemelerini kaldırdık.
            // Ordunun içeriğini dodurma işi artık haritada piyonu yaratırken MapController tarafından yapılıyor.

            atikDestesi.Add(secilenKart); // Kartı sonsuza dek silme, atık destesine koy!
            eldekiKartObjeleri.Remove(secilenKartinObjesi); // Hata almamak için kartı elimizdeki listeden çıkar.
            Destroy(secilenKartinObjesi); // Ekrandan yok et.
            
            secilenKart = null;
            secilenKartinObjesi = null;
        }
    }

    public void SavasKazanildiOdulGoster()
    {
        odulPaneli.SetActive(true); // Siyah ödül ekranını görünür yap
        
        // Önceki savaşlardan kalan ödül kartları (butonları) varsa ekrandan temizle
        foreach (Transform child in odulKartlariAlani)
        {
            Destroy(child.gameObject);
        }

        // 3 Adet rastgele kart seç 
        for (int i = 0; i < 3; i++)
        {
            if (tumKartHavuzu.Count == 0) break; // Havuz boşsa çökmesin diye güvenlik

            int r = Random.Range(0, tumKartHavuzu.Count);
            CardData rastgeleKart = tumKartHavuzu[r];

            // Kartı arayüzde (ödül panelinin içinde) oluştur
            GameObject yeniKart = Instantiate(cardPrefab, odulKartlariAlani);
            CardDisplay displayKodu = yeniKart.GetComponent<CardDisplay>();
            
            displayKodu.kartVerisi = rastgeleKart;
            displayKodu.odulKartiMi = true; // BU ÇOK ÖNEMLİ: Bu karta tıklanınca oyun tahtasına "Milis Eğitme", desteye ekle.
        }
    }

    public void OdulKartiniSec(CardData secilenOdul)
    {
        // 1. Kartı kalıcı destene eklersin (Böylece oyunu ileride kaydetsen bile hep seninle kalır)
        desteListesi.Add(secilenOdul);
        
        // 2. SLAY THE SPIRE KURALI: Kazanılan yeni kart, senin mevcut eline (veya çekme destene) gelmez. Direkt ATIK DESTESİNE gider.
        // Deste karıştırıldığında eline gelme şansı başlar.
        atikDestesi.Add(secilenOdul);
        
        // 3. Paneli kapatıp oyuna devam et
        odulPaneli.SetActive(false);
        Debug.Log("Ödül Alındı! Desteye eklenen kart: " + secilenOdul.kartAdi);
    }

    public void OduluGec()
    {
        // Hiçbir şey eklemeden sadece pencereyi kapat
        odulPaneli.SetActive(false);
        Debug.Log("Ödül geçildi. Deste aynen kaldı.");
    }
}