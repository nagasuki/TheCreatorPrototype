using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.SupportRequests
{
    public class ToggleViewBehavior : MonoBehaviour
    {
        public GameObject TargetView;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener( ToggleView );
        }

        private void ToggleView()
        {
            TargetView.SetActive( !TargetView.activeSelf );
            GetComponentInParent<Canvas>().gameObject.SetActive(false);
        }
    }
}
