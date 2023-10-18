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
            if (_textFilePolicy != null)
            {
                string fileContents = _textFilePolicy.text;
                _textMeshProTextPolicy.text = fileContents;
                Debug.Log("File was successfully uploaded");
            }
            if (_textFileTerms != null)
            {
                string fileContents = _textFileTerms.text;
                _textMeshProTextTerms.text = fileContents;
                Debug.Log("File was successfully uploaded");
            }
            else
            {
                Debug.LogError("File not found: " + "_textFile");
            }
        }
    }
}