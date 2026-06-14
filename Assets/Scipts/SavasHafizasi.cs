using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class SavasHafizasi : MonoBehaviour
{
    [Header("Ordu Verileri")]
    // Savaşa girerken makro haritadan buraya asker isimlerini (Örn: "Milis", "Okcu") dolduracağız
    public List<string> savasaGirecekOrdu = new List<string>();

    public static SavasHafizasi Instance { get; private set; }

    public bool savastanZaferleMiDondu = false;
    public GameObject sonSavasilanObje; 
    public GameObject savasanBizimOrdu; // YENİ: Kendi piyonumuzun referansı
    
    // YENİ: Makro haritadan gelen o anki zemin kaplamasının ismi (Çimen, Kar vs.)
    public string sonSavasilanBiyom; 

    [Header("Geri Dönüş Verileri (Kalıcı Hasar)")]
    public List<string> hayattaKalanBirlikler = new List<string>(); // YENİ: Savaştan dönen askerler burada tutulur

    // Manuel liste yerine, o an açık olan her şeyi otomatik aklında tutacak gizli liste
    private List<GameObject> gizlenenMakroObjeler = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // BiyomAdini varsayılan boş bıraktık ki eski kodlar patlamasın, ama hep yollayacağız.
    public void SavasiBaslat(GameObject dusmanObjesi, string biyomAdi = "")
    {
        sonSavasilanObje = dusmanObjesi;
        sonSavasilanBiyom = biyomAdi;
        savastanZaferleMiDondu = false; 
        gizlenenMakroObjeler.Clear();
        hayattaKalanBirlikler.Clear(); // YENİ: Önceki savaşın ölü/kalan hafızasını temizle

        // ÇÖZÜM 1: Makro haritadaki (oyun anında doğan KLONLAR dahil) HER ŞEYİ otomatik bul ve gizle
        Scene makroSahne = SceneManager.GetActiveScene();
        foreach (GameObject obje in makroSahne.GetRootGameObjects())
        {
            // Eğer obje açıksa (ve Savaş Hafızasının kendisi değilse) gizle ve listeye ekle
            if (obje.activeSelf && obje != this.gameObject) 
            {
                obje.SetActive(false);
                gizlenenMakroObjeler.Add(obje);
            }
        }

        // Savaş sahnesini yükle ve yüklendiğinde "SavasSahnesiYuklendi" fonksiyonunu tetikle
        SceneManager.sceneLoaded += SavasSahnesiYuklendi;
        SceneManager.LoadScene("BattleScene", LoadSceneMode.Additive);
    }

    // ÇÖZÜM 2: Savaş sahnesi yüklendiği an onu "Aktif Sahne" yap!
    private void SavasSahnesiYuklendi(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "BattleScene")
        {
            // Artık doğan tüm askerler bu sahneye ait olacak ve sahneyle beraber yok olacaklar!
            SceneManager.SetActiveScene(scene); 
            SceneManager.sceneLoaded -= SavasSahnesiYuklendi; // İşi biten tetikleyiciyi temizle
        }
    }

    // Butona basıldığında artık direkt dönmüyoruz, bir bekleme süreci (Coroutine) başlatıyoruz
    public void MakroHaritayaDon()
    {
        StartCoroutine(HaritayaDonSuresi());
    }

    // İşlemleri sıraya sokan özel zamanlayıcı fonksiyon
    private System.Collections.IEnumerator HaritayaDonSuresi()
    {
        // 1. ZAFER: Düşman kalesini sil ve ORDU KAYIPLARINI GÜNCELLE
        if (savastanZaferleMiDondu && sonSavasilanObje != null)
        {
            Destroy(sonSavasilanObje);

            // Hayatta kalanları makro taraftaki ordunun çantasına geri aktar
            if (savasanBizimOrdu != null)
            {
                ArmyStats bizimStat = savasanBizimOrdu.GetComponent<ArmyStats>();
                if (bizimStat != null)
                {
                    bizimStat.icindekiBirlikler.Clear();
                    bizimStat.icindekiBirlikler.AddRange(hayattaKalanBirlikler);
                    bizimStat.mevcutCan = hayattaKalanBirlikler.Count; // Canı yeni mevcuduna getir
                    bizimStat.CanYazisiniGuncelle(); // Ekranda da eksildiğini görsün
                }
            }
        }
        // 2. MAĞLUBİYET: Bizim saldıran ordu piyonumuzu makro haritadan sil
        else if (!savastanZaferleMiDondu && savasanBizimOrdu != null)
        {
            Destroy(savasanBizimOrdu);
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("SampleScene"));
        
        // SİHİRLİ SATIR: "BattleScene tamamen silinip yok olana kadar bu satırda bekle, aşağı inme!"
        yield return SceneManager.UnloadSceneAsync("BattleScene");

        // Üstteki işlem tamamen bittikten (savaş sahnesi içindeki EventSystem ile birlikte yok olduktan) sonra makroyu uyandır
        foreach (GameObject obje in gizlenenMakroObjeler)
        {
            if (obje != null) obje.SetActive(true);
        }

        // --- YENİ EKLENEN: ÖDÜL EKRANI TETİKLEYİCİSİ ---
        if (savastanZaferleMiDondu)
        {
            // Savaş sahnesi tamamen kapandıktan ve makro UI geri geldikten sonra ödül panelini açıyoruz
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SavasKazanildiOdulGoster();
            }
        }
    }
}