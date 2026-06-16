using UnityEngine;
using System.Collections.Generic;

public class EnemyAIManager : MonoBehaviour
{
    public static EnemyAIManager Instance { get; private set; }
    
    public int intikalPuani = 0;
    
    // Keşif Birliği Değişkenleri
    private GameObject aktifKesifBirli;
    private Vector3Int? kesifHedefi = null; 
    
    // YENİ: Düşman Ordusu Değişkenleri
    private GameObject aktifDusmanOrdusu;
    private Vector3Int? orduHedefi = null;

    private GameObject dusmanBaskenti;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void DusmanTuru()
    {
        intikalPuani = 1; // Yapay Zeka her tur 1 intikal puanına sahip, ama ordu ve keşif 1'er ap kullanarak ayrı ayrı hareket edecekler
        
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

        // --- LANNISTER TARAF DEĞİŞTİRME KONTROLÜ ---
        // Eğer oyuncu bu orduyu kendi tarafına çektiyse, AI bu orduyu yönetmeyi bırakmalı
        if (aktifDusmanOrdusu != null)
        {
            ArmyStats stats = aktifDusmanOrdusu.GetComponent<ArmyStats>();
            if (stats != null && !stats.dusmanMi)
            {
                Debug.Log("[ENEMY AI] Mevcut ordumuz çalındı/taraf değiştirdi! AI kontrolü bıraktı.");
                aktifDusmanOrdusu = null; 
            }
        }

        // --- 1. SPAWN (ÜRETİM) İŞLEMLERİ ---
        
        // 1.a) Keşif Birliği Üretimi
        if (aktifKesifBirli == null && mapCtrl.dusmanKesifPrefab != null)
        {
            Vector3 spawnPos = MusaitSpawnNoktasiBul(mapCtrl);
            if(spawnPos != Vector3.zero)
            {
                aktifKesifBirli = Instantiate(mapCtrl.dusmanKesifPrefab, spawnPos, Quaternion.identity);
                aktifKesifBirli.name = "DusmanKesif_Birligi";
                Debug.Log("[ENEMY AI] Düşman Başkenti çevresinde yeni bir Keşif Birliği eğitildi.");
            }
        }

        // 1.b) Düşman Ordusu Üretimi
        if (aktifDusmanOrdusu == null && mapCtrl.piyadePrefab != null)
        {
            Vector3 spawnPos = MusaitSpawnNoktasiBul(mapCtrl);
            if (spawnPos != Vector3.zero)
            {
                aktifDusmanOrdusu = Instantiate(mapCtrl.piyadePrefab, spawnPos, Quaternion.identity);
                aktifDusmanOrdusu.name = "Dusman_Ordusu";
                
                // YENİ: Oyuncu kendi ordusuyla karıştırmasın diye kırmızı renk veriyoruz
                SpriteRenderer sr = aktifDusmanOrdusu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.4f, 0.4f, 1f); 
                
                ArmyStats stats = aktifDusmanOrdusu.GetComponent<ArmyStats>();
                if (stats != null)
                {
                    stats.dusmanMi = true; // Düşman ordusu olduğunu işaretle
                    stats.icindekiBirlikler.Clear();
                    stats.icindekiBirlikler.Add("Piyade");
                    stats.icindekiBirlikler.Add("Piyade");
                    stats.icindekiBirlikler.Add("Piyade");
                    stats.mevcutCan = 3;
                    stats.maxCan = 3;
                    stats.hasarGucu = 3;
                    stats.CanYazisiniGuncelle();
                }
                Debug.Log("[ENEMY AI] Düşman Başkenti çevresinde yepyeni bir ORDU eğitildi!");
            }
        }

        // --- 2. HAREKET VE SALDIRI İŞLEMLERİ ---
        
        if (aktifKesifBirli != null)
        {
            YapayZekaYurutVeSaldir(aktifKesifBirli, ref kesifHedefi, mapCtrl, false);
        }

        if (aktifDusmanOrdusu != null)
        {
            YapayZekaYurutVeSaldir(aktifDusmanOrdusu, ref orduHedefi, mapCtrl, true);
        }
        
