using UnityEngine;

public class BattleTile : MonoBehaviour
{
    public int x;
    public int y;
    public bool engelMi = false; // YENİ: Buraya yürünebilir mi?
    
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