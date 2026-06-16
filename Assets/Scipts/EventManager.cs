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
        
        // 0: Yabanıl Kampı, 1: Gizemli Tüccar, 2: Aç Kaçaklar
        aktifEventID = Random.Range(0, 3);
        
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
}
