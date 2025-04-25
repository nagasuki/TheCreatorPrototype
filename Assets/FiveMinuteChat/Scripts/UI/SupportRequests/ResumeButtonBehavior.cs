using System;
using FiveMinuteChat;
using FiveMinutes.Model.Messages.Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ResumeButtonBehavior : MonoBehaviour
{
    private Button _button;
    private SupportConnectionBehavior _connection;

    public TMP_InputField TicketIdField;
    public TMP_Text ErrorMessageField;
    
    public GameObject ResumeSupportTicketView;
    public GameObject SupportTicketConversationView;

    private bool _isWaiting;
    void Awake()
    {
        _connection = GetComponentInParent<SupportConnectionBehavior>();
        _button = GetComponent<Button>();
        _button.onClick.AddListener( () =>
        {
            if(_isWaiting) return;

            ErrorMessageField.text = string.Empty;
            _button.interactable = false;
            _isWaiting = true;
            var subscriptionId = Guid.Empty; 
            subscriptionId = _connection.Subscribe<ServerGetSupportTicketResponse>( response =>
            {
                _connection.Unsubscribe( subscriptionId );
                _isWaiting = false;
                _button.interactable = true;
                if( response.Success )
                {
                    ResumeSupportTicketView.SetActive( false );
                    SupportTicketConversationView.SetActive( true );
                }
                else
                {
                    ErrorMessageField.text = response.FailureReason ?? "Unknown error";
                }
            } );
            _connection.ResumeSupportTicket( TicketIdField.text );
        } );
    }
}
