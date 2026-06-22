using UnityEngine;

public class BattleTile : MonoBehaviour
{
    public int x;
    public int y;
    
    public enum ZeminTipi { Duz, Kaya, Orman, Tepe, YananOrman }
    [Header("Zemin Ayarları")]
    public ZeminTipi zeminTuru = ZeminTipi.Duz; 
    
    // YENİ: Kayanın üzerinden geçilemez, diğerlerinin üzerinden geçilebilir
    public bool YurunebilirMi => zeminTuru != ZeminTipi.Kaya && !geciciEngel;
    
    // Kale kapısı vb. tarafından kapatılabilen dinamik engel
    [HideInInspector] public bool geciciEngel = false;
    
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