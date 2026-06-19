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

    [Header("Hareket Animasyonu")]
    public float hareketHizi = 5f;
    private System.Collections.Generic.List<Vector2> rotam = new System.Collections.Generic.List<Vector2>();
    private bool yoldaMi = false;

    [Header("Arayüz (UI)")]
    public TMP_Text canYazisi;

    public void Setup(UnitData baslangicVerisi, int x, int y, bool bizdenMi)
    {
        veri = baslangicVerisi;
        mevcutCan = veri.maxCan;
        gridX = x;
        gridY = y;
        oyuncununBirimiMi = bizdenMi;
        
        if (veri.birimGorseli != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = veri.birimGorseli;
            
            // YENİ: Düşman askerlerinin yüzünü otomatik olarak oyuncuya (sola) çevir
            // Eğer senin orijinal resimlerin zaten sola bakıyorsa burayı tam tersi yapabilirsin
            if (!bizdenMi) sr.flipX = true;
            else sr.flipX = false;
        }

        // YENİ: Asker doğar doğmaz can yazısını güncelle
        CaniGuncelle();
    }

    public void CaniGuncelle()
    {
        if (canYazisi != null)
        {
            canYazisi.text = $"{mevcutCan}/{veri.maxCan}";
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
}