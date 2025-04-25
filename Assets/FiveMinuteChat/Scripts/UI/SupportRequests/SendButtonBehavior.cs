using FiveMinuteChat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SendButtonBehavior : MonoBehaviour
{
    private Button _button;
    private SupportConnectionBehavior _connection;

    public TMP_InputField Message;
    public TMP_InputField Topic;
    
    public GameObject NewSupportTicketView;
    public GameObject SupportTicketConversationView;
    void Awake()
    {
        _connection = GetComponentInParent<SupportConnectionBehavior>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener( () =>
        {
            NewSupportTicketView.SetActive( false );
            SupportTicketConversationView.SetActive( true );
            _connection.SendSupportRequest( Topic.text, Message.text );
        } );
        Topic.text = "I don' know what to call this";
        Message.text = "But yano... I have this problem. Like, not sure how... but I'm missing a specific hair on my head!";
    }
}
