using UnityEngine;
using TMPro; 

public class BattleUnit : MonoBehaviour
{
    [Header("Birim Kimliği")]
    public UnitData veri; 
    public bool oyuncununBirimiMi;

    [Header("Anlık Durum")]
    public int mevcutCan;
    public int gridX;
    public int gridY;

    public bool saldirdiMi = false;
    public bool yuruduMu = false;

    [Header("Yetenek Durumu")]
    public int yetenekCooldown = 0; // Yetenek kullandıktan sonra bekleyeceği tur sayısı
    public bool savunmaPozisyonuAktif = false;
    public bool vurKacAktif = false;
    public int sabitlenmeSuresi = 0; // Ağ fırlatma mekaniği için eklendi

    [Header("Hareket Animasyonu")]
    public float hareketHizi = 5f;
    private System.Collections.Generic.List<Vector2> rotam = new System.Collections.Generic.List<Vector2>();
    private bool yoldaMi = false;

    [Header("Arayüz (UI)")]
    public TMP_Text canYazisi;

    public void Setup(UnitData baslangicVerisi, int x, int y, bool bizdenMi)
    {
        // canYazisi Inspector'dan atanmamışsa, çocuk objelerden bul veya sıfırdan oluştur
        if (canYazisi == null)
        {
            canYazisi = GetComponentInChildren<TMP_Text>(true);
        }
        if (canYazisi == null)
        {
            // Prefab'da çalışan bir TMP_Text yok, sıfırdan oluştur
            GameObject canvasObj = new GameObject("OtoCanvas");
            canvasObj.transform.SetParent(transform);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.localPosition = new Vector3(0f, 0.15f, 0f);
            canvasRT.localRotation = Quaternion.identity;
            canvasRT.localScale = new Vector3(0.004f, 0.004f, 1f);
            canvasRT.sizeDelta = new Vector2(200f, 50f);
            
            GameObject textObj = new GameObject("CanYazisi");
            textObj.transform.SetParent(canvasObj.transform);
            
            canYazisi = textObj.AddComponent<TextMeshProUGUI>();
            canYazisi.text = "";
            canYazisi.fontSize = 36;
            canYazisi.color = Color.white;
            canYazisi.alignment = TextAlignmentOptions.Center;
            canYazisi.fontStyle = FontStyles.Bold;
            
            // Arkaplan gölgesi için outline ekle (okunabilirlik)
            canYazisi.outlineWidth = 0.3f;
            canYazisi.outlineColor = Color.black;
            
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.localPosition = Vector3.zero;
            textRT.localRotation = Quaternion.identity;
            textRT.localScale = Vector3.one;
            textRT.sizeDelta = new Vector2(200f, 50f);
            
            Debug.Log($"[BattleUnit] canYazisi sıfırdan oluşturuldu: {gameObject.name}");
        }
        
        veri = baslangicVerisi;
        mevcutCan = veri.maxCan;
        gridX = x;
        gridY = y;
        oyuncununBirimiMi = bizdenMi;
        
        if (veri.birimGorseli != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            
            // Eğer özel bir prefab atanmışsa, onun üstündeki görseli ezme! Sadece jenerik prefabı ez.
            if (veri.birimPrefab == null)
            {
                sr.sprite = veri.birimGorseli;
            }
            
            // YENİ: Binaların (Kale Kapısı gibi) veya özel tasarlanmış yapıların yönünü (flip) zorla değiştirme
            if (!veri.isBina)
            {
                if (!bizdenMi) sr.flipX = true;
                else sr.flipX = false;
            }
        }

        // YENİ: Askerlerin her zaman ağaçların ve tepelerin (Tile'ların) ÜSTÜNDE görünmesini sağla
        SpriteRenderer asilRenderer = GetComponent<SpriteRenderer>();
        if (asilRenderer != null)
        {
            asilRenderer.sortingOrder = 10; // Zemin genelde 0 veya 1'dir. Askerler 10 katmanında en önde durur.
        }

        // YENİ: Asker doğar doğmaz can yazısını güncelle
        CaniGuncelle();
        
        if (veri != null && veri.birimAdi == "Kale Kapısı")
        {
            SetAdjacentTilesObstacle(true);
        }
    }

    public void CaniGuncelle()
    {
        if (canYazisi != null)
        {
            canYazisi.text = $"{mevcutCan}/{veri.maxCan}";
        }
    }

    public void OlumKontrolu()
    {
        if (mevcutCan <= 0)
        {
            Debug.Log($"{veri.birimAdi} isimli birlik öldü!");
            
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.BirimOldu(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void Update()
    {
        if (yoldaMi && rotam.Count > 0)
        {
            Vector2 siradakiHedef = rotam[0];
            transform.position = Vector2.MoveTowards(transform.position, siradakiHedef, hareketHizi * Time.deltaTime);

            if (Vector2.Distance(transform.position, siradakiHedef) < 0.01f)
            {
                transform.position = siradakiHedef; 
                rotam.RemoveAt(0);

                if (rotam.Count == 0) 
                {
                    yoldaMi = false; 
                }
            }
        }
    }

    public void RotayiBaslat(System.Collections.Generic.List<Vector2Int> adimlar, float tileBoyutu)
    {
        rotam.Clear();
        foreach (var adim in adimlar)
        {
            rotam.Add(new Vector2(adim.x * tileBoyutu, adim.y * tileBoyutu));
        }
        yoldaMi = true;
    }

    public bool HareketEdiyorMu()
    {
        return yoldaMi;
    }

    private void SetAdjacentTilesObstacle(bool isObstacle)
    {
        if (BattleManager.Instance == null || BattleManager.Instance.grid == null) return;
        
        int targetYPlus = gridY + 1;
        int targetYMinus = gridY - 1;
        
        // Sınır kontrolleriyle + ve - y koordinatındaki tile'ları engel yap/kaldır
        if (targetYPlus < BattleManager.Instance.yukseklik)
        {
            var tile = BattleManager.Instance.grid[gridX, targetYPlus];
            if (tile != null) tile.geciciEngel = isObstacle;
        }
        
        if (targetYMinus >= 0)
        {
            var tile = BattleManager.Instance.grid[gridX, targetYMinus];
            if (tile != null) tile.geciciEngel = isObstacle;
        }
    }

    private void OnDestroy()
    {
        if (veri != null && veri.birimAdi == "Kale Kapısı")
        {
            SetAdjacentTilesObstacle(false);
        }
    }
}