using UnityEngine;
using UnityEngine.UI;

namespace FiveMinuteChat.UI.SupportRequests
{
    public class BubbleButtonBehavior : MonoBehaviour
    {
        public SupportConnectionBehavior Connection;
        public GameObject ChooseActionView;
        public GameObject SupportTicketConversationView;
        public GameObject NewView;
        public GameObject ResumeView;

        public void Start()
        {
            if( !Connection )
            {
                Connection = GetComponentInParent<SupportConnectionBehavior>();
            }
            Connection.ConnectionAccepted += _ =>
            {
                GetComponent<Button>().interactable = true;
            };
        }

        public void ToggleSupportRequestView()
        {
            if( string.IsNullOrEmpty( Connection.CurrentSupportTicketId ) )
            {
                ChooseActionView.SetActive( !ChooseActionView.activeSelf );
            }
            else
            {
                SupportTicketConversationView.SetActive( !SupportTicketConversationView.activeSelf );
            }   
            NewView.SetActive( false );
            ResumeView.SetActive( false );
        }
    }
}
