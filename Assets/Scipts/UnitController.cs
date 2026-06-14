using UnityEngine;
using System.Collections.Generic; // YENİ EKLENDİ

public class UnitController : MonoBehaviour
{
    // Askerin yürüme hızı (Unity arayüzünden değiştirebilirsin)
    public float hareketHizi = 5f; 
    
    private List<Vector3> rotam = new List<Vector3>();
    private bool hareketEdiyorMu = false;

    void Update()
    {
        // Eğer bir hedef nokta varsa oraya doğru yürü
        if (hareketEdiyorMu && rotam.Count > 0)
        {
            Vector3 siradakiHedef = rotam[0];

            // MoveTowards: Mevcut konumdan, hedefe doğru saniyede 'hareketHizi' kadar ilerle
            transform.position = Vector3.MoveTowards(transform.position, siradakiHedef, hareketHizi * Time.deltaTime);

            // Asker o noktaya tam ulaştıysa noktayı sil ve bir sonrakine geçmeye hazırlan
            if (Vector3.Distance(transform.position, siradakiHedef) < 0.01f)
            {
                transform.position = siradakiHedef; // Tam merkeze oturt
                rotam.RemoveAt(0);
                
                if (rotam.Count == 0) 
                {
                    hareketEdiyorMu = false; // Tüm yol bittiyse dur
                }
            }
        }
    }

    // MapController'ın bu askere rotayı vermek için kullanacağı yeni fonksiyon
    public void YolaCik(List<Vector3> yeniRota)
    {
        rotam = new List<Vector3>(yeniRota);
        hareketEdiyorMu = true;
    }
}