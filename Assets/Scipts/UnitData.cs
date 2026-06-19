using UnityEngine;

[CreateAssetMenu(fileName = "YeniBirim", menuName = "GoT_KartOyunu/Birim Verisi")]
public class UnitData : ScriptableObject
{
    [Header("Görsel ve Prefab")]
    public string birimAdi;
    public Sprite birimGorseli; // (Opsiyonel) UI için veya fallback ikon
    public GameObject birimPrefab; // YENİ: Bu birime özel (Okçu, Süvari vb.) 3D/2D obje kalıbı

    [Header("D&D Savaş İstatistikleri")]
    public int maxCan = 3;
    public int hasar = 1;
    public int zirhDegeri = 12; // AC (Armor Class) - Hasar almamak için zarın geçmesi gereken sayı
    public int isabetDegeri = 3; // Zarına eklenecek olan bonus (+3 gibi)
    
    [Header("Özel Kurallar (Koçbaşı vb.)")]
    public bool isBina = false; // YENİ: Hedef bir bina mı? (Örn: Kale Kapısı)
    public bool askerlereHasarVurabilirMi = true; // Koçbaşı için false yapılacak
    public bool binaHasarCari = false; // Hedef bina ise hasarı x2 veya ekstra vurur
    public int binayaEkstraHasar = 0; // Eğer bina vuruyorsa, normal hasara eklenecek bonus
    
    [Header("Taktiksel Hareket")]
    public int hareketMenzili = 2; // Bir turda kaç kare yürüyebilir
    public int saldiriMenzili = 1; // Kaç kare uzağa saldırabilir
}