using UnityEngine;

[CreateAssetMenu(fileName = "YeniBirim", menuName = "GoT_KartOyunu/Birim Verisi")]
public class UnitData : ScriptableObject
{
    [Header("Görsel")]
    public string birimAdi;
    public Sprite birimGorseli; // Arenada nasıl görüneceği

    [Header("D&D Savaş İstatistikleri")]
    public int maxCan = 3;
    public int hasar = 1;
    public int zirhDegeri = 12; // AC (Armor Class) - Hasar almamak için zarın geçmesi gereken sayı
    public int isabetDegeri = 3; // Zarına eklenecek olan bonus (+3 gibi)
    
    [Header("Taktiksel Hareket")]
    public int hareketMenzili = 2; // Bir turda kaç kare yürüyebilir
    public int saldiriMenzili = 1; // Kaç kare uzağa saldırabilir
}