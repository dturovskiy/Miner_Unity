using UnityEngine;
using TMPro;

// Use for load text from .txt file to TextMeshPro component
namespace AwesomeTools.UI
{
    public class PolicyTermsLoader : MonoBehaviour
    {
        public TextAsset _textFilePolicy;
        public TextMeshProUGUI _textMeshProTextPolicy;
        public TextAsset _textFileTerms;
        public TextMeshProUGUI _textMeshProTextTerms;

        private void Awake()
        {
            LoadText();
        }

        private void LoadText()
        {
            if (_textFilePolicy != null && _textMeshProTextPolicy != null)
            {
                _textMeshProTextPolicy.text = _textFilePolicy.text;
            }

            if (_textFilePolicy != null && _textMeshProTextPolicy == null)
            {
                Debug.LogError("PolicyTermsLoader: Policy text target is missing.");
            }

            if (_textFileTerms != null && _textMeshProTextTerms != null)
            {
                _textMeshProTextTerms.text = _textFileTerms.text;
            }

            if (_textFileTerms != null && _textMeshProTextTerms == null)
            {
                Debug.LogError("PolicyTermsLoader: Terms text target is missing.");
            }
        }
    }
}
