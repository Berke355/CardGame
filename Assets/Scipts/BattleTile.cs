using UnityEngine;

public class BattleTile : MonoBehaviour
{
    public int x;
    public int y;
    
    public enum ZeminTipi { Duz, Kaya, Orman, Tepe, YananOrman }
    [Header("Zemin Ayarları")]
    public ZeminTipi zeminTuru = ZeminTipi.Duz; 
    
    // YENİ: Kayanın üzerinden geçilemez, diğerlerinin üzerinden geçilebilir
    public bool YurunebilirMi => zeminTuru != ZeminTipi.Kaya;
    
    // ESKİ engelMi özelliğini şimdilik geriye dönük uyumluluk için siliyoruz, BattleManager'da güncelleyeceğiz.
    
    private Color orijinalRenk; 

    public void Setup(int gridX, int gridY)
    {
        x = gridX;
        y = gridY;
        gameObject.name = $"Tile_{x}_{y}"; 
        
        orijinalRenk = GetComponent<SpriteRenderer>().color; 
    }

    public void RengiSifirla()
    {
        GetComponent<SpriteRenderer>().color = orijinalRenk; 
    }
}