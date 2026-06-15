using UnityEngine;
using System.Collections.Generic;

public class EnemyAIManager : MonoBehaviour
{
    public static EnemyAIManager Instance { get; private set; }
    
    public int intikalPuani = 0;
    private GameObject aktifKesifBirli;
    private GameObject dusmanBaskenti;
    private Vector3Int? kesifHedefi = null; // YENİ: Keşif için uzun vadeli rota hedefi
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void DusmanTuru()
    {
        intikalPuani = 1;
        
        if (dusmanBaskenti == null)
        {
            GameObject go = GameObject.Find("DusmanKalesi(Clone)");
            if (go != null) dusmanBaskenti = go;
            
            if (dusmanBaskenti == null)
            {
                Debug.Log("[ENEMY AI] Düşman başkenti bulunamadı!");
                return;
            }
        }
        
        MapController mapCtrl = Object.FindAnyObjectByType<MapController>();
        if (mapCtrl == null) return;

        // 1- Spawn işlemi (Eğer piyasa boşsa)
        if (aktifKesifBirli == null)
        {
            if (mapCtrl.dusmanKesifPrefab != null)
            {
                Vector3Int hqHex = mapCtrl.hexTilemap.WorldToCell(dusmanBaskenti.transform.position);
                List<Vector3Int> hqNeighbors = mapCtrl.GetHexKomsular(hqHex);
                Vector3 spawnPos = dusmanBaskenti.transform.position; // Default: Kalenin üstü
                
                // Müsait çimenlik komşu bul (Kalenin içine spawn olmasın diye)
                foreach(var hex in hqNeighbors) {
                    if (mapCtrl.hexTilemap.HasTile(hex) && mapCtrl.hexTilemap.GetTile(hex) == mapCtrl.cimenTile)
                    {
                        Vector3 wPos = mapCtrl.hexTilemap.GetCellCenterWorld(hex);
                        Collider2D[] col = Physics2D.OverlapCircleAll(wPos, 0.5f);
                        if (col.Length == 0) {
                            spawnPos = wPos;
                            break;
                        }
                    }
                }
                
                aktifKesifBirli = Instantiate(mapCtrl.dusmanKesifPrefab, spawnPos, Quaternion.identity);
                aktifKesifBirli.name = "DusmanKesif_Birligi";
                Debug.Log("[ENEMY AI] Düşman Başkenti çevresinde yeni bir Keşif Birliği eğitildi.");
                intikalPuani--;
            }
        }
        else // 2- Hareket ve Saldırı işlemi
        {
            // Radar Taraması (Yaklaşık 4.5 birim yarıçap, yani etrafındaki 4-5 hex)
            GameObject hedefObj = null;
            float minMesafe = 9999f;
            
            Collider2D[] gorusMenzili = Physics2D.OverlapCircleAll(aktifKesifBirli.transform.position, 4.5f);
            foreach (Collider2D col in gorusMenzili)
            {
                if (col.gameObject == aktifKesifBirli) continue; // Kendini hedef alma
                
                bool hedeflenebilir = false;
                if (col.CompareTag("Haydut")) hedeflenebilir = true;
                if (col.CompareTag("Koy")) hedeflenebilir = true;
                if (col.CompareTag("Unit")) hedeflenebilir = true; // Oyuncunun ordu/keşif birliği
                
                if (hedeflenebilir)
                {
                    float d = Vector2.Distance(aktifKesifBirli.transform.position, col.transform.position);
                    if (d < minMesafe) {
                        minMesafe = d;
                        hedefObj = col.gameObject;
                    }
                }
            }

            int adimSayisi = 2; // Bir ap ile 2 hex hareket
            for (int adim = 0; adim < adimSayisi; adim++)
            {
                if (aktifKesifBirli == null) break; // Eğer ilk adımda ölürse devam etme
                
                Vector3Int currentHex = mapCtrl.hexTilemap.WorldToCell(aktifKesifBirli.transform.position);
                List<Vector3Int> neighbors = mapCtrl.GetHexKomsular(currentHex);

                Vector3 hedeflenenNokta = Vector3.zero;
                bool moved = false;

                if (hedefObj != null)
                {
                    kesifHedefi = null; // Savaş/hedef bulunduğu an keşif rotasını hafızadan sil

                    // YENİ: Göle veya engele takılmamak için Kus Uçuşu (Greedy) yerine BFS Yol Bulma algoritmasını kullan
                    Vector3Int targetHex = mapCtrl.hexTilemap.WorldToCell(hedefObj.transform.position);
                    Dictionary<Vector3Int, List<Vector3Int>> yollar = mapCtrl.BFSYolBul(currentHex, 20); // 20 hex uzağa kadar tarar

                    Vector3Int bestHex = currentHex;

                    if (yollar.ContainsKey(targetHex) && yollar[targetHex].Count > 0)
                    {
                        // Hedefe giden kesintisiz bir yol var, ilk adımını at
                        bestHex = yollar[targetHex][0];
                    }
                    else
                    {
                        // Hedef çok uzaksa veya tamamen kapalıysa, en azından hedefe en çok yaklaşabileceği yöne doğru ilk adımını at
                        float bestD = 9999f;
                        foreach (var kvp in yollar)
                        {
                            float d = Vector2.Distance(mapCtrl.hexTilemap.GetCellCenterWorld(kvp.Key), hedefObj.transform.position);
                            if (d < bestD && kvp.Value.Count > 0)
                            {
                                bestD = d;
                                bestHex = kvp.Value[0];
                            }
                        }
                    }

                    if (bestHex != currentHex)
                    {
                        aktifKesifBirli.transform.position = mapCtrl.hexTilemap.GetCellCenterWorld(bestHex);
                        moved = true;
                    }
                }
                else
                {
                    // --- YENİ EKLENEN KEŞİF (AMAC) SİSTEMİ ---
                    // Eğer hedefimiz yoksa veya hedefe ulaştıysak yeni, çok uzak bir kara parçası seç
                    if (kesifHedefi == null || Vector2.Distance(aktifKesifBirli.transform.position, mapCtrl.hexTilemap.GetCellCenterWorld(kesifHedefi.Value)) < 2f)
                    {
                        BoundsInt bounds = mapCtrl.hexTilemap.cellBounds;
                        List<Vector3Int> uzakKaralar = new List<Vector3Int>();
                        foreach (var pos in bounds.allPositionsWithin)
                        {
                            if (mapCtrl.hexTilemap.HasTile(pos) && mapCtrl.hexTilemap.GetTile(pos) == mapCtrl.cimenTile)
                            {
                                if (Vector2.Distance(mapCtrl.hexTilemap.GetCellCenterWorld(pos), aktifKesifBirli.transform.position) > 12f)
                                {
                                    uzakKaralar.Add(pos);
                                }
                            }
                        }
                        
                        if (uzakKaralar.Count > 0)
                        {
                            kesifHedefi = uzakKaralar[Random.Range(0, uzakKaralar.Count)];
                            Debug.Log($"[ENEMY AI] Yeni keşif amacı belirlendi! Hedef koordinat: {kesifHedefi.Value}");
                        }
                    }

                    // Belirlenen o amaca doğru BFS algoritmasıyla (dağları gölleri aşarak) adım adım yürü
                    if (kesifHedefi != null)
                    {
                        Vector3Int targetHex = kesifHedefi.Value;
                        Dictionary<Vector3Int, List<Vector3Int>> yollar = mapCtrl.BFSYolBul(currentHex, 20);

                        Vector3Int bestHex = currentHex;
                        if (yollar.ContainsKey(targetHex) && yollar[targetHex].Count > 0)
                        {
                            bestHex = yollar[targetHex][0];
                        }
                        else
                        {
                            float bestD = 9999f;
                            foreach (var kvp in yollar)
                            {
                                float d = Vector2.Distance(mapCtrl.hexTilemap.GetCellCenterWorld(kvp.Key), mapCtrl.hexTilemap.GetCellCenterWorld(targetHex));
                                if (d < bestD && kvp.Value.Count > 0)
                                {
                                    bestD = d;
                                    bestHex = kvp.Value[0];
                                }
                            }
                        }

                        if (bestHex != currentHex)
                        {
                            aktifKesifBirli.transform.position = mapCtrl.hexTilemap.GetCellCenterWorld(bestHex);
                            moved = true;
                        }
                        else
                        {
                            kesifHedefi = null; // Tıkanırsa inat etmesin, iptal edip yeni yer baksın
                        }
                    }
                }

                if (!moved) 
                {
                    Debug.Log("[ENEMY AI] Düşman Keşifi yürüyecek tile bulamadı.");
                    break; 
                }

                // EĞER hedefimiz varsa ve yanına kadar ulaştıysak saldır!
                if (hedefObj != null)
                {
                    float currentDist = Vector2.Distance(aktifKesifBirli.transform.position, hedefObj.transform.position);
                    if (currentDist <= 1.5f) // Yanındaki hexte ise
                    {
                        if (hedefObj.CompareTag("Haydut") && hedefObj.GetComponent<HaydutKampi>() != null)
                        {
                            // 1- ARKA PLAN SAVAŞI (Vahşi Haydutlara Karşı)
                            int dusmanGucu = Random.Range(1, 21) + 2; // Yapay Zekaya minik bir +2 bonus
                            int haydutZirh = hedefObj.GetComponent<HaydutKampi>().zirhDegeri;
                            
                            if (dusmanGucu >= haydutZirh) {
                                hedefObj.GetComponent<HaydutKampi>().YokOl();
                                Debug.Log($"[ENEMY AI] Savaş Oynandı: Haydutlar temizlendi! (DüşmanZarı: {dusmanGucu} vs Zırh: {haydutZirh})");
                                break; // YENİ EKLENDİ: Savaş kazandıysa turu bitirmesi lazım, yoksa yok olmuş objeye saldırmaya devam eder ve bug'a girer!
                            } else {
                                Destroy(aktifKesifBirli);
                                Debug.Log($"[ENEMY AI] Hezimet! Düşman Keşifi haydut kampında öldürüldü! (DüşmanZarı: {dusmanGucu} vs Zırh: {haydutZirh})");
                                break; // Öldü, turu bitti
                            }
                        }
                        else
                        {
                            // 2- OYUNCU İLE SAVAŞ (Köy veya Keşif Birliği / Ordu)
                            // YENİ: Göçmen kontrolü! Eğer hedeflenen oyuncu birimi Göçmen ise, savaş ekranına girme, direkt öldür!
                            bool isGocmen = false;
                            if (hedefObj.CompareTag("Unit"))
                            {
                                ArmyStats hedefStats = hedefObj.GetComponent<ArmyStats>();
                                if (hedefStats != null && hedefStats.icindekiBirlikler.Contains("Gocmen"))
                                {
                                    isGocmen = true;
                                }
                            }

                            if (isGocmen)
                            {
                                Destroy(hedefObj);
                                Debug.Log("[ENEMY AI] Düşman Birliği, oyuncunun savunmasız Göçmen'ini yakaladı ve anında katletti!");
                            }
                            else
                            {
                                int savunanZirh = 10; // Köy Zırhı vs. varsayalım
                                if (hedefObj.GetComponent<MakroKoy>() != null) savunanZirh = hedefObj.GetComponent<MakroKoy>().zirhDegeri;
                                else if (hedefObj.CompareTag("Unit")) savunanZirh = 13; // Sizin oyuncu birliğinizin savunması (örn 13)
                                
                                if (MakroSavasManager.Instance != null) {
                                    // savunmaModu = true olarak gönderiyoruz
                                    MakroSavasManager.Instance.SavasiBaslat(aktifKesifBirli, hedefObj, savunanZirh, true);
                                    Debug.Log("[ENEMY AI] OYUNCUYA SALDIRDI! MAKRO SAVAŞ BAŞLATILDI!");
                                }
                            }
                            
                            // Pop-up ekranı oyunun akışını durduracağı veya birim yok edildiği için AI hamlesini burada anında kesmeli
                            break; 
                        }
                    }
                }
            }
            intikalPuani--;
        }
    }
}
