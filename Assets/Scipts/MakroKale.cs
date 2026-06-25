using UnityEngine;

public class MakroKale : MonoBehaviour
{
    // YENİ: Kalenin kuşatmalara karşı dayanıklılığını temsil eden can değerleri
    public int maxKapiCani = 10;
    public int kapiCani = 10;

    // YENİ: Relic ve Geliştirme Sistemi İçin Kale Seviyesi
    public int kaleSeviyesi = 1;
    public int maxSeviye = 2; // Başkent sadece 1 kez seviye atlar

    public void SeviyeAtla()
    {
        if (kaleSeviyesi >= maxSeviye)
        {
            Debug.Log("Başkent zaten maksimum seviyede!");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.altin >= 30 && GameManager.Instance.tas >= 20)
        {
            GameManager.Instance.altin -= 30;
            GameManager.Instance.tas -= 20;
            kaleSeviyesi++;
            Debug.Log($"[SİSTEM] Başkent Seviye {kaleSeviyesi} oldu! Altın ve Taş harcandı.");

            // Relic panelini tetikle
            GameManager.Instance.RelicSeciminiBaslat();
            
            // Paneli de güncelleyelim ki yeni seviye ekranda yansısın
            GameManager.Instance.GuncelleIncelePaneli();
        }
        else
        {
            Debug.Log("Başkenti geliştirmek için yeterli kaynak yok! (Gereken: 30 Altın, 20 Taş)");
        }
    }
    void OnMouseDown()
    {
        // Eski "her yerden saldırma" mekaniğini sildik. 
        // Artık oyuncularımıza sağ tıklamaları gerektiğini hatırlatıyoruz!
        Debug.Log("BİLGİ: Kaleye saldırmak için önce kendi ordunuzu SOL TIK ile seçmeli, ardından bu kaleye SAĞ TIK ile saldırı emri vermelisiniz.");
    }
}