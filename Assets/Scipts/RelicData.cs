using UnityEngine;

public enum RelicTuru { Genel, HanedanOzel }

[CreateAssetMenu(fileName = "YeniRelic", menuName = "GoT_KartOyunu/Relic")]
public class RelicData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string relicAdi;
    [TextArea(3, 5)] public string aciklama;
    public RelicTuru turu;
    
    // Not: Enum'ı tam olarak string eşleşmesi için veya direkt public string ozelHanedan; 
    // olarak kullanabiliriz. "Stark" veya "Lannister" yazmak kolaylık sağlayacaktır.
    [Tooltip("Sadece HanedanOzel ise doldurun. Örn: 'Stark' veya 'Lannister'")]
    public string ozelHanedan = ""; 
    public Sprite relicGorseli;

    [Header("Ortak/Genel Etkiler")]
    public bool apBedeliAzalt = false;
    public bool turBasiEkstraKartCek = false;

    [Header("Stark Hanedanı Etkileri")]
    [Tooltip("Needle: Savaş içi yeteneklerin cooldown'ını 1 azaltır.")]
    public bool yetenekCooldownAzalt = false;
    [Tooltip("Kışyarı Savunması: Kalenin dayanıklılığı artar.")]
    public bool kaleGuclendirme = false;
    [Tooltip("Ghost: Kar biyomlarında orduların intikali artar.")]
    public bool ghostKarBiyomuHizi = false;

    [Header("Lannister Hanedanı Etkileri")]
    [Tooltip("Casterly Rock Hazinesi: Market fiyatları yarı yarıya düşer.")]
    public bool casterlyRockMarketIndirimi = false;
    [Tooltip("Espiyonaj: Haritadaki tüm savaş sisi kalıcı olarak kalkar.")]
    public bool lannisterSavasSisiKalkar = false;
    [Tooltip("Kızıl Süvariler: Lannister Süvarilerine savaşta +2 hareket ve +1 isabet sağlar.")]
    public bool lannisterSuvariBuff = false;
}
