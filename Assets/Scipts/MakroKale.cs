using UnityEngine;

public class MakroKale : MonoBehaviour
{
    // YENİ: Kalenin kuşatmalara karşı dayanıklılığını temsil eden can değerleri
    public int maxKapiCani = 10;
    public int kapiCani = 10;
    void OnMouseDown()
    {
        // Eski "her yerden saldırma" mekaniğini sildik. 
        // Artık oyuncularımıza sağ tıklamaları gerektiğini hatırlatıyoruz!
        Debug.Log("BİLGİ: Kaleye saldırmak için önce kendi ordunuzu SOL TIK ile seçmeli, ardından bu kaleye SAĞ TIK ile saldırı emri vermelisiniz.");
    }
}