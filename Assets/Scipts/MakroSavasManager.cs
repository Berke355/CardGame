using UnityEngine;
using TMPro; // UI Yazıları için
using UnityEngine.UI; // Butonlar için

public class MakroSavasManager : MonoBehaviour
{
    // Singleton (Her yerden kolay erişim için)
    public static MakroSavasManager Instance;

    [Header("UI Panelleri")]
    public GameObject popUpPaneli;
    public TextMeshProUGUI hedefZirhYazisi;
    public TextMeshProUGUI sonucYazisi;
    public Button zarAtButonu;
    public Button fedaEtButonu;

    [Header("Savaş Verileri")]
    private GameObject aktifHedefObjesi;
    private GameObject saldiranBirlik;
    private int aktifHedefZirhDegeri;
    private bool kartFedaEdildi = false;
    private bool savunmaModuAktif = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Başlangıçta paneli gizle
        if (popUpPaneli != null) popUpPaneli.SetActive(false);
    }

    // MapController veya EnemyAIManager'dan hedef seçildiğinde burası çağrılacak
    public void SavasiBaslat(GameObject saldiran, GameObject hedef, int zirhDegeri, bool savunmaModu = false)
    {
        saldiranBirlik = saldiran;
        aktifHedefObjesi = hedef;
        aktifHedefZirhDegeri = zirhDegeri;
        kartFedaEdildi = false;
        savunmaModuAktif = savunmaModu;

        // UI Güncelle ve Paneli Aç
        if (savunmaModuAktif)
        {
            // Düşman saldırıyor
            if (hedef.GetComponent<MakroKoy>() != null)
                hedefZirhYazisi.text = "Saldıran: Düşman Keşifi\nSenin Köyünün Zırhı: " + zirhDegeri;
            else
                hedefZirhYazisi.text = "Saldıran: Düşman Keşifi\nSenin Birliğinin Zırhı: " + zirhDegeri;
                
            sonucYazisi.text = "Düşman Atak Yapıyor... Savun!";
            fedaEtButonu.interactable = false; // Savunmada feda yok
        }
        else
        {
            // Biz saldırıyoruz
            if (hedef.GetComponent<MakroKoy>() != null)
                hedefZirhYazisi.text = "Hedef: Düşman Köyü\nZırh: " + zirhDegeri;
            else if (hedef.name.Contains("DusmanKesif"))
                hedefZirhYazisi.text = "Hedef: Düşman Keşif Birliği\nZırh: " + zirhDegeri;
            else
                hedefZirhYazisi.text = "Hedef: Haydut Kampı\nZırh: " + zirhDegeri;
                
            sonucYazisi.text = "Zar Atmayı Bekliyor...";
            fedaEtButonu.interactable = true;
        }
        
        zarAtButonu.interactable = true;
        
        popUpPaneli.SetActive(true);
    }

    // Feda Et Butonuna basıldığında
    public void KartFedaEt()
    {
        if (kartFedaEdildi) return;

        // Rastgele bir kart eksiltmek yerine basitçe AP feda etmeyi seçelim (Daha az random, daha taktiksel)
        // Ya da oyuncunun elinden (HandArea) kart silebiliriz. Şimdilik AP feda ettirelim.
        if (GameManager.Instance.aksiyonPuani >= 1)
        {
            GameManager.Instance.aksiyonPuani -= 1;
            kartFedaEdildi = true;
            fedaEtButonu.interactable = false;
            sonucYazisi.text = "1 AP Feda Edildi!\nZara +5 Bonus Eklenecek!";
        }
        else
        {
            sonucYazisi.text = "Yetersiz AP! Feda edemezsin.";
        }
    }

    // Zar At Butonuna basıldığında
    public void ZarAt()
    {
        zarAtButonu.interactable = false;
        fedaEtButonu.interactable = false;

        int zar = Random.Range(1, 21); // 1-20 arası
        int toplamGuc = zar;
        
        if (kartFedaEdildi) toplamGuc += 5;

        Debug.Log($"Savaş: Zar {zar} + Bonus {(kartFedaEdildi ? 5 : 0)} = {toplamGuc} vs Zırh {aktifHedefZirhDegeri}");

        if (toplamGuc >= aktifHedefZirhDegeri)
        {
            // SALDIRAN KAZANDI
            if (savunmaModuAktif) sonucYazisi.text = $"Düşman Zarı: {toplamGuc} ! DÜŞMAN BAŞARILI!\nHedefin Yok Edildi.";
            else sonucYazisi.text = $"Zar: {toplamGuc} ! BAŞARILI!\nHedef Yok Edildi.";
            
            // Bekleyip kapat ve objeyi yok et
            Invoke("ZaferSonucu", 1.5f);
        }
        else
        {
            // SALDIRAN KAYBETTİ
            if (savunmaModuAktif) sonucYazisi.text = $"Düşman Zarı: {toplamGuc} ! DÜŞMAN BAŞARISIZ!\nPüskürtüldü ve Düşman Yok Edildi.";
            else sonucYazisi.text = $"Zar: {toplamGuc} ! BAŞARISIZ!\nKeşif Birliğiniz Yok Edildi!";
            
            // Bekleyip kapat ve birliği yok et
            Invoke("YenilgiSonucu", 1.5f);
        }
    }

    private void ZaferSonucu()
    {
        // Hedefi yok et (Köy veya Haydut)
        if (aktifHedefObjesi != null)
        {
            MakroKoy koy = aktifHedefObjesi.GetComponent<MakroKoy>();
            HaydutKampi kamp = aktifHedefObjesi.GetComponent<HaydutKampi>();
            
            if (koy != null) koy.HasarAl(999); // Tek atma
            else if (kamp != null) kamp.YokOl(); 
            else Destroy(aktifHedefObjesi); // Ne olur ne olmaz
        }
        
        PaneliKapat();
    }

    private void YenilgiSonucu()
    {
        if (saldiranBirlik != null)
        {
            Destroy(saldiranBirlik);
            // Obje yok edildiği için MapController'daki referansı otomatik olarak "null" görünecektir.
        }
        
        PaneliKapat();
    }

    public void PaneliKapat()
    {
        popUpPaneli.SetActive(false);
    }
}
