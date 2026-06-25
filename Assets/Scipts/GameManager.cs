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
    
    // YENİ: Hanedanlara özel başlangıç desteleri
    public List<CardData> starkBaslangicDestesi;
    public List<CardData> lannisterBaslangicDestesi;
    
    // Oyun başladığında seçilen hanedanın kartlarını buraya dolduracağız
    private List<CardData> oyundakiAnaDeste = new List<CardData>();
    
    private List<CardData> cekmeDestesi = new List<CardData>();
    private List<CardData> atikDestesi = new List<CardData>();
    public List<GameObject> eldekiKartObjeleri = new List<GameObject>(); // YENİ: EventManager'ın erişebilmesi için public yapıldı

    [Header("Panic Button Ayarları")]
    public bool panicKullanildi = false;
    public Button panicButton; // UI'daki butonu buraya bağlayacağız

    [Header("Ödül Sistemi Altyapısı")]
    public List<CardData> tumKartHavuzu; // Oyundaki düşürülünebilecek tüm kartların deposu
    public GameObject odulPaneli; // Savaş bitince ekrana çıkacak siyah kararık alan
    public Transform odulKartlariAlani; // İçinde 3 tane kart Prefab'ının belireceği "Layout Group" alanı

    [Header("İncele (Inspect) Sistemi")]
    public GameObject incelePaneli;
    public TMPro.TextMeshProUGUI inceleBaslikYazisi;
    public TMPro.TextMeshProUGUI inceleIcerikYazisi;
    public List<UnitData> tumBirimlerVeritabani; // YENİ: Inspect panelinde baz statları okumak için
    private GameObject suAnIncelenenHedef; // YENİ: Panelin açık olduğu hedefi hatırla

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
        if (seciliHanedan == HanedanTipi.Stark) oyundakiAnaDeste.AddRange(starkBaslangicDestesi);
        else if (seciliHanedan == HanedanTipi.Lannister) oyundakiAnaDeste.AddRange(lannisterBaslangicDestesi);
        
        cekmeDestesi.AddRange(oyundakiAnaDeste);
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
            // YENİ EKLENEN MEKANİK 1: KART ÇEKME
            if (secilenKart.cekilecekKartSayisi > 0)
            {
                KartCek(secilenKart.cekilecekKartSayisi);
                Debug.Log($"{secilenKart.kartAdi} oynandı, {secilenKart.cekilecekKartSayisi} kart çekildi!");
            }

            // YENİ EKLENEN MEKANİK 2: TÜKET (EXHAUST)
            if (secilenKart.isTuketimKarti)
            {
                // Tüketim kartları atık destesine girmez, bu tur için tamamen oyundan silinir!
                Debug.Log($"{secilenKart.kartAdi} TÜKETİLDİ! (Atık destesine dönmeyecek)");
            }
            else
            {
                atikDestesi.Add(secilenKart); // Kartı sonsuza dek silme, atık destesine koy!
            }

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
        oyundakiAnaDeste.Add(secilenOdul);
        
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
        Debug.Log("Ödül alınmadan geçildi.");
    }

    void Update()
    {
        // İncele paneli açıksa ve U tuşuna basıldıysa
        if (incelePaneli != null && incelePaneli.activeSelf && Input.GetKeyDown(KeyCode.U))
        {
            if (suAnIncelenenHedef != null)
            {
                MakroKale incelenenKale = suAnIncelenenHedef.GetComponent<MakroKale>();
                if (incelenenKale == null) incelenenKale = suAnIncelenenHedef.GetComponentInParent<MakroKale>();
                
                if (incelenenKale != null)
                {
                    incelenenKale.SeviyeAtla();
                }
            }
        }
    }

    public void RelicSeciminiBaslat()
    {
        // TODO: İleride burada 3 adet Relic ekrana gelecek
        Debug.Log("[RELİC SİSTEMİ] Hanedana özel 3 Relic sunuluyor...");
        Debug.Log("[RELİC SİSTEMİ] (Şimdilik sistem altyapısı kuruldu. Relic objeleri eklendiğinde burada UI paneli açılacak.)");
        
        // Şimdilik test amaçlı Incele panelini kapatabiliriz:
        if (incelePaneli != null) incelePaneli.SetActive(false);
    }

    // --- YENİ: İNCELE (INSPECT) SİSTEMİ ---
    public void IncelePaneliniAc(GameObject hedefObj)
    {
        if (incelePaneli == null) return; // Panel henüz atanmadıysa hata verme
        
        suAnIncelenenHedef = hedefObj;
        incelePaneli.SetActive(true);
        ArmyStats ordu = hedefObj.GetComponent<ArmyStats>();
        if (ordu == null) ordu = hedefObj.GetComponentInParent<ArmyStats>();
        
        MakroKale kale = hedefObj.GetComponent<MakroKale>();
        if (kale == null) kale = hedefObj.GetComponentInParent<MakroKale>();

        if (ordu != null)
        {
            inceleBaslikYazisi.text = (ordu.dusmanMi ? "Düşman Ordusu " : "Dost Ordu ") + $"({ordu.mevcutCan}/{ordu.maxCan})";
            
            string icerik = "Birliğin İçindekiler:\n\n";
            foreach (string birlik in ordu.icindekiBirlikler)
            {
                // Veritabanından baz statları bul
                int hasar = 0;
                int can = 0;
                UnitData bazVeri = null;
                if (tumBirimlerVeritabani != null)
                {
                    bazVeri = tumBirimlerVeritabani.Find(v => v != null && v.birimAdi == birlik);
                }
                
                if (bazVeri != null)
                {
                    hasar = bazVeri.hasar;
                    can = bazVeri.maxCan;
                }
                else
                {
                    Debug.Log($"[BUFF TEST] '{birlik}' veritabani listesinde BULUNAMADI! Lutfen Isimleri kontrol et (Bosluk olabilir).");
                }
                
                // Eğer buff/debuff varsa parantez içinde matematiksel olarak göster
                string hasarYazisi = (ordu.ekstraBirlikHasari > 0) ? $"{hasar} + <color=green>{ordu.ekstraBirlikHasari}</color>" : (ordu.ekstraBirlikHasari < 0) ? $"{hasar} - <color=red>{Mathf.Abs(ordu.ekstraBirlikHasari)}</color>" : $"{hasar}";
                string canYazisi = (ordu.ekstraBirlikCani > 0) ? $"{can} + <color=green>{ordu.ekstraBirlikCani}</color>" : (ordu.ekstraBirlikCani < 0) ? $"{can} - <color=red>{Mathf.Abs(ordu.ekstraBirlikCani)}</color>" : $"{can}";
                
                Debug.Log($"[BUFF TEST] Birlik Eklendi: {birlik} - Hasar: {hasarYazisi}");
                icerik += $"- {birlik} (Hasar: {hasarYazisi}, Can: {canYazisi})\n";
            }
            if (ordu.icindekiBirlikler.Count == 0) icerik += "- (Boş)\n";
            
            icerik += $"\nMakro Hasar Gücü: {ordu.hasarGucu}\nİntikal: {ordu.hareketMenzili}";
            inceleIcerikYazisi.text = icerik;
        }
        else if (kale != null)
        {
            inceleBaslikYazisi.text = "Kale Bilgisi";
            string icerik = $"Seviye: {kale.kaleSeviyesi} / {kale.maxSeviye}\n";
            icerik += $"Kapı Canı: {kale.kapiCani} / {kale.maxKapiCani}\n\n";
            if (kale.kaleSeviyesi < kale.maxSeviye)
            {
                icerik += "<color=yellow>Kaleyi geliştirip Hanedan Relic'i seçmek için 'U' (Upgrade) tuşuna basın. (Bedel: 30 Altın, 20 Taş)</color>";
            }
            inceleIcerikYazisi.text = icerik;
        }
    }

    public void GuncelleIncelePaneli()
    {
        Debug.Log($"[BUFF TEST] GuncelleIncelePaneli cagirildi. panel={incelePaneli?.name}, panel.activeSelf={incelePaneli?.activeSelf}, hedef={suAnIncelenenHedef?.name}");
        if (incelePaneli != null && incelePaneli.activeSelf && suAnIncelenenHedef != null)
        {
            Debug.Log("[BUFF TEST] Panel ve hedef gecerli, IncelePaneliniAc tekrar cagiriliyor...");
            IncelePaneliniAc(suAnIncelenenHedef);
        }
    }

    public void IncelePaneliniKapat()
    {
        suAnIncelenenHedef = null;
        if (incelePaneli != null) incelePaneli.SetActive(false);
    }
}