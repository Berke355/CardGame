using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour
{
    public Tilemap hexTilemap;
    public Transform secimImleci; 
    public GameObject piyadePrefab; 
    public GameObject menzilPrefab; 
    public GameObject kesifBirligiPrefab; // YENİ: Keşif Birliği için şablon
    public GameObject gocmenPrefab; // YENİ: Göçmen Birimi için özel şablon
    public GameObject kalePrefab; // YENİ: Kalemizi buraya bağlayacağız
    public GameObject dusmanKalesiPrefab; // YENİ: Düşman kalesi şablonu
    public GameObject dusmanKesifPrefab; // YENİ: Düşman Keşif Piyonu
    public GameObject koyPrefab; // YENİ: Oyuncunun Köy Prefabı
    public GameObject haydutKampiPrefab; // YENİ: Haritada rastgele çıkacak haydut kampı
    
    [Header("Prosedürel Harita (PCG)")]
    public bool haritaUret = true;
    public int haritaGenisligi = 40;
    public int haritaYuksekligi = 30;
    public float perlinOlcegi = 0.15f;
    public float suEsigi = 0.40f; 
    public TileBase cimenTile;
    public TileBase suTile;

    [Header("Kaynak Biyomları (Yeni Ekonomi)")]
    public TileBase altinTile; // Altın Madeni
    public TileBase tasTile;   // Taş Ocağı
    public TileBase ormanTile; // Orman (Yemek)

    [Header("Savaş Sisi ve Sınır (Territory) Sistemi")]
    public Tilemap fogTilemap; // Siyah sisi dizeceğimiz katman
    public TileBase fogTile; // Siyah altıgen objesi
    
    // YENİ HARİTA SİSTEMİ:
    // Bizim olan altıgenlerin koordinatlarını tutan liste
    public List<Vector3Int> bizimSinirlar = new List<Vector3Int>();
    public List<Vector3Int> dusmanSinirlar = new List<Vector3Int>(); // YENİ: Düşmanın bölgelerini tutar
    
    // YENİ: Oyuna çizdiğimiz Çizgi objelerini havzada tutan liste
    private List<GameObject> aktifSinirCizgileri = new List<GameObject>();
    private Color sinirRengi = new Color(0f, 0.8f, 1f, 1f); // Dış hudutların parlak Mavi rengi
    private Color dusmanSinirRengi = new Color(1f, 0f, 0f, 1f); // Düşman hudutlarının Kırmızı rengi

    private List<GameObject> aktifMenziller = new List<GameObject>(); 
    private GameObject secilenBirlik; 

    public void OyunuBaslat()
    {
        if(haritaUret) HaritaUret(); // YENİ: Başkentten önce haritayı çiz

        // TEST İÇİN GEÇİCİ OLARAK SİS KALDIRILDI
        // HaritayiSisleKapla();
        BaskentKur();
        DusmanYapayZekasiniKur(); // YENİ: Düşman AI kalesini kur
        HaydutKamplariniKoy(); // YENİ: Haydutları oyuncunun yanına düşmesin diye sona aldık
        // Cihazı yormadan her 0.2 saniyede bir sisleri ve sınırları kontrol et (Yürüdükçe açılsın diye)
        InvokeRepeating("SinirlariVeSisiGuncelle", 0.1f, 0.2f);
    }

    void HaritaUret()
    {
        if (cimenTile == null || suTile == null) 
        {
            Debug.LogError("HATA: Lütfen MapController içine Cimen ve Su tile'larını sürükleyin!");
            return;
        }

        hexTilemap.ClearAllTiles();
        if (fogTilemap != null) fogTilemap.ClearAllTiles();

        float offsetX = Random.Range(0f, 100000f);
        float offsetY = Random.Range(0f, 100000f);

        // 1. Perlin Noise ile Altıgenleri Boya
        for (int x = -haritaGenisligi / 2; x < haritaGenisligi / 2; x++)
        {
            for (int y = -haritaYuksekligi / 2; y < haritaYuksekligi / 2; y++)
            {
                // Daha doğal ve noktasal göller için iki farklı Perlin Noise katmanını birleştiriyoruz
                float anaGurultu = Mathf.PerlinNoise(x * perlinOlcegi * 1.5f + offsetX, y * perlinOlcegi * 1.5f + offsetY);
                float detayGurultu = Mathf.PerlinNoise(x * perlinOlcegi * 3f + offsetX, y * perlinOlcegi * 3f + offsetY);
                float noise = (anaGurultu * 0.8f) + (detayGurultu * 0.2f);
                
                // YENİ: Sadece haritanın EN uç kısımlarında (1-2 tile) denizi zorunlu kılacak sert falloff
                float nx = (float)Mathf.Abs(x) / (haritaGenisligi / 2f);
                float ny = (float)Mathf.Abs(y) / (haritaYuksekligi / 2f);
                float falloff = Mathf.Max(nx, ny);
                
                if (falloff > 0.85f)
                {
                    // Sadece %85'ten dışarıdaysa (en kenarlar) karayı çok hızlı bir şekilde çökert
                    noise -= (falloff - 0.85f) * 5f; 
                }

                // Göllerin çok devasa nehirler olmaması için su eşiğini biraz kıstık (Eğer inspector'da 0.40 ise koda göre 0.35 gibi davranır)
                float gercekSuEsigi = suEsigi * 0.85f; 

                Vector3Int pos = new Vector3Int(x, y, 0);

                if (noise < gercekSuEsigi)
                {
                    hexTilemap.SetTile(pos, suTile);
                }
                else
                {
                    // Çimen karasına geldik, şimdi rastgelelik katalım
                    float rastgeleKaynak = Random.value; // 0.0 ile 1.0 arası sayı üretir
                    
                    if (altinTile != null && rastgeleKaynak <= 0.02f) 
                    {
                        // %2 İhtimalle Altın Madeni
                        hexTilemap.SetTile(pos, altinTile);
                    }
                    else if (tasTile != null && rastgeleKaynak > 0.02f && rastgeleKaynak <= 0.06f) 
                    {
                        // %4 İhtimalle Taş Ocağı
                        hexTilemap.SetTile(pos, tasTile);
                    }
                    else if (ormanTile != null && rastgeleKaynak > 0.06f && rastgeleKaynak <= 0.14f) 
                    {
                        // %8 İhtimalle Orman (Yemek alanı)
                        hexTilemap.SetTile(pos, ormanTile);
                    }
                    else 
                    {
                        // Geriye kalan ihtimalle standart Çimen
                        hexTilemap.SetTile(pos, cimenTile);
                    }
                }
            }
        }

        // Fiziksel sınırları güncelle ki algoritmalar doğru tarasın
        hexTilemap.CompressBounds();

        // 2. İzole kalmış adaları temizleyip sadece en büyük ana karayı bırak
        IzoleAdalariSuyaCevir();
        
        Debug.Log("[SİSTEM] Prosedürel Harita Başarıyla Üretildi!");
    }

    void IzoleAdalariSuyaCevir()
    {
        BoundsInt bounds = hexTilemap.cellBounds;
        List<Vector3Int> tumKaralar = new List<Vector3Int>();

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (hexTilemap.HasTile(pos) && hexTilemap.GetTile(pos).name != "Su" && hexTilemap.GetTile(pos).name != "Deniz")
            {
                tumKaralar.Add(pos);
            }
        }

        if (tumKaralar.Count == 0) return;

        List<List<Vector3Int>> tumAdalar = new List<List<Vector3Int>>();
        HashSet<Vector3Int> ziyaretEdilenler = new HashSet<Vector3Int>();

        foreach (var kara in tumKaralar)
        {
            if (!ziyaretEdilenler.Contains(kara))
            {
                List<Vector3Int> yeniAda = new List<Vector3Int>();
                Queue<Vector3Int> bfsKuyruk = new Queue<Vector3Int>();

                bfsKuyruk.Enqueue(kara);
                ziyaretEdilenler.Add(kara);

                while (bfsKuyruk.Count > 0)
                {
                    Vector3Int mevcut = bfsKuyruk.Dequeue();
                    yeniAda.Add(mevcut);

                    foreach (var komsu in HexKomsulariBul(mevcut))
                    {
                        if (tumKaralar.Contains(komsu) && !ziyaretEdilenler.Contains(komsu))
                        {
                            ziyaretEdilenler.Add(komsu);
                            bfsKuyruk.Enqueue(komsu);
                        }
                    }
                }
                tumAdalar.Add(yeniAda);
            }
        }

        // Dünyanın en büyük anakarasını bul
        List<Vector3Int> enBuyukAnaKara = tumAdalar[0];
        foreach (var ada in tumAdalar)
        {
            if (ada.Count > enBuyukAnaKara.Count) enBuyukAnaKara = ada;
        }

        // Ana kara haricindeki tüm küçük adaları boğ (Suya dönüştür)
        foreach (var ada in tumAdalar)
        {
            if (ada != enBuyukAnaKara)
            {
                foreach (var kucukAdaPikseli in ada)
                {
                    hexTilemap.SetTile(kucukAdaPikseli, suTile);
                }
            }
        }
    }

    List<Vector3Int> HexKomsulariBul(Vector3Int merkez)
    {
        List<Vector3Int> komsular = new List<Vector3Int>();
        Vector3 merkezDunya = hexTilemap.GetCellCenterWorld(merkez);
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                Vector3Int aday = new Vector3Int(merkez.x + x, merkez.y + y, 0);
                
                // Yaklaşık 1.2f yarıçapı, bir altıgenin etrafındaki 6 komşuyu tam kapsamaya yeterlidir
                if (Vector2.Distance(merkezDunya, hexTilemap.GetCellCenterWorld(aday)) <= 1.2f)
                {
                    komsular.Add(aday);
                }
            }
        }
        return komsular;
    }

    void Update()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector3Int cellPosition = hexTilemap.WorldToCell(mouseWorldPos);

        // YENİ EKLENEN: KLAVYE İLE İPTAL (ESC TUŞU)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.secilenKart != null)
            {
                GameManager.Instance.secilenKart = null;
                Debug.Log("ESC: Kart seçimi iptal edildi.");
            }
            if (secilenBirlik != null)
            {
                secilenBirlik = null;
                MenziliTemizle();
            }
        }

        // SOL TIK: Birlik Seçimi ve Menzil Çizimi
        if (Input.GetMouseButtonDown(0))
        {
            if (hexTilemap.HasTile(cellPosition))
            {
                secimImleci.position = hexTilemap.GetCellCenterWorld(cellPosition);
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
                
                if (hit.collider != null && hit.collider.CompareTag("Unit"))
                {
                    secilenBirlik = hit.collider.gameObject;
                    MenziliCiz(secilenBirlik); 
                }
                else
                {
                    secilenBirlik = null;
                    MenziliTemizle(); 
                }
            }
        }

        // SAĞ TIK: Hareket ve Üretim
        if (Input.GetMouseButtonDown(1))
        {
            if (hexTilemap.HasTile(cellPosition))
            {
                TileBase tiklananTile = hexTilemap.GetTile(cellPosition);

                // EĞER KARAYA TIKLANDIYSA (ESKİ KODLARIN)
                if (tiklananTile.name != "Su") 
                {
                    Vector3 hedefPozisyon = hexTilemap.GetCellCenterWorld(cellPosition);

                    Collider2D[] buradakiler = Physics2D.OverlapCircleAll(hedefPozisyon, 0.1f);
                    
                    // --- YENİ EKLENEN: HEDEF TESPİTİ (Kale, Köy, Haydut vb) ---
                    GameObject tiklananKale = null;
                    GameObject tiklananKoy = null;
                    GameObject tiklananHaydut = null;
                    GameObject tiklananBirlik = null; // YENİ

                    foreach (Collider2D obje in buradakiler)
                    {
                        if (obje.CompareTag("Kale") || obje.GetComponent<MakroKale>() != null) { tiklananKale = obje.gameObject; }
                        else if (obje.CompareTag("Koy") || obje.GetComponent<MakroKoy>() != null) { tiklananKoy = obje.gameObject; }
                        else if (obje.CompareTag("Haydut") || obje.GetComponent<HaydutKampi>() != null) { tiklananHaydut = obje.gameObject; }
                        else if (obje.CompareTag("Unit")) { tiklananBirlik = obje.gameObject; }
                    }

                    // --- YENİ EKLENEN: İNCELE (INSPECT) MANTIĞI ---
                    if (GameManager.Instance.secilenKart == null)
                    {
                        if (tiklananBirlik != null || tiklananKale != null)
                        {
                            GameObject hedef = tiklananBirlik != null ? tiklananBirlik : tiklananKale;
                            ArmyStats hedefOrdu = hedef.GetComponent<ArmyStats>();
                            
                            // Bu kale/birlik düşman mı?
                            bool isEnemy = (hedefOrdu != null && hedefOrdu.dusmanMi) || (hedef.GetComponent<MakroKale>() != null && !bizimSinirlar.Contains(cellPosition));
                            
                            // Eğer DOST bir birliğe/kaleye tıkladıysak VEYA (düşmansa ve bizde ordu seçili değilse) => İNCELE!
                            if (!isEnemy || (isEnemy && secilenBirlik == null))
                            {
                                if (GameManager.Instance != null) GameManager.Instance.IncelePaneliniAc(hedef);
                                return; // Yürütme/saldırı kodlarını atla
                            }
                        }
                        else
                        {
                            // Boş bir yere sağ tıkladıysak inceleme panelini kapat
                            if (GameManager.Instance != null) GameManager.Instance.IncelePaneliniKapat();
                        }
                    }

                    // 1. DURUM: EĞER BİR BİRLİK SEÇİLİYSE VE BİR HEDEFE TIKLANDIYSA (Kart Oynanmıyorsa)
                    if (secilenBirlik != null && GameManager.Instance.secilenKart == null)
                    {
                        ArmyStats orduStat = secilenBirlik.GetComponent<ArmyStats>();
                        bool isKesif = (orduStat != null && orduStat.icindekiBirlikler.Contains("Kesif"));
                        bool isGocmen = (orduStat != null && orduStat.icindekiBirlikler.Contains("Gocmen"));

                        // GÖÇMEN KORUMASI: Göçmenler savaşamaz!
                        if (isGocmen && (tiklananHaydut != null || tiklananKoy != null || tiklananKale != null))
                        {
                            Debug.Log("HATA: Göçmenler savaşamaz veya saldıramaz!");
                            return; // Hedefe yürümeyi veya saldırmayı iptal et
                        }

                        if (isKesif)
                        {
                            if (tiklananHaydut != null)
                            {
                                float mesafe = Vector2.Distance(secilenBirlik.transform.position, tiklananHaydut.transform.position);
                                if (mesafe <= 1.5f)
                                {
                                    HaydutKampi kamp = tiklananHaydut.GetComponent<HaydutKampi>();
                                    int zirh = kamp != null ? kamp.zirhDegeri : 12;
                                    if (MakroSavasManager.Instance != null) MakroSavasManager.Instance.SavasiBaslat(secilenBirlik, tiklananHaydut, zirh);
                                    return;
                                }
                                else
                                {
                                    Debug.Log("HATA: Haydut Kampına saldırmak için Keşif Birliği en az 1 tile (hemen yanında) yakın olmalıdır!");
                                    return;
                                }
                            }
                            else if (tiklananKoy != null)
                            {
                                float mesafe = Vector2.Distance(secilenBirlik.transform.position, tiklananKoy.transform.position);
                                if (mesafe <= 1.5f)
                                {
                                    MakroKoy koyScript = tiklananKoy.GetComponent<MakroKoy>();
                                    int zirh = koyScript != null ? koyScript.zirhDegeri : 10;
                                    
                                    Vector3Int koyGrid = hexTilemap.WorldToCell(tiklananKoy.transform.position);
                                    if (!bizimSinirlar.Contains(koyGrid)) // Bizim değilse düşmanındır
                                    {
                                        if (MakroSavasManager.Instance != null) MakroSavasManager.Instance.SavasiBaslat(secilenBirlik, tiklananKoy, zirh);
                                        return;
                                    }
                                    else
                                    {
                                        Debug.Log("HATA: Kendi köyünüze saldıramazsınız!");
                                        return;
                                    }
                                }
                                else
                                {
                                    Debug.Log("HATA: Düşman Köyüne saldırmak için Keşif Birliği en az 1 tile (hemen yanında) yakın olmalıdır!");
                                    return;
                                }
                            }
                            else if (tiklananBirlik != null)
                            {
                                ArmyStats hedefStats = tiklananBirlik.GetComponent<ArmyStats>();
                                if (hedefStats != null && hedefStats.dusmanMi)
                                {
                                    if (hedefStats.icindekiBirlikler.Contains("Kesif") || hedefStats.icindekiBirlikler.Contains("Gocmen"))
                                    {
                                        float mesafe = Vector2.Distance(secilenBirlik.transform.position, tiklananBirlik.transform.position);
                                        if (mesafe <= 1.5f)
                                        {
                                            if (MakroSavasManager.Instance != null) MakroSavasManager.Instance.SavasiBaslat(secilenBirlik, tiklananBirlik, 10);
                                            return;
                                        }
                                        else
                                        {
                                            Debug.Log("HATA: Düşman Keşif birliğine saldırmak için yanına kadar yürümelisin!");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        Debug.Log("HATA: Keşif birlikleri normal ordulara saldıramaz!");
                                        return;
                                    }
                                }
                            }
                        }
                        
                        // 1.2 NORMAL ORDU İSE (Mikro Savaş Başlat)
                        if (!isKesif)
                        {
                            if (tiklananKale != null)
                            {
                                // Birliğin, kalenin 'yan tile'ında olup olmadığını (mesafeyi) kontrol ediyoruz
                                float mesafe = Vector2.Distance(secilenBirlik.transform.position, tiklananKale.transform.position);
                                
                                // 1.5f mesafe, bitişik hex (altıgen) sınırları için idealdir
                                if (mesafe <= 1.5f) 
                                {
                                    Debug.Log("⚔️ Yakın temas! Kaleye saldırı emri verildi!");
                                    if (SavasHafizasi.Instance != null)
                                    {
                                        SavasHafizasi.Instance.savasaGirecekOrdu.Clear();
                                        
                                        ArmyStats seciliOrduStat = secilenBirlik.GetComponent<ArmyStats>();
                                        if (seciliOrduStat != null)
                                        {
                                            SavasHafizasi.Instance.savasaGirecekOrdu.AddRange(seciliOrduStat.icindekiBirlikler);
                                            SavasHafizasi.Instance.savasanBizimOrdu = secilenBirlik;
                                        }
                                        
                                        Vector3Int kaleGride = hexTilemap.WorldToCell(tiklananKale.transform.position);
                                        string biyomAdi = "";
                                        if (hexTilemap.HasTile(kaleGride)) biyomAdi = hexTilemap.GetTile(kaleGride).name;

                                        SavasHafizasi.Instance.SavasiBaslat(tiklananKale, biyomAdi);
                                    }
                                }
                                else
                                {
                                    Debug.Log("HATA: Kaleye sağ tıklandı fakat seçili ordu uzakta! Önce kalenin yanına kadar yürümelisin.");
                                }
                                return; 
                            }
                            else if (tiklananBirlik != null)
                            {
                                ArmyStats hedefStats = tiklananBirlik.GetComponent<ArmyStats>();
                                if (hedefStats != null && hedefStats.dusmanMi)
                                {
                                    if (!hedefStats.icindekiBirlikler.Contains("Kesif") && !hedefStats.icindekiBirlikler.Contains("Gocmen"))
                                    {
                                        float mesafe = Vector2.Distance(secilenBirlik.transform.position, tiklananBirlik.transform.position);
                                        if (mesafe <= 1.5f) 
                                        {
                                            Debug.Log("⚔️ Yakın temas! Düşman ordusuna saldırı emri verildi!");
                                            if (SavasHafizasi.Instance != null)
                                            {
                                                SavasHafizasi.Instance.savasaGirecekOrdu.Clear();
                                                
                                                ArmyStats seciliOrduStat = secilenBirlik.GetComponent<ArmyStats>();
                                                if (seciliOrduStat != null)
                                                {
                                                    SavasHafizasi.Instance.savasaGirecekOrdu.AddRange(seciliOrduStat.icindekiBirlikler);
                                                    SavasHafizasi.Instance.savasanBizimOrdu = secilenBirlik;
                                                }
                                                
                                                Vector3Int gridPos = hexTilemap.WorldToCell(tiklananBirlik.transform.position);
                                                string biyomAdi = "";
                                                if (hexTilemap.HasTile(gridPos)) biyomAdi = hexTilemap.GetTile(gridPos).name;

                                                SavasHafizasi.Instance.SavasiBaslat(tiklananBirlik, biyomAdi);
                                            }
                                        }
                                        else
                                        {
                                            Debug.Log("HATA: Düşman ordusuna saldırmak için yanına kadar yürümelisin.");
                                        }
                                        return; 
                                    }
                                    else
                                    {
                                        Debug.Log("HATA: Ordular, keşif birlikleriyle etkileşime giremez!");
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    // 2. DURUM: EĞER HİÇBİR SAVAŞ ŞARTI SAĞLANMADIYSA VE TIKLANAN YER DOLUYSA
                    if (buradakiler.Length > 0)
                    {
                        bool isBuildingOverSettler = false;
                        if (GameManager.Instance.secilenKart != null && GameManager.Instance.secilenKart.binaKartiMi)
                        {
                            foreach (Collider2D obje in buradakiler)
                            {
                                if (obje.CompareTag("Unit"))
                                {
                                    ArmyStats stats = obje.GetComponent<ArmyStats>();
                                    if (stats != null && stats.icindekiBirlikler.Contains("Gocmen"))
                                    {
                                        isBuildingOverSettler = true;
                                        break;
                                    }
                                }
                            }
                        }

                        bool isTargetedBuffCard = GameManager.Instance.secilenKart != null && 
                            !GameManager.Instance.secilenKart.orduKartiMi && 
                            !GameManager.Instance.secilenKart.binaKartiMi;

                        if (!isBuildingOverSettler && !isTargetedBuffCard)
                        {
                            Debug.Log("HATA: Bu altıgen dolu veya saldırmak için uygun birlik/hedef seçilmedi!");
                            return; // Oraya yürüme veya kart üretimi yapmasını engelliyoruz
                        }
                    }

                    // 3. DURUM: EĞER BİR BİRLİK SEÇİLİYSE VE BOŞ BİR YERE YÜRÜMEK İSTİYORSA
                    if (secilenBirlik != null && GameManager.Instance.secilenKart == null)
                    {
                        ArmyStats stats = secilenBirlik.GetComponent<ArmyStats>();
                        
                        // YENİ: Civilization Kuralı Kontrolü
                        if (stats.buTurHareketEttiMi)
                        {
                            Debug.Log("HATA: Bu birlik bu tur zaten hareket etti! Bir sonraki turu beklemelisin.");
                            secilenBirlik = null;
                            MenziliTemizle();
                            return;
                        }

                        Vector3Int baslangicHex = hexTilemap.WorldToCell(secilenBirlik.transform.position);
                        
                        // Kuş uçuşu yerine sadece karadan gidebildiği kadar BFS sorgusu yap
                        Dictionary<Vector3Int, List<Vector3Int>> ulasilabilirler = BFSYolBul(baslangicHex, stats.hareketMenzili);

                        if (ulasilabilirler.ContainsKey(cellPosition)) 
                        {
                            if (GameManager.Instance.intikalPuani >= 1)
                            {
                                List<Vector3> rotamDunya = new List<Vector3>();
                                foreach (var h in ulasilabilirler[cellPosition])
                                {
                                    rotamDunya.Add(hexTilemap.GetCellCenterWorld(h));
                                }

                                secilenBirlik.GetComponent<UnitController>().YolaCik(rotamDunya);
                                stats.buTurHareketEttiMi = true; // YENİ: Birlik hareket etti olarak işaretlendi
                                GameManager.Instance.intikalPuani -= 1; 
                                secilenBirlik = null; 
                                MenziliTemizle();     
                            }
                            else Debug.Log("Yetersiz İntikal Puanı!");
                        }
                        else Debug.Log("Bu asker o kadar uzağa veya denizlerin üzerinden geçerek yürüyemez!");
                    }
                    else if (GameManager.Instance.secilenKart != null)
                    {
                        CardData oynanacakKart = GameManager.Instance.secilenKart;

                        if (oynanacakKart.orduKartiMi)
                        {
                            // DİNAMİK KAYNAK KONTROLÜ (Sadece Yemek ve AP değil, Altın ve Taş bedeli varsa epsini kontrol et)
                            if (GameManager.Instance.aksiyonPuani >= oynanacakKart.apBedeli && 
                                GameManager.Instance.yemek >= oynanacakKart.yemekBedeli &&
                                GameManager.Instance.tas >= oynanacakKart.tasBedeli &&
                                GameManager.Instance.altin >= oynanacakKart.altinBedeli)
                            {
                                Collider2D[] etraftakiler = Physics2D.OverlapCircleAll(hedefPozisyon, 1.2f);
                                bool kaleBulundu = false;

                                foreach (Collider2D obje in etraftakiler)
                                {
                                    if (obje.CompareTag("Kale")) { kaleBulundu = true; break; }
                                }

                                if (kaleBulundu)
                                {
                                    GameObject yeniOrdu = null;
                                    if (oynanacakKart.uretilecekBirlikler.Contains("Gocmen") && gocmenPrefab != null)
                                    {
                                        yeniOrdu = Instantiate(gocmenPrefab, hedefPozisyon, Quaternion.identity);
                                    }
                                    else if (oynanacakKart.uretilecekBirlikler.Contains("Kesif") && kesifBirligiPrefab != null)
                                    {
                                        yeniOrdu = Instantiate(kesifBirligiPrefab, hedefPozisyon, Quaternion.identity);
                                    }
                                    else 
                                    {
                                        yeniOrdu = Instantiate(piyadePrefab, hedefPozisyon, Quaternion.identity);
                                    }
                                    
                                    // ** TAMAMEN DİNAMİK LİSTE YÖNETİMİ **
                                    // Kartın içinde Unity üzerinden hangi birim listesini girdiysen piyonun çantasına direkt o listeyi doldur!
                                    ArmyStats yeniOrduStat = yeniOrdu.GetComponent<ArmyStats>();
                                    yeniOrduStat.icindekiBirlikler.Clear(); 
                                    yeniOrduStat.icindekiBirlikler.AddRange(oynanacakKart.uretilecekBirlikler);

                                    // Parametreli ödemeleri kes
                                    GameManager.Instance.aksiyonPuani -= oynanacakKart.apBedeli;
                                    GameManager.Instance.yemek -= oynanacakKart.yemekBedeli;
                                    GameManager.Instance.tas -= oynanacakKart.tasBedeli;
                                    GameManager.Instance.altin -= oynanacakKart.altinBedeli;
                                    
                                    GameManager.Instance.KartOynandi(); 
                                    Debug.Log($"{oynanacakKart.kartAdi} oynandı, ordu kalenin hemen dibinde kuruldu!");
                                }
                                else Debug.Log("HATA: Orduları sadece sana ait bir kalenin bitişiğindeki noktalara (garnizon dışına) konuşlandırabilirsin!");
                            }
                            else Debug.Log($"Yetersiz Kaynak! {oynanacakKart.kartAdi} kartını oynamak için tüm maliyeti karşılayamıyorsun.");
                        }
                        
                        else if (oynanacakKart.binaKartiMi)
                        {
                            if (GameManager.Instance.aksiyonPuani >= oynanacakKart.apBedeli && GameManager.Instance.tas >= oynanacakKart.tasBedeli && GameManager.Instance.altin >= oynanacakKart.altinBedeli && GameManager.Instance.yemek >= oynanacakKart.yemekBedeli)
                            {
                                // --- GÜNCELLENEN KURAL: İnşaat alanı ya kendi bölgemiz içinde, sınırımızın hemen 1 hex bitişiğinde olmalı, YA DA hedefte bir Göçmen birimimiz olmalı ---
                                Vector3Int hedefGrid = hexTilemap.WorldToCell(hedefPozisyon);
                                
                                bool insaIzniVar = bizimSinirlar.Contains(hedefGrid);
                                
                                // Hedef kendi sınırımızda değilse, komşularından herhangi biri bizim sınırımızda mı diye bak
                                if (!insaIzniVar)
                                {
                                    List<Vector3Int> komsular = GetHexKomsular(hedefGrid);
                                    foreach (Vector3Int komsu in komsular)
                                    {
                                        if (bizimSinirlar.Contains(komsu))
                                        {
                                            insaIzniVar = true; // Sınırımıza değiyor, izin ver
                                            break;
                                        }
                                    }
                                }

                                GameObject gocmenBirimi = null;
                                Collider2D[] buradakiObjeler = Physics2D.OverlapCircleAll(hedefPozisyon, 0.1f);
                                foreach (Collider2D obje in buradakiObjeler)
                                {
                                    if (obje.CompareTag("Unit"))
                                    {
                                        ArmyStats stats = obje.GetComponent<ArmyStats>();
                                        if (stats != null && stats.icindekiBirlikler.Contains("Gocmen"))
                                        {
                                            gocmenBirimi = obje.gameObject;
                                            insaIzniVar = true; // Şart B sağlandı
                                            break;
                                        }
                                    }
                                }

                                if (!insaIzniVar)
                                {
                                    Debug.Log("HATA: İzin verilmedi! Binaları sadece kendi bölgende, sınırlarına tam bitişik 1 hex dış alanda veya bir Göçmen biriminin olduğu yerde inşa edebilirsin.");
                                    return; // Oynamayı iptal et
                                }

                                if (oynanacakKart.insaEdilecekBina == "Kale")
                                {
                                    Collider2D[] yakinindakiler = Physics2D.OverlapCircleAll(hedefPozisyon, 1.8f);
                                    bool cokYakin = false;

                                    foreach (Collider2D obje in yakinindakiler)
                                    {
                                        if (obje.CompareTag("Kale") || obje.GetComponent<MakroKale>() != null) { cokYakin = true; break; }
                                    }

                                    if (cokYakin)
                                    {
                                        Debug.Log("HATA: Spacing Kuralı! Kaleler birbirine bu kadar yakın (" + 1.8f + " hex) inşa edilemez.");
                                        return;
                                    }

                                    if (kalePrefab != null) Instantiate(kalePrefab, hedefPozisyon, Quaternion.identity);
                                    Debug.Log("Sınır Genişletildi! Kale başarıyla inşa edildi.");
                                }
                                else if (oynanacakKart.insaEdilecekBina == "Koy")
                                {
                                    Collider2D[] yakinindakiler = Physics2D.OverlapCircleAll(hedefPozisyon, 1.8f);
                                    bool cokYakin = false;

                                    foreach (Collider2D obje in yakinindakiler)
                                    {
                                        if (obje.CompareTag("Koy") || obje.CompareTag("Kale") || obje.GetComponent<MakroKale>() != null) { cokYakin = true; break; }
                                    }

                                    if (cokYakin)
                                    {
                                        Debug.Log("HATA: Spacing Kuralı! Köyler, kalelere veya diğer köylere yan yana (bitişik) inşa edilemez.");
                                        return;
                                    }

                                    if (koyPrefab != null) Instantiate(koyPrefab, hedefPozisyon, Quaternion.identity);
                                    Debug.Log("Sınır Genişletildi! Köy başarıyla inşa edildi.");
                                }
                                else
                                {
                                    Debug.Log("HATA: Bilinmeyen bina türü. Lütfen CardData içindeki 'insaEdilecekBina' kutusuna 'Kale' veya 'Koy' yazın.");
                                    return;
                                }

                                // Kaynakları kes, kartı tüket ve sınırları yenile
                                GameManager.Instance.aksiyonPuani -= oynanacakKart.apBedeli;
                                GameManager.Instance.tas -= oynanacakKart.tasBedeli;
                                GameManager.Instance.altin -= oynanacakKart.altinBedeli;
                                GameManager.Instance.yemek -= oynanacakKart.yemekBedeli;
                                
                                if (gocmenBirimi != null)
                                {
                                    Destroy(gocmenBirimi);
                                    Debug.Log("Göçmen birimi kullanılarak inşaat tamamlandı, birim yok edildi.");
                                }

                                GameManager.Instance.KartOynandi(); 
                                SinirlariVeSisiGuncelle(); // Sınır anında yenilensin
                            }
                            else Debug.Log($"Yetersiz Kaynak! {oynanacakKart.kartAdi} kartını oynamak için tüm bedelleri karşılayamıyorsun.");
                        }
                        else 
                        {
                            // YENİ: BUFF VEYA SABOTAJ (Hedefli Kart) BUG FIX: OverlapCircleAll kullanıldı
                            if (GameManager.Instance.aksiyonPuani >= oynanacakKart.apBedeli && GameManager.Instance.tas >= oynanacakKart.tasBedeli && GameManager.Instance.altin >= oynanacakKart.altinBedeli && GameManager.Instance.yemek >= oynanacakKart.yemekBedeli)
                            {
                                Collider2D[] hedeflenenObjeler = Physics2D.OverlapCircleAll(hedefPozisyon, 0.5f);
                                ArmyStats orduStats = null;
                                MakroKale kaleStats = null;

                                foreach (Collider2D obje in hedeflenenObjeler)
                                {
                                    if (obje.GetComponent<ArmyStats>() != null) orduStats = obje.GetComponent<ArmyStats>();
                                    if (obje.GetComponent<MakroKale>() != null) kaleStats = obje.GetComponent<MakroKale>();
                                }

                                if (orduStats != null)
                                {
                                    if (oynanacakKart.kendiOrdunuFedaEt && !orduStats.dusmanMi)
                                    {
                                        Destroy(orduStats.gameObject);
                                        Debug.Log("Kendi ordu feda edildi!");
                                    }
                                    else
                                    {
                                        orduStats.hasarGucu += oynanacakKart.orduHasarArtisi;
                                        orduStats.mevcutCan += oynanacakKart.orduCanArtisi;
                                        if (oynanacakKart.orduCanArtisi > 0) orduStats.maxCan += oynanacakKart.orduCanArtisi;
                                        
                                        if (oynanacakKart.orduHareketHiziArtisi > 0)
                                        {
                                            orduStats.hareketMenzili += oynanacakKart.orduHareketHiziArtisi;
                                        }
                                        
                                        if (oynanacakKart.dusmanHareketEngelle && orduStats.dusmanMi)
                                        {
                                            orduStats.buTurHareketEttiMi = true; 
                                            Debug.Log("Düşman ordusu donduruldu!");
                                        }
                                        if (oynanacakKart.dusmanIntikalSifirla && orduStats.dusmanMi)
                                        {
                                            orduStats.buTurHareketEttiMi = true; // Intikal sistemi tam gelişmediği için hareket hakkını alıyoruz
                                            Debug.Log("Düşmanın tedariki kesildi!");
                                        }
                                        if (oynanacakKart.dusmanBirlikYokEt > 0 && orduStats.dusmanMi)
                                        {
                                            orduStats.mevcutCan = Mathf.Max(0, orduStats.mevcutCan - oynanacakKart.dusmanBirlikYokEt);
                                            Debug.Log("Düşman birliğine suikast/rüşvet yapıldı!");
                                            if(orduStats.mevcutCan <= 0) Destroy(orduStats.gameObject);
                                        }
                                        if (oynanacakKart.dusmanOrduyuCal && orduStats.dusmanMi)
                                        {
                                            orduStats.dusmanMi = false; // Taraf değiştir
                                            SpriteRenderer sr = orduStats.GetComponent<SpriteRenderer>();
                                            if (sr != null) sr.color = Color.white; // Rengi normale çevir
                                            Debug.Log("Düşman ordusu taraf değiştirdi!");
                                        }
                                        if (oynanacakKart.ekHareketHakki > 0 && !orduStats.dusmanMi)
                                        {
                                            orduStats.buTurHareketEttiMi = false; 
                                            Debug.Log("Orduya ek hareket hakkı verildi!");
                                        }
                                        
                                        orduStats.CanYazisiniGuncelle();
                                    }

                                    GameManager.Instance.aksiyonPuani -= oynanacakKart.apBedeli;
                                    GameManager.Instance.altin -= oynanacakKart.altinBedeli;
                                    GameManager.Instance.tas -= oynanacakKart.tasBedeli;
                                    GameManager.Instance.yemek -= oynanacakKart.yemekBedeli;
                                    GameManager.Instance.KartOynandi();
                                }
                                else if (kaleStats != null)
                                {
                                    kaleStats.maxKapiCani += oynanacakKart.kaleKapiCaniArtisi;
                                    kaleStats.kapiCani += oynanacakKart.kaleKapiCaniArtisi; 
                                    
                                    GameManager.Instance.aksiyonPuani -= oynanacakKart.apBedeli;
                                    GameManager.Instance.altin -= oynanacakKart.altinBedeli;
                                    GameManager.Instance.tas -= oynanacakKart.tasBedeli;
                                    GameManager.Instance.yemek -= oynanacakKart.yemekBedeli;
                                    GameManager.Instance.KartOynandi();
                                }
                                else
                                {
                                    Debug.Log("HATA: Boşluğa tıklayamazsın. Bir hedef (ordu/kale) seçmelisin.");
                                }
                            }
                            else Debug.Log($"Yetersiz Kaynak! {oynanacakKart.kartAdi} maliyetini karşılamıyorsun.");
                        }
                    }
                }
                // YENİ EKLENEN: EĞER SUYA TIKLANDIYSA (İPTAL ET)
                else 
                {
                    if (GameManager.Instance.secilenKart != null)
                    {
                        GameManager.Instance.secilenKart = null;
                        Debug.Log("Suya tıklandı. Kart seçimi iptal edildi.");
                    }
                    if (secilenBirlik != null)
                    {
                        secilenBirlik = null;
                        MenziliTemizle();
                    }
                }
            }
        }
    }

    void MenziliCiz(GameObject asker)
    {
        MenziliTemizle(); 
        Vector3Int merkezHex = hexTilemap.WorldToCell(asker.transform.position);
        ArmyStats stats = asker.GetComponent<ArmyStats>();
        
        Dictionary<Vector3Int, List<Vector3Int>> ulasilabilir = BFSYolBul(merkezHex, stats.hareketMenzili);

        foreach (var pos in ulasilabilir.Keys)
        {
            if (pos != merkezHex) // Kendisine parlama koymasın
            {
                Vector3 tilePos = hexTilemap.GetCellCenterWorld(pos);
                RaycastHit2D hit = Physics2D.Raycast(tilePos, Vector2.zero);
                
                if (hit.collider == null || !hit.collider.CompareTag("Unit"))
                {
                    GameObject parlama = Instantiate(menzilPrefab, tilePos, Quaternion.identity);
                    aktifMenziller.Add(parlama);
                }
            }
        }
    }

    void MenziliTemizle()
    {
        foreach (var parlama in aktifMenziller)
        {
            Destroy(parlama);
        }
        aktifMenziller.Clear();
    }

    // --- 4X VİZYONU: SİS, BAŞKENT VE BÖLGE (TERRITORY) MEKANİKLERİ ---
    
    void HaritayiSisleKapla()
    {
        if (fogTilemap == null || fogTile == null) return;

        // YENİ EKLENDİ: Editörden haritayı genişletirsen, oyunun bu yeni sınırları tanıması için önce güncelletiyoruz.
        hexTilemap.CompressBounds(); 

        BoundsInt bounds = hexTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (hexTilemap.HasTile(pos)) // Sadece zemin olan yerlere (Uzay boşluğuna değil) sis at
            {
                fogTilemap.SetTile(pos, fogTile);
            }
        }
    }

    void BaskentKur()
    {
        BoundsInt bounds = hexTilemap.cellBounds;
        List<Vector3Int> uygunKaralar = new List<Vector3Int>();
        
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (hexTilemap.HasTile(pos))
            {
                TileBase tile = hexTilemap.GetTile(pos);
                if (tile == cimenTile) uygunKaralar.Add(pos); 
            }
        }

        if (uygunKaralar.Count > 0)
        {
            int r = Random.Range(0, uygunKaralar.Count);
            Vector3Int secilenKare = uygunKaralar[r];
            Vector3 dunyaPozisyonu = hexTilemap.GetCellCenterWorld(secilenKare);
            
            Instantiate(kalePrefab, dunyaPozisyonu, Quaternion.identity);
            Debug.Log($"[SİSTEM] Başkent başarıyla haritanın {secilenKare} köşesinde kuruldu!");
            
            // YENİ EKLENDİ: Oyun başladığında kamerayı Başkent'in tam tepesine ışınla
            if (Camera.main != null)
            {
                Vector3 kameraHedefi = new Vector3(dunyaPozisyonu.x, dunyaPozisyonu.y, Camera.main.transform.position.z);
                Camera.main.transform.position = kameraHedefi;
            }
        }
    }

    void DusmanYapayZekasiniKur()
    {
        if (dusmanKalesiPrefab == null) return;
        
        // Bizim kalemizi bul (Başkent)
        GameObject bizimBaskent = GameObject.FindGameObjectWithTag("Kale");
        if (bizimBaskent == null) return;

        BoundsInt bounds = hexTilemap.cellBounds;
        List<Vector3Int> uygunKaralar = new List<Vector3Int>();
        
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (hexTilemap.HasTile(pos))
            {
                TileBase tile = hexTilemap.GetTile(pos);
                // Yapay Zeka başkenti de TEK bir çimenliğe kurulsun
                if (tile == cimenTile) 
                {
                    Vector3 dunyaPos = hexTilemap.GetCellCenterWorld(pos);
                    // Mesafe şartı 40x30 haritada çok büyük olunca kale doğmuyordu, 14f'e düşürdük
                    if (Vector2.Distance(dunyaPos, bizimBaskent.transform.position) > 14f)
                    {
                        uygunKaralar.Add(pos);
                    }
                }
            }
        }

        // Eğer 14f bile çok büyük geldiyse (çok küçük harita üretildiyse) Fallback yap
        if (uygunKaralar.Count == 0)
        {
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (hexTilemap.HasTile(pos) && hexTilemap.GetTile(pos) == cimenTile)
                {
                    Vector3 dunyaPos = hexTilemap.GetCellCenterWorld(pos);
                    if (Vector2.Distance(dunyaPos, bizimBaskent.transform.position) > 8f) uygunKaralar.Add(pos);
                }
            }
        }

        if (uygunKaralar.Count > 0)
        {
            int r = Random.Range(0, uygunKaralar.Count);
            Vector3Int adayGride = uygunKaralar[r];
            Vector3 adayDunya = hexTilemap.GetCellCenterWorld(adayGride);
            
            Instantiate(dusmanKalesiPrefab, adayDunya, Quaternion.identity);
            Debug.Log($"[SİSTEM] Düşman Merkez Üssü başarıyla kuruldu! Bizim kalemizden uzaklığı garanti edildi.");
        }
    }

    void HaydutKamplariniKoy()
    {
        if (haydutKampiPrefab == null) return;
        
        BoundsInt bounds = hexTilemap.cellBounds;
        List<Vector3Int> uygunCimenler = new List<Vector3Int>();
        
        GameObject bizimBaskent = GameObject.FindGameObjectWithTag("Kale");

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (hexTilemap.HasTile(pos))
            {
                TileBase tile = hexTilemap.GetTile(pos);
                if (tile == cimenTile) 
                {
                    Vector3 dunyaPos = hexTilemap.GetCellCenterWorld(pos);
                    // Eğer oyuncunun başkenti varsa, haydutlar yakınına konmasın
                    if (bizimBaskent != null && Vector2.Distance(dunyaPos, bizimBaskent.transform.position) > 12f)
                    {
                        uygunCimenler.Add(pos); 
                    }
                    else if (bizimBaskent == null)
                    {
                        uygunCimenler.Add(pos);
                    }
                }
            }
        }

        int maxHaydutKamp = 4;
        int uretilen = 0;
        
        while (uretilen < maxHaydutKamp && uygunCimenler.Count > 0)
        {
            int r = Random.Range(0, uygunCimenler.Count);
            Vector3 dunyaDuzen = hexTilemap.GetCellCenterWorld(uygunCimenler[r]);
            
            // Etrafında başka kamp veya kale olmasın
            Collider2D[] yakinlikTesti = Physics2D.OverlapCircleAll(dunyaDuzen, 2f);
            if (yakinlikTesti.Length == 0)
            {
                Instantiate(haydutKampiPrefab, dunyaDuzen, Quaternion.identity);
                uretilen++;
            }
            uygunCimenler.RemoveAt(r);
        }
        Debug.Log($"[SİSTEM] Toplam {uretilen} adet rastgele Haydut Kampı yerleştirildi.");
    }

    public List<Vector3Int> GetHexKomsular(Vector3Int hex)
    {
        List<Vector3Int> komsular = new List<Vector3Int>();
        bool isEvenY = (Mathf.Abs(hex.y) % 2 == 0);

        if (isEvenY)
        {
            komsular.Add(new Vector3Int(hex.x + 1, hex.y, 0));
            komsular.Add(new Vector3Int(hex.x, hex.y - 1, 0));
            komsular.Add(new Vector3Int(hex.x - 1, hex.y - 1, 0));
            komsular.Add(new Vector3Int(hex.x - 1, hex.y, 0));
            komsular.Add(new Vector3Int(hex.x - 1, hex.y + 1, 0));
            komsular.Add(new Vector3Int(hex.x, hex.y + 1, 0));
        }
        else
        {
            komsular.Add(new Vector3Int(hex.x + 1, hex.y, 0));
            komsular.Add(new Vector3Int(hex.x + 1, hex.y - 1, 0));
            komsular.Add(new Vector3Int(hex.x, hex.y - 1, 0));
            komsular.Add(new Vector3Int(hex.x - 1, hex.y, 0));
            komsular.Add(new Vector3Int(hex.x, hex.y + 1, 0));
            komsular.Add(new Vector3Int(hex.x + 1, hex.y + 1, 0));
        }
        return komsular;
    }

    public Dictionary<Vector3Int, List<Vector3Int>> BFSYolBul(Vector3Int baslangic, int menzil)
    {
        Dictionary<Vector3Int, List<Vector3Int>> yollar = new Dictionary<Vector3Int, List<Vector3Int>>();
        Queue<Vector3Int> kuyruk = new Queue<Vector3Int>();
        
        yollar[baslangic] = new List<Vector3Int>();
        kuyruk.Enqueue(baslangic);

        while (kuyruk.Count > 0)
        {
            Vector3Int gecerli = kuyruk.Dequeue();
            List<Vector3Int> gecerliYol = yollar[gecerli];

            if (gecerliYol.Count >= menzil) continue;

            foreach (Vector3Int komsu in GetHexKomsular(gecerli))
            {
                if (hexTilemap.HasTile(komsu))
                {
                    TileBase tile = hexTilemap.GetTile(komsu);
                    if (tile.name != "Su" && tile.name != "Deniz")
                    {
                        if (!yollar.ContainsKey(komsu))
                        {
                            Vector3 komsuDunya = hexTilemap.GetCellCenterWorld(komsu);
                            Collider2D[] engelVarMi = Physics2D.OverlapCircleAll(komsuDunya, 0.1f);
                            bool engel = false;
                            foreach (var e in engelVarMi) { if(e.GetComponent<MakroKale>() != null || e.CompareTag("Unit")) engel = true;  }

                            List<Vector3Int> yeniYol = new List<Vector3Int>(gecerliYol);
                            yeniYol.Add(komsu);
                            yollar[komsu] = yeniYol;
                            
                            // Eğer engel yoksa arkasındaki taşlara da devam et
                            if (!engel) kuyruk.Enqueue(komsu);
                        }
                    }
                }
            }
        }
        return yollar;
    }

    public void SinirlariVeSisiGuncelle()
    {
        if (hexTilemap == null) return;

        foreach (GameObject cizgi in aktifSinirCizgileri) Destroy(cizgi);
        aktifSinirCizgileri.Clear();
        bizimSinirlar.Clear();
        dusmanSinirlar.Clear();

        // Kaleleri Kategorilendir
        GameObject[] bizimKaleler = GameObject.FindGameObjectsWithTag("Kale");
        MakroKale[] tumKaleler = FindObjectsByType<MakroKale>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<GameObject> dusmanKaleler = new List<GameObject>();
        foreach (var k in tumKaleler)
        {
            if (!k.CompareTag("Kale")) dusmanKaleler.Add(k.gameObject);
        }

        // ÖNCE DÜŞMAN BÖLGELER (1 Hex - Yarıçap)
        foreach (GameObject kale in dusmanKaleler)
        {
            Vector3Int kaleGride = hexTilemap.WorldToCell(kale.transform.position);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int adayKare = new Vector3Int(kaleGride.x + x, kaleGride.y + y, 0);
                    Vector3 adayKareDunya = hexTilemap.GetCellCenterWorld(adayKare);
                    if (Vector2.Distance(kale.transform.position, adayKareDunya) <= 1.2f) 
                    {
                        if (hexTilemap.HasTile(adayKare) && !dusmanSinirlar.Contains(adayKare)) dusmanSinirlar.Add(adayKare);
                    }
                }
            }
        }

        // SONRA BİZİM BÖLGELER (3 Hex - Düşman Tarafından Ezilir)
        foreach (GameObject kale in bizimKaleler)
        {
            Vector3Int kaleGride = hexTilemap.WorldToCell(kale.transform.position);
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    Vector3Int adayKare = new Vector3Int(kaleGride.x + x, kaleGride.y + y, 0);
                    Vector3 adayKareDunya = hexTilemap.GetCellCenterWorld(adayKare);
                    if (Vector2.Distance(kale.transform.position, adayKareDunya) <= 3.2f) 
                    {
                        // Düşman sınırına denk gelen kareleri BİZİM sınırımıza EKLEME (rakip sınırına girmesin)
                        if (hexTilemap.HasTile(adayKare) && !dusmanSinirlar.Contains(adayKare) && !bizimSinirlar.Contains(adayKare)) 
                        {
                            bizimSinirlar.Add(adayKare);
                        }
                    }
                }
            }
        }

        // BİZİM KÖYLERİMİZ (1 Hex Yarıçaplı Ufak Yayılma)
        GameObject[] bizimKoyler = GameObject.FindGameObjectsWithTag("Koy");
        foreach (GameObject koy in bizimKoyler)
        {
            Vector3Int koyGride = hexTilemap.WorldToCell(koy.transform.position);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int adayKare = new Vector3Int(koyGride.x + x, koyGride.y + y, 0);
                    Vector3 adayKareDunya = hexTilemap.GetCellCenterWorld(adayKare);
                    if (Vector2.Distance(koy.transform.position, adayKareDunya) <= 1.2f) 
                    {
                        if (hexTilemap.HasTile(adayKare) && !dusmanSinirlar.Contains(adayKare) && !bizimSinirlar.Contains(adayKare)) 
                        {
                            bizimSinirlar.Add(adayKare);
                        }
                    }
                }
            }
        }

        // Sınır Çizimleri
        float w = hexTilemap.cellSize.x; 
        float h = hexTilemap.cellSize.y; 

        // Bizim mavi sınırlar her zaman çizilir
        CizgileriUret(bizimSinirlar, sinirRengi, w, h, true, null);
        
        // Düşmanın kırmızı sınırları Bizim Sınırlarımızla temas ettiği kenarlarda ÇİZİLMEZ (Çakışmayı önler, mavi görünür)
        CizgileriUret(dusmanSinirlar, dusmanSinirRengi, w, h, false, bizimSinirlar);
        
        // Askerlerin yürüdüğü rotaları sisin içinden arındırma
        GameObject[] askerler = GameObject.FindGameObjectsWithTag("Unit");
        foreach(GameObject asker in askerler)
        {
            Vector3Int askerGrid = hexTilemap.WorldToCell(asker.transform.position);
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    Vector3Int etrafKare = new Vector3Int(askerGrid.x + x, askerGrid.y + y, 0);
                    Vector3 etrafDunya = hexTilemap.GetCellCenterWorld(etrafKare);
                    
                    if (Vector2.Distance(asker.transform.position, etrafDunya) <= 2.2f) 
                    {
                        if (fogTilemap != null) fogTilemap.SetTile(etrafKare, null);
                    }
                }
            }
        }
    }

    void CizgileriUret(List<Vector3Int> bolgeListesi, Color renk, float w, float h, bool sisiKaldir, List<Vector3Int> cizilmeyecekKomsular = null)
    {
        foreach (Vector3Int sinirKaresi in bolgeListesi)
        {
            if (sisiKaldir && fogTilemap != null) 
            {
                // 1) Kendi bölgemizdeki (sınır içindeki) sisi kaldır
                fogTilemap.SetTile(sinirKaresi, null);
                
                // 2) YENİ GÜNCELLEME: Sınırımızın 1 hex dışındaki tampon bölgenin (komşuların) sisini de kaldır
                List<Vector3Int> komsular = GetHexKomsular(sinirKaresi);
                foreach (Vector3Int komsu in komsular)
                {
                    fogTilemap.SetTile(komsu, null);
                }
            }

            Vector3 center = hexTilemap.GetCellCenterWorld(sinirKaresi);
            
            Vector3[] c = new Vector3[6];
            c[0] = center + new Vector3(0, h / 2f, 0);        
            c[1] = center + new Vector3(w / 2f, h / 4f, 0);   
            c[2] = center + new Vector3(w / 2f, -h / 4f, 0);  
            c[3] = center + new Vector3(0, -h / 2f, 0);       
            c[4] = center + new Vector3(-w / 2f, -h / 4f, 0); 
            c[5] = center + new Vector3(-w / 2f, h / 4f, 0);  

            for (int i = 0; i < 6; i++)
            {
                Vector3 kose1 = c[i];
                Vector3 kose2 = c[(i + 1) % 6];
                
                Vector3 merkezdenKenara = ((kose1 + kose2) / 2f) - center;
                Vector3 komsuMerkezi = center + (merkezdenKenara * 2.0f);
                Vector3Int komsuTileGride = hexTilemap.WorldToCell(komsuMerkezi);

                if (!bolgeListesi.Contains(komsuTileGride))
                {
                    // Eğer karşı taraf "çizilmeyecek" bir komşuysa (mesela Bizim Bölgemizse), o kenara Kırmızı çizgi çekme
                    if (cizilmeyecekKomsular != null && cizilmeyecekKomsular.Contains(komsuTileGride))
                    {
                        continue;
                    }
                    KenarCizgisiYarat(kose1, kose2, renk);
                }
            }
        }
    }

    void KenarCizgisiYarat(Vector3 baslamaNoktasi, Vector3 bitisNoktasi, Color renk)
    {
        GameObject cizgiObjesi = new GameObject("HudutCizgisi");
        cizgiObjesi.transform.SetParent(this.transform);

        LineRenderer lr = cizgiObjesi.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, baslamaNoktasi);
        lr.SetPosition(1, bitisNoktasi);
        
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.numCapVertices = 2;
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = renk;
        lr.endColor = renk;
        lr.sortingOrder = 5;

        aktifSinirCizgileri.Add(cizgiObjesi);
    }

    // YENİ EKONOMİ: Bize ait mavi sınırların içindeki belirli bir kare tipinden (örn: Altın) kaç tane olduğunu sayar
    public int SinirlarIcindekiTileSayisi(TileBase arananTile)
    {
        if (arananTile == null || hexTilemap == null) return 0;
        
        int sayac = 0;
        foreach (Vector3Int pos in bizimSinirlar)
        {
            if (hexTilemap.HasTile(pos))
            {
                TileBase suAnkiTile = hexTilemap.GetTile(pos);
                if (suAnkiTile == arananTile)
                {
                    sayac++;
                }
            }
        }
        return sayac;
    }
}