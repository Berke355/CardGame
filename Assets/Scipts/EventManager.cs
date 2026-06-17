using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("UI Elementleri")]
    public GameObject eventPaneli;
    public TextMeshProUGUI eventBaslikYazisi;
    public TextMeshProUGUI eventHikayeYazisi;
    public Button secenekA_Butonu;
    public TextMeshProUGUI secenekA_Yazisi;
    public Button secenekB_Butonu;
    public TextMeshProUGUI secenekB_Yazisi;

    [Header("Özel Kartlar (Unity'den Atanacak)")]
    public CardData piyadeKarti;
    public CardData okcuOrdusuKarti; // YENİ: Gölgeler İçindeki Suikastçılar eventi için

    private GameObject aktifKesifBirligi;
    private GameObject aktifSoruIsareti;
    private int aktifEventID;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (eventPaneli != null) eventPaneli.SetActive(false);
    }

    public void EventPaneliniAc(GameObject kesifBirligi, GameObject soruIsaretiObjesi)
    {
        aktifKesifBirligi = kesifBirligi;
        aktifSoruIsareti = soruIsaretiObjesi;
        
        // 0: Yabanıl Kampı, 1: Gizemli Tüccar, 2: Aç Kaçaklar, 3: Eski Kadim Tapınak, 4: Zehirli Bataklık, 5: Gölgeler İçindeki Suikastçılar
        aktifEventID = Random.Range(0, 6);
        
        eventPaneli.SetActive(true);

        secenekA_Butonu.onClick.RemoveAllListeners();
        secenekB_Butonu.onClick.RemoveAllListeners();

        if (aktifEventID == 0)
        {
            eventBaslikYazisi.text = "Yabanıl Kampı Enkazı";
            eventHikayeYazisi.text = "Karanlık Ormanda terk edilmiş bir yabanıl kampı buldun. İçeride bazı sandıklar var ama etrafta tehlike kokusu da var.";
            
            secenekA_Yazisi.text = "Kampı Araştır\n(%50 İhtimalle: 20 Altın ve 1 Kart | %50 İhtimalle: Ölüm)";
            secenekA_Butonu.onClick.AddListener(() => YabanilA_Secildi());

            secenekB_Yazisi.text = "Risk Alma, Uzaklaş\n(Bir şey olmaz)";
            secenekB_Butonu.onClick.AddListener(() => IptalEt());
        }
        else if (aktifEventID == 1)
        {
            eventBaslikYazisi.text = "Gizemli Tüccar";
            eventHikayeYazisi.text = "Yolda tekerleği kırılmış bir tüccar karavanı buldunuz. Tüccar, elindeki eşsiz bilgileri para karşılığı size satabileceğini söylüyor.";
            
            secenekA_Yazisi.text = "25 Altın Öde\n(Bedeli ödersin, anında 2 yeni kart çekersin)";
            secenekA_Butonu.onClick.AddListener(() => TuccarA_Secildi());

            secenekB_Yazisi.text = "Tüccarı Gasp Et\n(%30 İhtimalle: 15 Altın ve 15 Yemek | %70 İhtimalle: Ölüm)";
            secenekB_Butonu.onClick.AddListener(() => TuccarB_Secildi());
        }
        else if (aktifEventID == 2)
        {
            eventBaslikYazisi.text = "Açlıktan Kırılan Kaçaklar";
            eventHikayeYazisi.text = "Savaştan kaçan ve açlıktan ölmek üzere olan bir grup asker buldunuz. Eğer onları doyurursanız size kalpten bağlanacaklar.";
            
            secenekA_Yazisi.text = "20 Yemek Ver\n(Yemeği feda edersin, eline Piyade kartı gelir)";
            secenekA_Butonu.onClick.AddListener(() => KacaklarA_Secildi());

            secenekB_Yazisi.text = "Kendi Hallerine Bırak\n(%50 İhtimalle 10 Altın kaybedersin)";
            secenekB_Butonu.onClick.AddListener(() => KacaklarB_Secildi());
        }
        else if (aktifEventID == 3)
        {
            eventBaslikYazisi.text = "Eski Kadim Tapınak";
            eventHikayeYazisi.text = "Ormanın derinliklerinde kadim bir tapınak buldunuz. Sunak üzerinde değerli bir el yazması duruyor ancak etrafta kemikler ve lanet söylentileri var.";
            
            secenekA_Yazisi.text = "El Yazmasını Çal\n(3 yeni kart çekersin ama %50 ihtimalle elindeki tüm altınlar kül olur)";
            secenekA_Butonu.onClick.AddListener(() => TapinakA_Secildi());

            secenekB_Yazisi.text = "Sunağa Adak Ada\n(10 Yemek feda et, %100 ihtimalle 1 Kart çek)";
            secenekB_Butonu.onClick.AddListener(() => TapinakB_Secildi());
        }
        else if (aktifEventID == 4)
        {
            eventBaslikYazisi.text = "Zehirli Bataklık";
            eventHikayeYazisi.text = "Bir bataklığa saptınız ve atlar çamura saplanmaya başladı. Karşı tarafta batmış bir karavan ve sandıklar var ama oraya ulaşmak çok tehlikeli.";
            
            secenekA_Yazisi.text = "Gözünü Karart ve Bataklığa Gir\n(%60 ihtimalle 30 Altın ve 20 Taş bulursun | %40 ihtimalle Keşif Birliğin yok olur)";
            secenekA_Butonu.onClick.AddListener(() => BataklikA_Secildi());

            secenekB_Yazisi.text = "Etrafından Dolan\n(Risk almazsın ama 2 İntikal Puanı kaybedersin)";
            secenekB_Butonu.onClick.AddListener(() => BataklikB_Secildi());
        }
        else if (aktifEventID == 5)
        {
            eventBaslikYazisi.text = "Gölgeler İçindeki Suikastçılar";
            eventHikayeYazisi.text = "Gece kamp kurduğunuzda birliğinizin etrafını gölgeler sardı. Liderleri, onlara malzeme vermeniz karşılığında ordunuza katılacaklarını söylüyor.";
            
            secenekA_Yazisi.text = "25 Taş ve 10 Yemek Ver\n(Anlaşma sağlanır, eline 1 adet Okçu Ordusu Kartı gelir)";
            secenekA_Butonu.onClick.AddListener(() => SuikastciA_Secildi());

            secenekB_Yazisi.text = "Onlarla Savaş!\n(%30 ihtimalle yener 20 Altın/Yemek/Taş alırsın | %70 ihtimalle Birliğin yok edilir)";
            secenekB_Butonu.onClick.AddListener(() => SuikastciB_Secildi());
        }
    }

    private void OlayiBitir()
    {
        if (aktifSoruIsareti != null) Destroy(aktifSoruIsareti);
        eventPaneli.SetActive(false);
    }

    private void BirligiOldur()
    {
        Debug.Log("Keşif Birliği öldü!");
        if (aktifKesifBirligi != null) Destroy(aktifKesifBirligi);
        OlayiBitir();
    }

    private void IptalEt()
    {
        Debug.Log("Risk alınmadı, uzaklaşıldı.");
        OlayiBitir();
    }

    // --- YABANIL KAMPI ETKİLERİ ---
    private void YabanilA_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 50)
        {
            GameManager.Instance.altin += 20;
            GameManager.Instance.KartCek(1);
            Debug.Log("Başarılı! 20 Altın ve 1 Kart kazanıldı.");
            OlayiBitir();
        }
        else
        {
            BirligiOldur();
        }
    }

    // --- GİZEMLİ TÜCCAR ETKİLERİ ---
    private void TuccarA_Secildi()
    {
        if (GameManager.Instance.altin >= 25)
        {
            GameManager.Instance.altin -= 25;
            GameManager.Instance.KartCek(2);
            Debug.Log("25 Altın ödendi, 2 Kart çekildi.");
            OlayiBitir();
        }
        else
        {
            Debug.Log("Yeterli altının yok!");
            // Kapanmasın, belki diğer seçeneği seçer
        }
    }

    private void TuccarB_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 30)
        {
            GameManager.Instance.altin += 15;
            GameManager.Instance.yemek += 15;
            Debug.Log("Gasp başarılı! 15 Altın ve 15 Yemek kazanıldı.");
            OlayiBitir();
        }
        else
        {
            BirligiOldur();
        }
    }

    // --- AÇ KAÇAKLAR ETKİLERİ ---
    private void KacaklarA_Secildi()
    {
        if (GameManager.Instance.yemek >= 20)
        {
            GameManager.Instance.yemek -= 20;
            if (piyadeKarti != null)
            {
                // Elimize kartı vermek için GameManager'in Deste sistemini by-pass edip direkt eldeki kart objesi oluşturabiliriz.
                // KartCek metodunu kullanmıyoruz, çünkü desteden rastgele bir şey çekmek yerine özel bir kart vermek istiyoruz.
                GameObject yeniKart = Instantiate(GameManager.Instance.cardPrefab, GameManager.Instance.handArea);
                yeniKart.GetComponent<CardDisplay>().kartVerisi = piyadeKarti;
                GameManager.Instance.eldekiKartObjeleri.Add(yeniKart);
                Debug.Log("20 Yemek feda edildi, Piyade kartı kazanıldı.");
            }
            OlayiBitir();
        }
        else
        {
            Debug.Log("Yeterli yemeğin yok!");
        }
    }

    private void KacaklarB_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 50)
        {
            GameManager.Instance.altin = Mathf.Max(0, GameManager.Instance.altin - 10);
            Debug.Log("Lanetlendiniz! 10 Altın kaybettiniz.");
        }
        else
        {
            Debug.Log("Bir şey olmadı.");
        }
        OlayiBitir();
    }
    // --- ESKİ KADİM TAPINAK ETKİLERİ ---
    private void TapinakA_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 50)
        {
            GameManager.Instance.KartCek(3);
            Debug.Log("Başarılı! El yazmasından 3 yeni kart kazanıldı.");
            OlayiBitir();
        }
        else
        {
            GameManager.Instance.altin = 0; // Tüm altınları kül olur
            Debug.Log("Lanetlendiniz! Tüm altınlarınız küle dönüştü.");
            OlayiBitir();
        }
    }

    private void TapinakB_Secildi()
    {
        if (GameManager.Instance.yemek >= 10)
        {
            GameManager.Instance.yemek -= 10;
            GameManager.Instance.KartCek(1);
            Debug.Log("Adak kabul edildi, 1 Kart kazanıldı.");
            OlayiBitir();
        }
        else
        {
            Debug.Log("Yeterli yemeğin yok!");
        }
    }

    // --- ZEHİRLİ BATAKLIK ETKİLERİ ---
    private void BataklikA_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 60)
        {
            GameManager.Instance.altin += 30;
            GameManager.Instance.tas += 20;
            Debug.Log("Başarılı! Sandıklardan 30 Altın ve 20 Taş çıktı.");
            OlayiBitir();
        }
        else
        {
            BirligiOldur();
        }
    }

    private void BataklikB_Secildi()
    {
        GameManager.Instance.intikalPuani = Mathf.Max(0, GameManager.Instance.intikalPuani - 2);
        Debug.Log("Risk alınmadı ancak bataklık etrafından dolanmak 2 İntikal Puanına mal oldu.");
        OlayiBitir();
    }

    // --- GÖLGELER İÇİNDEKİ SUİKASTÇILAR ETKİLERİ ---
    private void SuikastciA_Secildi()
    {
        if (GameManager.Instance.tas >= 25 && GameManager.Instance.yemek >= 10)
        {
            GameManager.Instance.tas -= 25;
            GameManager.Instance.yemek -= 10;
            
            if (okcuOrdusuKarti != null)
            {
                GameObject yeniKart = Instantiate(GameManager.Instance.cardPrefab, GameManager.Instance.handArea);
                yeniKart.GetComponent<CardDisplay>().kartVerisi = okcuOrdusuKarti;
                GameManager.Instance.eldekiKartObjeleri.Add(yeniKart);
                Debug.Log("Anlaşma sağlandı! Okçu Ordusu kartı kazanıldı.");
            }
            else
            {
                // Eğer oyuncu okcuOrdusuKarti'ni Inspector'dan atamayı unutursa, fallback olarak 1 rastgele kart ver
                GameManager.Instance.KartCek(1);
                Debug.Log("Anlaşma sağlandı! 1 Kart çekildi. (Okçu Ordusu kartı Inspector'a atanmamış)");
            }
            OlayiBitir();
        }
        else
        {
            Debug.Log("Yeterli malzemen yok!");
        }
    }

    private void SuikastciB_Secildi()
    {
        int sans = Random.Range(0, 100);
        if (sans < 30)
        {
            GameManager.Instance.altin += 20;
            GameManager.Instance.yemek += 20;
            GameManager.Instance.tas += 20;
            Debug.Log("Suikastçılar bozguna uğratıldı! 20 Altın, 20 Yemek, 20 Taş kazanıldı.");
            OlayiBitir();
        }
        else
        {
            BirligiOldur();
        }
    }
}
