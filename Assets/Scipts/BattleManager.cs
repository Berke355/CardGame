using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Sahneler arası geçiş için gerekli
using TMPro; // Yazıları değiştirmek için gerekli

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Etkileşim")]
    public BattleUnit seciliBirim;

    [Header("Savaş Öncesi Kurulum")]
    public GameObject milisPrefab; // Inspector'dan Piyade prefabını sürükleyeceğiz
    // İleride buraya public GameObject okcuPrefab; vs. ekleyebiliriz.

    public Transform[] oyuncuBaslangicNoktalari; // Askerlerin doğacağı kareler

    [Header("Savaş Sonu UI")]
    public GameObject savasSonuPaneli;
    public TMP_Text savasSonuYazisi;

    [Header("Yetenek Sistemi (Ateşli Ok)")]
    public GameObject yetenekButonu;
    public TMP_Text yetenekButonuYazisi;
    public GameObject yananOrmanPrefab;
    public bool atesliOkAktifMi = false;

    [Header("Tur Sistemi")]
    public bool oyuncuTuru = true; // Oyun bizimle başlar
    public bool savasBittiMi = false;

    [Header("Harita Biyom Ayarları")]
    public int genislik = 15; // YENİ: Harita 15x10'a büyütüldü
    public int yukseklik = 10;
    public float tileBoyutu = 1.05f;

    [System.Serializable]
    public class BiyomVerisi
    {
        public string biyomAdi; // Makro haritadaki tile ismi, örn: "Kar"
        public GameObject[] tilePrefablari; // Kar, buz, kütük vb. prefablari atılacak
    }
    
    [Header("Biyom Veritabanı")]
    public List<BiyomVerisi> biyomlar = new List<BiyomVerisi>();
    public GameObject varsayilanTilePrefab; // Eğer eşleşen biyom bulamazsa (failsafe)

    [Header("Birim Ayarları (Test)")]
    public GameObject unitPrefab;
    public UnitData piyadeVerisi;
    public UnitData balistaVerisi;
    public UnitData kaleKapisiVerisi;
    
    [Header("Birim Veritabanı")]
    // YENİ: Oyundaki tüm asker türlerinin Inspector'da sürükleneceği dev havuz
    public List<UnitData> tumBirimlerVeritabani;

    [Header("Savaşçı Listeleri")]
    public List<BattleUnit> oyuncuBirimleri = new List<BattleUnit>();
    public List<BattleUnit> dusmanBirimleri = new List<BattleUnit>();

    // Haritadaki kareleri X ve Y koordinatıyla bulmamızı sağlayacak hafıza (Matris)
    public BattleTile[,] grid; 

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        HaritayiOlustur();
        KamerayiOrtala();
        AskerleriDiz(); // Askerleri sahaya sürelim!
    }

    void HaritayiOlustur()
    {
        grid = new BattleTile[genislik, yukseklik]; // 10x10'luk hafızayı aç

        // YENİ BİYOM SİSTEMİ: Savaş hafızasından gelen stringi kendi veritabanımızda arayalım
        string okunanBiyom = "";
        BiyomVerisi secilenBiyom = null;
        if (SavasHafizasi.Instance != null && !string.IsNullOrEmpty(SavasHafizasi.Instance.sonSavasilanBiyom))
        {
            okunanBiyom = SavasHafizasi.Instance.sonSavasilanBiyom;
            foreach (var b in biyomlar)
            {
                if (b.biyomAdi == okunanBiyom) { secilenBiyom = b; break; }
            }
        }

        for (int x = 0; x < genislik; x++)
        {
            for (int y = 0; y < yukseklik; y++)
            {
                Vector2 pozisyon = new Vector2(x * tileBoyutu, y * tileBoyutu);
                
                // 1. Askerlerin Başlangıç Konumu Gerekçesiyle "Güvenli Bölge" mi?
                bool baslangicNoktasiMi = false;
                
                // Kale ve Balista Doğuş Noktaları (Harita genişlediği için sağa kaydı)
                if ((x == 13 && y == 5) || (x == 13 && y == 3)) baslangicNoktasiMi = true; 
                
                // Meydan Muharebesi Ordusu Doğuş Noktaları
                if ((x == 13 && y == 4) || (x == 13 && y == 6) || (x == 14 && y == 5)) baslangicNoktasiMi = true;

                // Failsafe oyuncu doğuşu
                if (x == 1 && y == 2) baslangicNoktasiMi = true;
                if (oyuncuBaslangicNoktalari != null)
                {
                    foreach (var dogus in oyuncuBaslangicNoktalari)
                    {
                        if (dogus == null) continue;
                        int dogusX = Mathf.RoundToInt(dogus.position.x / tileBoyutu);
                        int dogusY = Mathf.RoundToInt(dogus.position.y / tileBoyutu);
                        if (x == dogusX && y == dogusY) baslangicNoktasiMi = true;
                    }
                }

                GameObject seciliPrefab = varsayilanTilePrefab;
                
                // 2. Rastgele Zemin/Engel Seçimi (%15 ihtimalle ve güvenli bölge değilse)
                if (!baslangicNoktasiMi && secilenBiyom != null && secilenBiyom.tilePrefablari.Length > 0)
                {
                    if (Random.value < 0.15f) // %15 Şans
                    {
                        int rastgeleIndex = Random.Range(0, secilenBiyom.tilePrefablari.Length);
                        seciliPrefab = secilenBiyom.tilePrefablari[rastgeleIndex];
                    }
                }

                GameObject yeniTile = Instantiate(seciliPrefab, pozisyon, Quaternion.identity);
                yeniTile.transform.SetParent(this.transform);
                
                BattleTile tileKodu = yeniTile.GetComponent<BattleTile>();
                tileKodu.Setup(x, y);
                // NOT: Artık engel/zemin bilgisini Inspector'daki prefabın kendisinden okuyoruz.
                // tileKodu.engelMi = engelYapildi; kodunu sildik.
                
                grid[x, y] = tileKodu; // Kareyi hafızaya kaydet
            }
        }
        
        Debug.Log($"[BİYOM SİSTEMİ] Savaş Haritası oluşturuldu. Uygulanan Biyom: {(secilenBiyom != null ? secilenBiyom.biyomAdi : "Varsayılan")}");
    }

    void KamerayiOrtala()
    {
        float merkezX = (genislik * tileBoyutu) / 2f - (tileBoyutu / 2f);
        float merkezY = (yukseklik * tileBoyutu) / 2f - (tileBoyutu / 2f);
        Camera.main.transform.position = new Vector3(merkezX, merkezY, -10f);
        Camera.main.orthographicSize = 6f;
    }

    void AskerleriDiz()
    {
        // 1. MAKRONDAN MI GELDİK? (Çanta dolu mu?)
        if (SavasHafizasi.Instance != null && SavasHafizasi.Instance.savasaGirecekOrdu.Count > 0)
        {
            int index = 0;
            
            // Çantadaki her bir asker tipi ("Milis" vs.) için bu döngü çalışacak
            foreach (string askerTipi in SavasHafizasi.Instance.savasaGirecekOrdu)
            {
                // Eğer belirlediğimiz başlangıç noktalarından daha fazla askerimiz varsa, kalanları doğurma
                if (index >= oyuncuBaslangicNoktalari.Length) break; 

                Transform dogusNoktasi = oyuncuBaslangicNoktalari[index];
                
                // ÇOK ÖNEMLİ: Objenin uzaydaki yerini (Örn: 2.1f), Grid koordinatına (X:2) çeviriyoruz
                int kareX = Mathf.RoundToInt(dogusNoktasi.position.x / tileBoyutu);
                int kareY = Mathf.RoundToInt(dogusNoktasi.position.y / tileBoyutu);

                // YENİ DİNAMİK SİSTEM: Veritabanında (tumBirimlerVeritabani) ismi eşleşen veriyi bul
                UnitData yaratilacakVeri = null;
                foreach (UnitData veri in tumBirimlerVeritabani)
                {
                    // Boş değilse ve birimAdi, senin yazdığınla birebir eşleşiyorsa (Büyük/Küçük harf duyarlı)
                    if (veri != null && veri.birimAdi == askerTipi)
                    {
                        yaratilacakVeri = veri;
                        break;
                    }
                }

                // YENİ: Göçmen için özel koruma. Unity'den ScriptableObject oluşturmayı unuttuysan bile kod otomatik tanımlar.
                if (yaratilacakVeri == null && askerTipi == "Gocmen")
                {
                    yaratilacakVeri = ScriptableObject.CreateInstance<UnitData>();
                    yaratilacakVeri.birimAdi = "Gocmen";
                    yaratilacakVeri.maxCan = 1;
                    yaratilacakVeri.hasar = 0;
                    yaratilacakVeri.zirhDegeri = 5; // En düşük zırh (Hemen ölür)
                    yaratilacakVeri.isabetDegeri = 0;
                    yaratilacakVeri.hareketMenzili = 2; // Makro haritadaki ile savaş alanındaki menzili aynı oranda
                    yaratilacakVeri.saldiriMenzili = 1; // Savaşamasa bile hata vermemesi için
                }

                if (yaratilacakVeri != null)
                {
                    // Orijinal yaratım fonksiyonunu kullanarak birimi doğur (true = bizim askerimiz)
                    BirimYarat(yaratilacakVeri, kareX, kareY, true);
                }
                else
                {
                    Debug.LogError($"HATA: '{askerTipi}' adında bir birim veritabanında (BattleManager) bulunamadı! Lütfen yazım hatası yapmadığından emin ol.");
                }
                
                index++;
            }
        }
        else
        {
            // 2. DİREKT SAVAŞ SAHNESİNİ Mİ TEST EDİYORUZ? (Çanta boşsa)
            // Makrodan gelmediğinde savaşın hemen "Mağlubiyet" ile bitmemesi için 1 tane test askeri koy
            BirimYarat(piyadeVerisi, 1, 2, true); 
        }

        // --- DÜŞMANLARI YERLEŞTİRME ---
        bool kaleyeMiSaliyoruz = false;
        if (SavasHafizasi.Instance != null && SavasHafizasi.Instance.sonSavasilanObje != null)
        {
            if (SavasHafizasi.Instance.sonSavasilanObje.CompareTag("Kale") || SavasHafizasi.Instance.sonSavasilanObje.GetComponent<MakroKale>() != null)
            {
                kaleyeMiSaliyoruz = true;
            }
        }
        else 
        {
            // SavasHafizasi yoksa direkt Scene test ediliyordur, varsayılan olarak Kale diyelim ki hata vermesin
            kaleyeMiSaliyoruz = true; 
        }

        if (kaleyeMiSaliyoruz)
        {
            // Kale Kuşatması
            BirimYarat(kaleKapisiVerisi, 13, 5, false); // Düşman Kale Kapısı
            BirimYarat(balistaVerisi, 13, 3, false);    // Düşman Balistası
        }
        else
        {
            // Meydan Muharebesi (Düşman Ordusu)
            BirimYarat(piyadeVerisi, 13, 4, false);
            BirimYarat(piyadeVerisi, 13, 6, false);
            
            UnitData dusmanOkcu = null;
            foreach(var v in tumBirimlerVeritabani) { if(v != null && v.birimAdi == "Okçu") dusmanOkcu = v; }
            
            if (dusmanOkcu != null) BirimYarat(dusmanOkcu, 14, 5, false);
            else BirimYarat(piyadeVerisi, 14, 5, false); // Failsafe
        }
    }

    void BirimYarat(UnitData veri, int x, int y, bool bizdenMi)
    {
        // Birimi haritadaki X,Y koordinatına tam oturt
        Vector2 pozisyon = new Vector2(x * tileBoyutu, y * tileBoyutu);
        
        // YENİ: Eğer UnitData içine özel bir prefab atanmışsa onu kullan, yoksa eski jenerik prefab'a düş
        GameObject basilacakPrefab = (veri != null && veri.birimPrefab != null) ? veri.birimPrefab : unitPrefab;
        GameObject yeniAsker = Instantiate(basilacakPrefab, pozisyon, Quaternion.identity);
        
        BattleUnit birimKodu = yeniAsker.GetComponent<BattleUnit>();
        birimKodu.Setup(veri, x, y, bizdenMi);

        if (bizdenMi) oyuncuBirimleri.Add(birimKodu);
        else dusmanBirimleri.Add(birimKodu);
    }

    // İŞTE MEŞHUR D&D ZAR VE HASAR SİSTEMİ
    public void SaldiriGerceklestir(BattleUnit saldiran, BattleUnit savunan)
    {
        // YENİ KURAL: Koçbaşı birimlere dalamaz
        if (!saldiran.veri.askerlereHasarVurabilirMi && !savunan.veri.isBina)
        {
            Debug.Log($"HATA: {saldiran.veri.birimAdi} sadece binalara saldırabilir!");
            return; // Hasar vurmadan çık
        }

        // YENİ: ZEMİN VE SİPER AVANTAJLARI
        BattleTile saldiranTile = grid[saldiran.gridX, saldiran.gridY];
        BattleTile savunanTile = grid[savunan.gridX, savunan.gridY];

        int aktifIsabet = saldiran.veri.isabetDegeri;
        int aktifZirh = savunan.veri.zirhDegeri;

        // Okçu Tepe'den atış yapıyorsa +2 İsabet
        if (saldiranTile.zeminTuru == BattleTile.ZeminTipi.Tepe && saldiran.veri.saldiriMenzili > 1)
        {
            aktifIsabet += 2;
            Debug.Log($"[TEPE AVANTAJI] {saldiran.veri.birimAdi} yüksekten ateş ettiği için +2 İsabet kazandı!");
        }

        // Savunan Orman'ın içindeyse (siper aldıysa) +2 Zırh
        if (savunanTile.zeminTuru == BattleTile.ZeminTipi.Orman)
        {
            aktifZirh += 2;
            Debug.Log($"[SİPER AVANTAJI] {savunan.veri.birimAdi} ormanda saklandığı için +2 Zırh kazandı!");
        }

        // 1 ile 20 arası rastgele zar at (21 dahil değil)
        int d20 = Random.Range(1, 21); 
        int toplamSaldiriGucu = d20 + aktifIsabet;

        Debug.Log($"--- SAVAŞ: {saldiran.veri.birimAdi} -> {savunan.veri.birimAdi} hedefine saldırıyor! ---");
        Debug.Log($"Zar (1d20): {d20} + İsabet: {aktifIsabet} = TOPLAM: {toplamSaldiriGucu}");

        // Toplam güç, savunanın zırhından (AC) büyük mü? VEYA natürel 20 mi geldi (Kritik vuruş)?
        if (toplamSaldiriGucu > aktifZirh || d20 == 20)
        {
            int verilecekHasar = saldiran.veri.hasar;

            // YENİ KURAL: Hedef bina ise Koçbaşı x2 ve ekstra hasar vurur
            if (savunan.veri.isBina && saldiran.veri.binaHasarCari)
            {
                verilecekHasar += saldiran.veri.binayaEkstraHasar;
                verilecekHasar *= 2; 
                Debug.Log($"[KUŞATMA!] {saldiran.veri.birimAdi} binaya ekstra hasar vurdu! Toplam Hasar: {verilecekHasar}");
            }

            // Vuruş başarılı olduysa hedef hasar alır
            savunan.mevcutCan -= verilecekHasar;
            savunan.CaniGuncelle();
            
            // YENİ: Birlik öldüyse onu OlumKontrolu fonksiyonuna devret
            savunan.OlumKontrolu();
            SavasDurumunuKontrolEt();
        }
        else
        {
            Debug.Log($"🛡️ ISKA / ZIRHTAN SEKTİ! Toplam ({toplamSaldiriGucu}), savunanın zırhını ({savunan.veri.zirhDegeri}) geçemedi.");
        }
    }

    public void AtesliOkGerceklestir(BattleUnit saldiran, BattleUnit savunan)
    {
        Debug.Log("🔥 ATEŞLİ OK KULLANILDI!");
        int eskiHasar = saldiran.veri.hasar;
        saldiran.veri.hasar += 3; // +3 Bonus
        
        SaldiriGerceklestir(saldiran, savunan);
        
        // ZEMİNİ KONTROL ET VE YAK
        BattleTile savunanTile = grid[savunan.gridX, savunan.gridY];
        OrmaniAteseVer(savunanTile);
        
        saldiran.veri.hasar = eskiHasar; // Hasarı geri düzelt
        saldiran.yetenekCooldown = 2; // 2 Tur bekleme
        SecimiTemizle();
    }

    public void OrmaniAteseVer(BattleTile tile)
    {
        if (tile != null && tile.zeminTuru == BattleTile.ZeminTipi.Orman && yananOrmanPrefab != null)
        {
            Vector3 pozisyon = tile.transform.position;
            int hX = tile.x;
            int hY = tile.y;
            Destroy(tile.gameObject);
            
            GameObject yeniTileObj = Instantiate(yananOrmanPrefab, pozisyon, Quaternion.identity);
            yeniTileObj.transform.SetParent(this.transform);
            
            BattleTile yeniTile = yeniTileObj.GetComponent<BattleTile>();
            yeniTile.Setup(hX, hY);
            yeniTile.zeminTuru = BattleTile.ZeminTipi.YananOrman;
            
            grid[hX, hY] = yeniTile;
            Debug.Log("🌲🔥 ORMAN ATEŞE VERİLDİ!");
        }
    }

    public void Buton_AtesliOk()
    {
        if (seciliBirim != null && seciliBirim.yetenekCooldown <= 0)
        {
            atesliOkAktifMi = true;
            yetenekButonu.SetActive(false);
            Debug.Log("Okçu ateşli ok modunda! Bir düşman seç.");
        }
    }

    public void BirimOldu(BattleUnit olenBirim)
    {
        if (olenBirim.oyuncununBirimiMi) oyuncuBirimleri.Remove(olenBirim);
        else dusmanBirimleri.Remove(olenBirim);

        Destroy(olenBirim.gameObject); // Ekrandan sil
        SavasDurumunuKontrolEt();
    }

    void Update()
    {
        if (savasBittiMi) return;
        if (!oyuncuTuru) return;

        // YENİ: Eğer haritadaki HERHANGİ bir bizim askerimiz yürüyorsa yeni komut verme!
        foreach (var asker in oyuncuBirimleri) { if (asker.HareketEdiyorMu()) return; }

        // SAĞ TIK VEYA ESC: Seçimi İptal Et
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            SecimiTemizle();
        }

        // SOL TIK: Birim Seç, Düşmana Saldır veya Yürü
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                BattleUnit tiklananBirim = hit.collider.GetComponent<BattleUnit>();
                BattleTile tiklananTile = hit.collider.GetComponent<BattleTile>();

                // DURUM 1 & 2: BİR BİRİME (ASKERE) TIKLADIK
                if (tiklananBirim != null)
                {
                    if (tiklananBirim.oyuncununBirimiMi)
                    {
                        SecimiTemizle();
                        seciliBirim = tiklananBirim;
                        seciliBirim.GetComponent<SpriteRenderer>().color = Color.yellow;
                        MenzilleriGoster(seciliBirim);
                        Debug.Log($"{seciliBirim.veri.birimAdi} komut bekliyor...");
                    }
                    else if (!tiklananBirim.oyuncununBirimiMi && seciliBirim != null)
                    {
                        if (!seciliBirim.saldirdiMi)
                        {
                            // Çapraz da dahil olmak üzere saldırı menzilini hesapla
                            int mesafeX = Mathf.Abs(seciliBirim.gridX - tiklananBirim.gridX);
                            int mesafeY = Mathf.Abs(seciliBirim.gridY - tiklananBirim.gridY);
                            int uzaklik = Mathf.Max(mesafeX, mesafeY);

                            if (uzaklik <= seciliBirim.veri.saldiriMenzili)
                            {
                                // EĞER ATEŞLİ OK AKTİFSE
                                if (atesliOkAktifMi && seciliBirim.veri.birimAdi == "Okçu")
                                {
                                    AtesliOkGerceklestir(seciliBirim, tiklananBirim);
                                }
                                else
                                {
                                    SaldiriGerceklestir(seciliBirim, tiklananBirim);
                                }
                                
                                seciliBirim.saldirdiMi = true;
                                SecimiTemizle();
                            }
                            else Debug.Log("HATA: Düşman menzil dışında!");
                        }
                        else Debug.Log("HATA: Bu birim bu tur zaten saldırdı!");
                    }
                }
                // DURUM 3: BOŞ BİR KAREYE TIKLADIK -> YÜRÜME VEYA ATEŞLİ OK MANTIĞI
                else if (tiklananTile != null && seciliBirim != null)
                {
                    // YENİ: Ateşli ok ile boş bir ormanı vurmak
                    if (atesliOkAktifMi && seciliBirim.veri.birimAdi == "Okçu" && !seciliBirim.saldirdiMi)
                    {
                        int mesafeX = Mathf.Abs(seciliBirim.gridX - tiklananTile.x);
                        int mesafeY = Mathf.Abs(seciliBirim.gridY - tiklananTile.y);
                        
                        if (Mathf.Max(mesafeX, mesafeY) <= seciliBirim.veri.saldiriMenzili)
                        {
                            if (tiklananTile.zeminTuru == BattleTile.ZeminTipi.Orman)
                            {
                                Debug.Log("🔥 ATEŞLİ OK KULLANILDI!");
                                OrmaniAteseVer(tiklananTile);
                                seciliBirim.yetenekCooldown = 2;
                                seciliBirim.saldirdiMi = true;
                                SecimiTemizle();
                            }
                            else Debug.Log("HATA: Sadece ormanları ateşe verebilirsin!");
                        }
                        else Debug.Log("HATA: Hedef menzil dışında!");
                        
                        return; // Yürüme mantığına girmeden çık
                    }

                    if (!tiklananTile.YurunebilirMi)
                    {
                        Debug.Log("HATA: Burası bir orman veya dağ, üzerine yürüyemezsin!");
                        return; // Oraya tıklanmasına izin verme
                    }

                    bool kareDoluMu = false;
                    foreach (var asker in oyuncuBirimleri) { if (asker.gridX == tiklananTile.x && asker.gridY == tiklananTile.y) kareDoluMu = true; }
                    foreach (var asker in dusmanBirimleri) { if (asker.gridX == tiklananTile.x && asker.gridY == tiklananTile.y) kareDoluMu = true; }

                    if (!kareDoluMu)
                    {
                        if (!seciliBirim.saldirdiMi)
                        {
                            if (!seciliBirim.yuruduMu)
                            {
                                // BFS YOL BULMA İLE GİDİLEBİLİR ROTAYI ÇIKAR
                                var adimlar = RotaBulBFS(new Vector2Int(seciliBirim.gridX, seciliBirim.gridY), new Vector2Int(tiklananTile.x, tiklananTile.y));

                                if (adimlar != null && adimlar.Count <= seciliBirim.veri.hareketMenzili)
                                {
                                    seciliBirim.gridX = tiklananTile.x;
                                    seciliBirim.gridY = tiklananTile.y;
                                    
                                    // Makro haritadaki yürüme sistemi gibi Listeyi askere ver
                                    seciliBirim.RotayiBaslat(adimlar, tileBoyutu);
                                    
                                    seciliBirim.yuruduMu = true;
                                    MenzilleriTemizle();
                                    Debug.Log($"{seciliBirim.veri.birimAdi} başarıyla yola çıktı.");
                                }
                                else Debug.Log("HATA: Hedefe giden engel-siz bir yol bulunamadı veya menzil yetersiz!");
                            }
                            else Debug.Log("HATA: Bu asker bu tur zaten yürüdü!");
                        }
                        else Debug.Log("HATA: Saldırı yaptıktan sonra yürüyemezsin!");
                    }
                    else Debug.Log("HATA: Hedef kare dolu!");
                }
            }
        }
    }

    void SecimiTemizle()
    {
        if (seciliBirim != null)
        {
            seciliBirim.GetComponent<SpriteRenderer>().color = Color.white; // Rengi normale çevir
            seciliBirim = null;
        }
        
        atesliOkAktifMi = false;
        if (yetenekButonu != null) yetenekButonu.SetActive(false);
        
        MenzilleriTemizle();
    }

    // --- TUR VE YAPAY ZEKA SİSTEMİ ---

    public void TuruBitir()
    {
        if (savasBittiMi) return;

        if (!oyuncuTuru) return; // Zaten düşmanın turundaysak butona basmayı engelle

        oyuncuTuru = false;
        SecimiTemizle();
        Debug.Log("--- SIRA DÜŞMANDA ---");
        
        StartCoroutine(DusmanTuruAnimasyonu());
    }

    void MenzilleriGoster(BattleUnit birim)
    {
        // Yetenek Arayüzünü Kontrol Et
        if (birim.veri.birimAdi == "Okçu" && yetenekButonu != null)
        {
            yetenekButonu.SetActive(true);
            if (birim.yetenekCooldown > 0)
            {
                yetenekButonuYazisi.text = $"Bekle: {birim.yetenekCooldown} Tur";
            }
            else
            {
                yetenekButonuYazisi.text = "Ateşli Ok (Hazır)";
            }
        }
        
        MenzilleriTemizle(); // Önce eski boyaları bir silelim

        // YENİ: Engellerin pürüzünü hesaba katan su baskını algoritması (Mavi renk)
        HashSet<Vector2Int> ulasilabilirler = UlasilabilirKareleriBul(birim);

        for (int x = 0; x < genislik; x++)
        {
            for (int y = 0; y < yukseklik; y++)
            {
                BattleTile tile = grid[x, y];
                Vector2Int pos = new Vector2Int(x, y);
                
                // Saldırı mesafesi okçular için engel tanımaz
                int saldiriMesafesi = Mathf.Max(Mathf.Abs(birim.gridX - tile.x), Mathf.Abs(birim.gridY - tile.y));

                bool kirmiziMi = false;

                // 1. KURAL: Eğer saldırmadıysa, saldırı menzilini Kırmızı yap
                if (!birim.saldirdiMi && saldiriMesafesi <= birim.veri.saldiriMenzili)
                {
                    tile.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 0f, 0.4f);
                    kirmiziMi = true;
                }

                // 2. KURAL: Eğer yürümediyse ve ulaşılabiliyorsa Maviye (ya da mor) boya
                if (!birim.yuruduMu && ulasilabilirler.Contains(pos))
                {
                    if (birim.gridX != tile.x || birim.gridY != tile.y)
                    {
                        if (kirmiziMi) tile.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0f, 0.8f, 0.5f); // Kırmızıyla çakışırsa mora yakın
                        else tile.GetComponent<SpriteRenderer>().color = new Color(0f, 0.5f, 1f, 0.4f); // Sadece yolları temiz Mavi
                    }
                }
            }
        }
    }

    void MenzilleriTemizle()
    {
        // Tüm haritayı gez ve renkleri asıl (orijinal) rengine döndür
        for (int x = 0; x < genislik; x++)
        {
            for (int y = 0; y < yukseklik; y++)
            {
                // Eski hatalı kod: grid[x, y].GetComponent<SpriteRenderer>().color = Color.white;
                
                // YENİ DOĞRU KOD:
                grid[x, y].RengiSifirla();
            }
        }
    }

    IEnumerator DusmanTuruAnimasyonu()
    {
        // Düşmanların yürüme/saldırma haklarını yenile, Cooldown düşür, Yanan Orman hasarı ver
        for (int i = dusmanBirimleri.Count - 1; i >= 0; i--) 
        { 
            BattleUnit dusman = dusmanBirimleri[i];
            dusman.saldirdiMi = false; 
            dusman.yuruduMu = false; 
            
            if (dusman.yetenekCooldown > 0) dusman.yetenekCooldown--;
            
            if (grid[dusman.gridX, dusman.gridY].zeminTuru == BattleTile.ZeminTipi.YananOrman)
            {
                Debug.Log($"[YANAN ORMAN] Düşman {dusman.veri.birimAdi} alevlerden 1 hasar aldı!");
                dusman.mevcutCan -= 1;
                dusman.CaniGuncelle();
                dusman.OlumKontrolu();
            }
        }
        
        SavasDurumunuKontrolEt();
        if (savasBittiMi) yield break;

        yield return new WaitForSeconds(1f); // Düşman düşünüyormuş gibi kısa bir bekleme

        // Her bir düşman için sırayla hamle yap
        foreach (var dusman in dusmanBirimleri)
        {
            if (oyuncuBirimleri.Count == 0) break; // Eğer askerimiz kalmadıysa döngüyü bitir

            // En yakın hedefimizi bulalım (Şimdilik ilk bulduğu askere odaklansın)
            BattleUnit hedef = oyuncuBirimleri[0]; 

            // 1. KURAL: Menzildeysek direkt saldır!
            int mesafe = Mathf.Max(Mathf.Abs(dusman.gridX - hedef.gridX), Mathf.Abs(dusman.gridY - hedef.gridY));
            
            if (mesafe <= dusman.veri.saldiriMenzili)
            {
                SaldiriGerceklestir(dusman, hedef);
                dusman.saldirdiMi = true;
            }
            else
            {
                // 2. KURAL: Hedefe Engel-Kaya Tanımadan A* Rotası ile Yürü
                var rota = RotaBulBFS(new Vector2Int(dusman.gridX, dusman.gridY), new Vector2Int(hedef.gridX, hedef.gridY), true);

                if (rota != null && rota.Count > 0)
                {
                    rota.RemoveAt(rota.Count - 1); // Hedefin kucağına çıkmamak (askeri ezmemek) için son adımı sil

                    List<Vector2Int> atilacakAdimlar = new List<Vector2Int>();
                    for (int i = 0; i < rota.Count; i++)
                    {
                        if (i >= dusman.veri.hareketMenzili) break; // Askerin nefesi ancak bu kadarına yeterse dur
                        atilacakAdimlar.Add(rota[i]);
                    }

                    if (atilacakAdimlar.Count > 0)
                    {
                        Vector2Int varis = atilacakAdimlar[atilacakAdimlar.Count - 1];
                        dusman.gridX = varis.x;
                        dusman.gridY = varis.y;
                        
                        dusman.RotayiBaslat(atilacakAdimlar, tileBoyutu);
                        dusman.yuruduMu = true;
                        Debug.Log($"{dusman.veri.birimAdi} etrafından dolanarak hedefe yaklaşıyor.");

                        // YAPAY ZEKA: Animasyson bitene kadar bir sonraki koda geçme!
                        yield return new WaitUntil(() => !dusman.HareketEdiyorMu());
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                // Yürüdükten sonra tekrar menzil kontrolü yap, girdiyse vur!
                mesafe = Mathf.Max(Mathf.Abs(dusman.gridX - hedef.gridX), Mathf.Abs(dusman.gridY - hedef.gridY));
                if (mesafe <= dusman.veri.saldiriMenzili && !dusman.saldirdiMi)
                {
                    SaldiriGerceklestir(dusman, hedef);
                    dusman.saldirdiMi = true;
                }
            }
            
            yield return new WaitForSeconds(0.8f); // Diğer düşmanın hamlesine geçmeden önce bekle
        }

        // Tüm düşmanlar hamlesini yaptı, sırayı oyuncuya geri ver
        Debug.Log("--- SIRA OYUNCUDA ---");
        oyuncuTuru = true;

        // Askerlerimizin yürüme/saldırma haklarını yenile, Cooldown düşür, Yanan Orman hasarı ver
        for (int i = oyuncuBirimleri.Count - 1; i >= 0; i--) 
        { 
            BattleUnit asker = oyuncuBirimleri[i];
            asker.saldirdiMi = false; 
            asker.yuruduMu = false; 
            
            if (asker.yetenekCooldown > 0) asker.yetenekCooldown--;
            
            if (grid[asker.gridX, asker.gridY].zeminTuru == BattleTile.ZeminTipi.YananOrman)
            {
                Debug.Log($"[YANAN ORMAN] Askerin {asker.veri.birimAdi} alevlerden 1 hasar aldı!");
                asker.mevcutCan -= 1;
                asker.CaniGuncelle();
                asker.OlumKontrolu();
            }
        }
        
        SavasDurumunuKontrolEt();
    }

    public void SavasDurumunuKontrolEt()
    {
        if (oyuncuBirimleri.Count == 0)
        {
            savasBittiMi = true;
            savasSonuPaneli.SetActive(true); // Paneli görünür yap
            savasSonuYazisi.text = "MAĞLUBİYET!\nOrdun Yok Edildi.";
            savasSonuYazisi.color = Color.red;
            if (SavasHafizasi.Instance != null) SavasHafizasi.Instance.savastanZaferleMiDondu = false;
        }
        else if (dusmanBirimleri.Count == 0)
        {
            savasBittiMi = true;
            savasSonuPaneli.SetActive(true); // Paneli görünür yap
            savasSonuYazisi.text = "ZAFER!\nKuşatma Başarılı.";
            savasSonuYazisi.color = Color.yellow;
            if (SavasHafizasi.Instance != null) SavasHafizasi.Instance.savastanZaferleMiDondu = true;
        }
    }

    public void HaritayaDon()
    {
        if (SavasHafizasi.Instance != null)
        {
            // YENİ KALICI HASAR: Makro haritaya dönmeden önce kuryenin çantasına sadece "Hayatta Kalanları" doldur
            SavasHafizasi.Instance.hayattaKalanBirlikler.Clear();
            foreach (BattleUnit asker in oyuncuBirimleri)
            {
                SavasHafizasi.Instance.hayattaKalanBirlikler.Add(asker.veri.birimAdi);
            }

            // Savaş hafızasına "Hadi bizi geri döndür ve kazanmışsak kaleyi yık" diyoruz.
            SavasHafizasi.Instance.MakroHaritayaDon();
        }
    }

    // --- BREADTH FIRST SEARCH (BFS) YOL BULMA ALGORİTMASI MİMARİSİ ---
    
    HashSet<Vector2Int> UlasilabilirKareleriBul(BattleUnit birim)
    {
        HashSet<Vector2Int> ulasilabilirler = new HashSet<Vector2Int>();
        Queue<Vector2Int> kuyruk = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> mesafeler = new Dictionary<Vector2Int, int>();

        Vector2Int baslangic = new Vector2Int(birim.gridX, birim.gridY);
        kuyruk.Enqueue(baslangic);
        mesafeler[baslangic] = 0;

        while(kuyruk.Count > 0)
        {
            Vector2Int mevcut = kuyruk.Dequeue();
            int mevcutMesafe = mesafeler[mevcut];

            ulasilabilirler.Add(mevcut);

            if(mevcutMesafe >= birim.veri.hareketMenzili) continue;

            Vector2Int[] komsular = {
                new Vector2Int(mevcut.x, mevcut.y + 1),
                new Vector2Int(mevcut.x, mevcut.y - 1),
                new Vector2Int(mevcut.x + 1, mevcut.y),
                new Vector2Int(mevcut.x - 1, mevcut.y)
            };

            foreach(var komsu in komsular)
            {
                if(komsu.x < 0 || komsu.x >= genislik || komsu.y < 0 || komsu.y >= yukseklik) continue;
                
                if (!grid[komsu.x, komsu.y].YurunebilirMi) continue;
                
                bool birimVar = false;
                foreach(var asker in oyuncuBirimleri) { if(asker.gridX == komsu.x && asker.gridY == komsu.y) birimVar = true; }
                foreach(var asker in dusmanBirimleri) { if(asker.gridX == komsu.x && asker.gridY == komsu.y) birimVar = true; }
                if(birimVar) continue; // Birimlerin içinden geçmeyi yasakladık

                if(!mesafeler.ContainsKey(komsu))
                {
                    mesafeler[komsu] = mevcutMesafe + 1;
                    kuyruk.Enqueue(komsu);
                }
            }
        }
        return ulasilabilirler;
    }

    List<Vector2Int> RotaBulBFS(Vector2Int baslangic, Vector2Int hedef, bool hedefeBirimVarIseGormezdenGel = false)
    {
        Queue<Vector2Int> kuyruk = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        
        kuyruk.Enqueue(baslangic);
        parentMap[baslangic] = new Vector2Int(-1, -1);

        bool bulundu = false;

        while(kuyruk.Count > 0)
        {
            Vector2Int mevcut = kuyruk.Dequeue();

            if (mevcut == hedef) { bulundu = true; break; }

            Vector2Int[] komsular = {
                new Vector2Int(mevcut.x, mevcut.y + 1),
                new Vector2Int(mevcut.x, mevcut.y - 1),
                new Vector2Int(mevcut.x + 1, mevcut.y),
                new Vector2Int(mevcut.x - 1, mevcut.y)
            };

            foreach(var komsu in komsular)
            {
                if(komsu.x < 0 || komsu.x >= genislik || komsu.y < 0 || komsu.y >= yukseklik) continue;
                if(parentMap.ContainsKey(komsu)) continue;
                if(!grid[komsu.x, komsu.y].YurunebilirMi) continue;

                bool birimVar = false;
                foreach(var asker in oyuncuBirimleri) { if(asker.gridX == komsu.x && asker.gridY == komsu.y) birimVar = true; }
                foreach(var asker in dusmanBirimleri) { if(asker.gridX == komsu.x && asker.gridY == komsu.y) birimVar = true; }
                
                if (birimVar)
                {
                    if (!(hedefeBirimVarIseGormezdenGel && komsu == hedef)) continue; 
                }

                parentMap[komsu] = mevcut;
                kuyruk.Enqueue(komsu);
            }
        }

        if (!bulundu) return null;

        List<Vector2Int> rota = new List<Vector2Int>();
        Vector2Int curr = hedef;
        while(curr != new Vector2Int(-1, -1))
        {
            if (curr != baslangic) rota.Add(curr);
            curr = parentMap[curr];
        }
        rota.Reverse();
        return rota;
    }
}