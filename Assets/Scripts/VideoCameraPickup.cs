using Mirror;
using UnityEngine;

public class VideoCameraPickup : NetworkBehaviour
{
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 3f;
    public CameraMarkerController markerController;

    private void OnTriggerStay(Collider other)
    {
        if (!NetworkClient.ready) return;

        if (other.CompareTag("Player") && other.TryGetComponent(out VideoRecorder recorder) && recorder.isLocalPlayer)
        {
            float dist = Vector3.Distance(other.transform.position, transform.position);
            if (dist <= pickupRange && Input.GetKeyDown(pickupKey))
            {
                //recorder.PickupCamera(this.gameObject);
                other.GetComponent<PlayerController>().animator.SetLayerWeight(2, 0f);

                // เรียกบน Client → ส่งไป Server → Server sync ให้ทุกคน
                CmdPickupAndHide();
            }
        }
    }

    [Command]
    void CmdPickupAndHide()
    {
        RpcPickupAndHide();
    }

    [ClientRpc]
    void RpcPickupAndHide()
    {
        if (markerController != null)
            markerController.HideMarkerForAll();

        // ถ้าจะลบกล้องออกจากฉาก:
        // NetworkServer.Destroy(gameObject);
        gameObject.SetActive(false); // หรือซ่อนไว้เฉยๆ
    }
}
