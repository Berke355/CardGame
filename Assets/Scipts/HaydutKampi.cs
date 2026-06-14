using UnityEngine;

public class HaydutKampi : MonoBehaviour
{
    [Header("Kamp Özellikleri")]
    public int zirhDegeri = 12; // Zarda geçilmesi gereken hedef değer

    public void YokOl()
    {
        Debug.Log("Haydut Kampı yok edildi! Ganimet kazanıldı.");
        
        // Ödül Sistemi: Haydut kampı yok edilince +5 Altın ve +3 Taş ver.
        GameManager.Instance.altin += 5;
        GameManager.Instance.tas += 3;
        
        Destroy(gameObject);
        
        // Sınırlarımızda bir açılma olabilir diye map controller'ı dürt
        MapController mapController = Object.FindAnyObjectByType<MapController>();
        if (mapController != null)
        {
            mapController.Invoke("SinirlariVeSisiGuncelle", 0.1f);
        }
    }
}
