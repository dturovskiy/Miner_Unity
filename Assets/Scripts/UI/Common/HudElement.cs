using DG.Tweening;
//using AwesomeTools.SoundAndFX;
using UnityEngine;

namespace AwesomeTools.UI
{
    public abstract class HudElement : MonoBehaviour
    {   
        [Header("HudElement")]
        [SerializeField] private float _appearDuration;
        [SerializeField] private RectTransform _appearPosition;
        //[SerializeField] protected SoundSystem _soundSystem;
        private RectTransform _rectTransform;

        protected Vector3 _cachedPosition;

        private bool _alreadyConfigured = false;

        private void Awake()
        {
            if(_alreadyConfigured == false)
            {
                _alreadyConfigured = true;
                _rectTransform = GetComponent<RectTransform>();
                _cachedPosition = _rectTransform.anchoredPosition;
            }
        }

        protected void Start()
        {
            if(_alreadyConfigured == false)
            {
                _alreadyConfigured = true;
                _rectTransform = GetComponent<RectTransform>();
                _cachedPosition = _rectTransform.anchoredPosition;
            }
        }
        public void Appear()
        {
            _rectTransform.DOAnchorPos(_appearPosition.anchoredPosition, _appearDuration).SetEase(Ease.OutBack);
        }

        public void Disappear()
        {
            _rectTransform.DOAnchorPos(_cachedPosition, _appearDuration).SetEase(Ease.InBack);
        }

        public void Click()
        {
            //if (_soundSystem != null)
            //    _soundSystem.PlaySound("buttonUIClick");
            //else
            //    Debug.LogError("SoundSystem is null");

            Vector3 cachedScale = _rectTransform.localScale;
            _rectTransform.DOScale(_rectTransform.localScale * 0.8f, 0.1f)
                .OnComplete(() =>
                {
                    _rectTransform.DOScale(cachedScale, 0.1f);
                });
        }
    }
}