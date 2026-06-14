using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Ayarları")]
    public float zoomHizi = 4f;
    public float minZoom = 2f;
    public float maxZoom = 10f; // Haritadan çok uzaklaşmamak için sınır

    [Header("Kaydırma (Pan) Ayarları")]
    public float klavyeKaydirmaHizi = 15f; // WASD veya ok tuşları için
    
    private Camera cam;
    private Vector3 baslangicSuruklemeNoktasi;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Eğer yanlış objeye atarsak otomatik Main Camera'yı bulsun
        if (cam == null) cam = Camera.main; 

        // Başlangıçta çok yakın/uzaksa düzelt
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    void Update()
    {
        if (cam == null) return;

        // --- 1. ZOOM (Farenin Orta Tekerleği) ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomHizi;
            // Kameranın belirlenen min ve max değerlerin dışına çıkmasını engelle
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // --- 2. KLAVYE İLE KAYDIRMA (WASD veya Ok Tuşları) ---
        float yatay = Input.GetAxis("Horizontal"); // A/D veya Sol/Sağ Ok
        float dikey = Input.GetAxis("Vertical");   // W/S veya Yukarı/Aşağı Ok
        
        if (yatay != 0f || dikey != 0f)
        {
            // Kamera yakınken (zoom) daha yavaş, uzakken daha hızlı hareket etsin ki dengeli olsun
            float hizCarpani = cam.orthographicSize / 5f; 
            Vector3 hareket = new Vector3(yatay, dikey, 0) * (klavyeKaydirmaHizi * hizCarpani) * Time.deltaTime;
            transform.Translate(hareket, Space.World);
        }

        // --- 3. FARE İLE KAYDIRMA (Farenin Orta Topuna Basılı Tutarak Sürükleme) ---
        // Orta tuşa (Mouse Button 2) ilk basıldığı an
        if (Input.GetMouseButtonDown(2)) 
        {
            // Farenin ekranda tıkladığı yerin haritadaki gerçek koordinatını hafızaya al
            baslangicSuruklemeNoktasi = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // Orta tuşa basılı tutmaya (sürüklemeye) devam ettikçe
        if (Input.GetMouseButton(2)) 
        {
            Vector3 mevcutNokta = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 fark = baslangicSuruklemeNoktasi - mevcutNokta;
            
            // Kamerayı, fareyi sürüklediğimiz kadar kaydır
            transform.position += fark;
        }
    }
}
