using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Класс, что отвечает за скрытие экрана
/// </summary>
[RequireComponent(typeof(Image))]
public class FadeScreenPanel : MonoBehaviour
{
    [SerializeField] private float _duration;

    private Image _fadeScreen;
    public event Action FadingIn;

    /// <summary>
    /// Получает компонент изображения и инициирует затухание
    /// </summary>
    private void Awake()
    {
        _fadeScreen = GetComponent<Image>();
        _fadeScreen.enabled = true;
        FadeOut();
    }

    /// <summary>
    /// Погашает экран 
    /// </summary>
    /// <returns>возвращает анимацию исчезновения</returns>
    public Tween FadeOut()
    {
        _fadeScreen.DOFade(1f, 0f);
        return _fadeScreen.DOFade(0f, _duration * 1.5f).SetEase(Ease.InOutQuad).SetLink(_fadeScreen.gameObject).OnComplete(() => _fadeScreen.enabled = false);
    }

    public Tween FadeOutCustomizable(float value)
    {
        FadingIn?.Invoke();
        _fadeScreen.enabled = true;
        return _fadeScreen.DOFade(0f, _duration * 1.5f).SetEase(Ease.InOutQuad).SetLink(_fadeScreen.gameObject).OnComplete(() => _fadeScreen.enabled = false);
    }

    /// <summary>
    /// Скрыть на экране
    /// </summary>
    /// <returns>возвращает анимацию исчезновения</returns>
    public Tween FadeIn()
    {
        FadingIn?.Invoke();
        _fadeScreen.enabled = true;
        return _fadeScreen.DOFade(1f, _duration).SetLink(_fadeScreen.gameObject);
    }

    public Tween FadeInCustomizable(float value)
    {
        FadingIn?.Invoke();
        _fadeScreen.enabled = true;
        return _fadeScreen.DOFade(value, _duration).SetLink(_fadeScreen.gameObject);
    }

    private void StartFadeIn()
        => FadeIn();
}
