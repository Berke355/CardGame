using UnityEngine;
using TMPro; // İleride Can yazısı eklersek diye

public class MakroKoy : MonoBehaviour
{
    [Header("Köy Özellikleri")]
    public int mevcutCan = 3;
    public int zirhDegeri = 10; // Haydutlar pop-up ekranda köyü basarken bu zırhı zarla geçmek zorunda kalacak

    // Şimdilik sadece hasar alma fonksiyonunu önden hazırlıyoruz
    public void HasarAl(int hasar)
    {
        mevcutCan -= hasar;
        Debug.Log($"Köy saldırıya uğradı! Mevcut canı: {mevcutCan}");

        if (mevcutCan <= 0)
        {
            KoyYikildi();
        }
    }

    private void KoyYikildi()
    {
        Debug.Log("Köy tamamen yıkıldı ve haritadan silindi!");
        
        // Köy haritadan silindiğinde etrafındaki mavi sınırların daralması için MapController'ı uyar
        MapController mapController = Object.FindAnyObjectByType<MapController>();
        Destroy(gameObject); // Önce kendini yokedip sonra taramayı tetikliyoruz
        
        if (mapController != null)
        {
            mapController.Invoke("SinirlariVeSisiGuncelle", 0.1f); // 0.1 saniye sonra sınırlar baştan çizilecek
        }
    }
}
