using System.Collections;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class MinimizeButtonBehavior : MonoBehaviour
{
    private Image _upArrowImage;
    private Image _downArrowImage;
    private RectTransform _selfRect;
    private RectTransform _channelsListRect;
    private RectTransform _channelsContainerRect;
    private float _channelsListMaximizedHeight;
    private Vector2 _channelsListMaximizedPosition;
    private float _channelsContainerMaximizedHeight;
    private bool _isMinimized;
    private bool _isAnimating;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener( () => OnClick() );
        _upArrowImage = transform.Find("UpArrow").GetComponent<Image>();
        _downArrowImage = transform.Find("DownArrow").GetComponent<Image>();
        _selfRect = transform.GetComponent<RectTransform>();
        _channelsListRect = transform.parent.Find( "ChannelList" ).GetComponent<RectTransform>();
        _channelsContainerRect = transform.parent.Find( "ChannelContainer" ).GetComponent<RectTransform>();
    }

    void Start()
    {
        _channelsListMaximizedPosition = _channelsListRect.anchoredPosition;
        _channelsListMaximizedHeight = _channelsListRect.sizeDelta.y;
        _channelsContainerMaximizedHeight = _channelsContainerRect.sizeDelta.y;
    }

    private void OnClick()
    {
        if( _isAnimating )
        {
            return;
        }
        _isAnimating = true;
        
        if( !_isMinimized )
        {
            StartCoroutine( Minimize() );
        }
        else
        {
            StartCoroutine( Maximize() );
        }

        _isMinimized = !_isMinimized;
    }

    private IEnumerator Minimize()
    {
        var containerDist = _channelsContainerMaximizedHeight;
        var timeStep = 0.01f;
        for( float i = 0; i < 1; i+=timeStep )
        {
            _selfRect.anchoredPosition = new Vector2( 0, Mathf.Lerp( _channelsListMaximizedPosition.y, 0, i ) );
            _channelsContainerRect.anchoredPosition = new Vector2( 0, Mathf.Lerp( 0, -containerDist, i ) );
            _channelsListRect.anchoredPosition = new Vector2( _channelsListMaximizedPosition.x, Mathf.Lerp( _channelsListMaximizedPosition.y, -_channelsListMaximizedHeight, i ) );
            yield return null;
        }

        _downArrowImage.gameObject.SetActive( false );
        _upArrowImage.gameObject.SetActive( true );
        _isAnimating = false;
    }

    private IEnumerator Maximize()
    {
        var containerDist = _channelsContainerMaximizedHeight;
        var timeStep = 0.01f;
        for( float i = 0; i < 1; i+=timeStep )
        {
            _selfRect.anchoredPosition = new Vector2( 0, Mathf.Lerp( 0, _channelsListMaximizedPosition.y, i ) );
            _channelsContainerRect.anchoredPosition = new Vector2( 0, Mathf.Lerp( -containerDist, 0, i ) );
            _channelsListRect.anchoredPosition = new Vector2( _channelsListMaximizedPosition.x, Mathf.Lerp( -_channelsListMaximizedHeight, _channelsListMaximizedPosition.y, i ) );
            yield return null;
        }

        _downArrowImage.gameObject.SetActive( true );
        _upArrowImage.gameObject.SetActive( false );        
        _isAnimating = false;
    }

}
