using Mirror;
using UnityEngine;

public class CameraMarkerController : NetworkBehaviour
{
    [Header("Marker")]
    public Canvas markerCanvas;

    [SyncVar(hook = nameof(OnMarkerVisibleChanged))]
    public bool isMarkerVisible = true;

    void LateUpdate()
    {
        if (markerCanvas != null && isMarkerVisible && Camera.main != null)
        {
            markerCanvas.transform.forward = Camera.main.transform.forward;
        }
    }

    // เรียกเมื่อค่าถูกเปลี่ยนจากฝั่งเซิร์ฟเวอร์
    void OnMarkerVisibleChanged(bool _, bool newValue)
    {
        markerCanvas.enabled = newValue;
    }

    [Server]
    public void HideMarkerForAll()
    {
        isMarkerVisible = false;
    }
}
