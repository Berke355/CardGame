using UnityEngine;

public class MakroKale : MonoBehaviour
{
    void OnMouseDown()
    {
        // Eski "her yerden saldırma" mekaniğini sildik. 
        // Artık oyuncularımıza sağ tıklamaları gerektiğini hatırlatıyoruz!
        Debug.Log("BİLGİ: Kaleye saldırmak için önce kendi ordunuzu SOL TIK ile seçmeli, ardından bu kaleye SAĞ TIK ile saldırı emri vermelisiniz.");
    }
}