        intikalPuani--;
    }

    private Vector3 MusaitSpawnNoktasiBul(MapController mapCtrl)
    {
        Vector3Int hqHex = mapCtrl.hexTilemap.WorldToCell(dusmanBaskenti.transform.position);
        List<Vector3Int> hqNeighbors = mapCtrl.GetHexKomsular(hqHex);
        
        foreach(var hex in hqNeighbors) 
        {
            if (mapCtrl.hexTilemap.HasTile(hex) && mapCtrl.hexTilemap.GetTile(hex) == mapCtrl.cimenTile)
            {
                Vector3 wPos = mapCtrl.hexTilemap.GetCellCenterWorld(hex);
                Collider2D[] col = Physics2D.OverlapCircleAll(wPos, 0.5f);
                if (col.Length == 0) {
                    return wPos;
                }
            }
        }
        return Vector3.zero;
    }

    private void YapayZekaYurutVeSaldir(GameObject birim, ref Vector3Int? hedefRotasi, MapController mapCtrl, bool isOrdu)
    {
        ArmyStats stats = birim.GetComponent<ArmyStats>();
        if (stats != null)
        {
            if (stats.buTurHareketEttiMi)
            {
                Debug.Log($"[ENEMY AI] {birim.name} bu tur dondurulmuş veya tedariki kesilmiş! Hareket edemiyor.");
                stats.buTurHareketEttiMi = false; // Bir sonraki tur için kilidi aç
                return;
            }
        }

        // Radar Taraması (Yaklaşık 4.5 birim yarıçap, yani etrafındaki 4-5 hex)
        GameObject hedefObj = null;
        float minMesafe = 9999f;
        
        Collider2D[] gorusMenzili = Physics2D.OverlapCircleAll(birim.transform.position, 4.5f);
        foreach (Collider2D col in gorusMenzili)
        {
            if (col.gameObject == birim) continue; // Kendini hedef alma
            if (col.gameObject.name.Contains("Dusman")) continue; // Kendi arkadaşlarına veya kalesine saldırma!
            
            bool hedeflenebilir = false;
            
            if (!isOrdu) 
            {
                // 1) KEŞİF BİRLİĞİ SADECE KÖYLERE, HAYDUTLARA VE DİĞER KEŞİFLERE DALAR
                if (col.CompareTag("Haydut")) hedeflenebilir = true;
                if (col.CompareTag("Koy")) hedeflenebilir = true;
                if (col.CompareTag("Unit")) 
                {
                    ArmyStats targetStats = col.GetComponent<ArmyStats>();
                    if (targetStats != null && !targetStats.dusmanMi) 
                    {
                        if (targetStats.icindekiBirlikler.Contains("Kesif") || targetStats.icindekiBirlikler.Contains("Gocmen")) 
                            hedeflenebilir = true;
                    }
                }
            }
            else 
            {
                // 2) ORDU SADECE DİĞER ORDULARA VE KALELERE DALAR
                if (col.CompareTag("Kale")) 
                {
                    if (!col.gameObject.name.Contains("Dusman")) hedeflenebilir = true;
                }
                if (col.CompareTag("Unit")) 
                {
                    ArmyStats targetStats = col.GetComponent<ArmyStats>();
                    if (targetStats != null && !targetStats.dusmanMi) 
                    {
                        if (!targetStats.icindekiBirlikler.Contains("Kesif") && !targetStats.icindekiBirlikler.Contains("Gocmen")) 
                            hedeflenebilir = true;
                    }
                }
            }
            
            if (hedeflenebilir)
            {
                float d = Vector2.Distance(birim.transform.position, col.transform.position);
                if (d < minMesafe) {
                    minMesafe = d;
                    hedefObj = col.gameObject;
                }
            }
        }

        int adimSayisi = isOrdu ? 1 : 2; // Ordu 1 adım atar, keşif 2 adım atar
        if (stats != null && stats.hareketMenzili > 0) adimSayisi = stats.hareketMenzili;

        for (int adim = 0; adim < adimSayisi; adim++)
        {
            if (birim == null) break; // Eğer ilk adımda ölürse devam etme
            
            Vector3Int currentHex = mapCtrl.hexTilemap.WorldToCell(birim.transform.position);

            Vector3 hedeflenenNokta = Vector3.zero;
            bool moved = false;

            if (hedefObj != null)
            {
                hedefRotasi = null; // Savaş/hedef bulunduğu an keşif rotasını hafızadan sil

                // Göle veya engele takılmamak için BFS Yol Bulma algoritmasını kullan
                Vector3Int targetHex = mapCtrl.hexTilemap.WorldToCell(hedefObj.transform.position);
                Dictionary<Vector3Int, List<Vector3Int>> yollar = mapCtrl.BFSYolBul(currentHex, 20); // 20 hex uzağa kadar tarar

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
                    birim.transform.position = mapCtrl.hexTilemap.GetCellCenterWorld(bestHex);
                    moved = true;
                }
            }
            else
            {
                // Hedef yoksa rastgele uzak bir yere devriye gez
                if (hedefRotasi == null || Vector2.Distance(birim.transform.position, mapCtrl.hexTilemap.GetCellCenterWorld(hedefRotasi.Value)) < 2f)
                {
                    BoundsInt bounds = mapCtrl.hexTilemap.cellBounds;
                    List<Vector3Int> uzakKaralar = new List<Vector3Int>();
                    foreach (var pos in bounds.allPositionsWithin)
                    {
                        if (mapCtrl.hexTilemap.HasTile(pos) && mapCtrl.hexTilemap.GetTile(pos) == mapCtrl.cimenTile)
                        {
                            if (Vector2.Distance(mapCtrl.hexTilemap.GetCellCenterWorld(pos), birim.transform.position) > 12f)
                            {
                                uzakKaralar.Add(pos);
                            }
                        }
                    }
                    
                    if (uzakKaralar.Count > 0)
                    {
                        hedefRotasi = uzakKaralar[Random.Range(0, uzakKaralar.Count)];
                        Debug.Log($"[ENEMY AI] {birim.name} için yeni devriye rotası belirlendi!");
                    }
                }

                if (hedefRotasi != null)
                {
                    Vector3Int targetHex = hedefRotasi.Value;
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
                        birim.transform.position = mapCtrl.hexTilemap.GetCellCenterWorld(bestHex);
                        moved = true;
                    }
                    else
                    {
                        hedefRotasi = null; // Tıkanırsa iptal et
                    }
                }
            }

            if (!moved) 
            {
                Debug.Log($"[ENEMY AI] {birim.name} yürüyecek tile bulamadı.");
                break; 
            }

            // EĞER hedefimiz varsa ve yanına kadar ulaştıysak saldır!
            if (hedefObj != null)
            {
                float currentDist = Vector2.Distance(birim.transform.position, hedefObj.transform.position);
                if (currentDist <= 1.5f) // Yanındaki hexte ise
                {
                    if (hedefObj.CompareTag("Haydut") && hedefObj.GetComponent<HaydutKampi>() != null)
                    {
                        // 1- ARKA PLAN SAVAŞI (Vahşi Haydutlara Karşı)
                        int dusmanGucu = Random.Range(1, 21) + (isOrdu ? 5 : 2); 
                        int haydutZirh = hedefObj.GetComponent<HaydutKampi>().zirhDegeri;
                        
                        if (dusmanGucu >= haydutZirh) {
                            hedefObj.GetComponent<HaydutKampi>().YokOl();
                            Debug.Log($"[ENEMY AI] {birim.name} haydutları temizledi!");
                            break; 
                        } else {
                            if (!isOrdu) { // Keşif ölür, ordu 1 can kaybeder
                                Destroy(birim);
                                Debug.Log($"[ENEMY AI] {birim.name} haydutlar tarafından katledildi!");
                            } else {
                                stats.mevcutCan--;
                                stats.CanYazisiniGuncelle();
                                if(stats.mevcutCan <= 0) Destroy(birim);
                            }
                            break;
                        }
                    }
                    else
                    {
                        // 2- OYUNCU İLE SAVAŞ (Köy, Kale veya Birlik)
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
                            Debug.Log($"[ENEMY AI] {birim.name}, savunmasız Göçmen'i katletti!");
                        }
                        else
                        {
                            if (!isOrdu)
                            {
                                // KEŞİF BİRLİĞİ İSE: MAKRO SAVAŞ (ZAR ATMA)
                                int savunanZirh = 10; 
                                if (hedefObj.GetComponent<MakroKoy>() != null) savunanZirh = hedefObj.GetComponent<MakroKoy>().zirhDegeri;
                                else if (hedefObj.CompareTag("Unit")) savunanZirh = 13; 
                                
                                if (MakroSavasManager.Instance != null) {
                                    MakroSavasManager.Instance.SavasiBaslat(birim, hedefObj, savunanZirh, true);
                                    Debug.Log($"[ENEMY AI] {birim.name} OYUNCUYA SALDIRDI!");
                                }
                            }
                            else
                            {
                                // ORDU İSE: MİKRO SAVAŞ (BATTLE SCENE)
                                if (SavasHafizasi.Instance != null) 
                                {
                                    SavasHafizasi.Instance.savasaGirecekOrdu.Clear();
                                    
                                    // Savunmada oyuncunun birlikleri olacak.
                                    // Eğer hedef orduysa, oyuncu ordusunu alalım
                                    ArmyStats oyuncuOrdusu = hedefObj.GetComponent<ArmyStats>();
                                    if (oyuncuOrdusu != null)
                                    {
                                        SavasHafizasi.Instance.savasaGirecekOrdu.AddRange(oyuncuOrdusu.icindekiBirlikler);
                                        SavasHafizasi.Instance.savasanBizimOrdu = hedefObj;
                                    }
                                    else if (hedefObj.CompareTag("Kale") || hedefObj.GetComponent<MakroKale>() != null)
                                    {
                                        // Kaleye saldırıysa
                                        SavasHafizasi.Instance.savasanBizimOrdu = hedefObj;
                                    }
                                    
                                    SavasHafizasi.Instance.SavasiBaslat(birim, ""); 
                                    Debug.Log($"[ENEMY AI] {birim.name} OYUNCUYA SALDIRDI (MİKRO SAVAŞ)!");
                                }
                            }
                        }
                        
                        break; 
                    }
                }
            }
        }
    }
}
