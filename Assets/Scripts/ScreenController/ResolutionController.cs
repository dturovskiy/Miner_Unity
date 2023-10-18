using UnityEngine;
using UnityEngine.UI;

public class ResolutionController : MonoBehaviour
{
    public Button[] buttons; // Масив кнопок, які потрібно розмістити
    public RectTransform[] appearPositions; // Масив пустих об'єктів, які визначають позиції на широкому екрані
    public RectTransform safeAreaPanel; // Посилання на панель, яка відображає SafeArea

    private int screenWidth;
    private int screenHeight;

    private void Start()
    {
        // Отримуємо розміри SafeArea при старті сцени
        SetSafeAreaPanelSize(Screen.safeArea);

        // Викликаємо метод для налаштування положення кнопок і appearPosition при старті сцени
        SetButtonPositions();
    }

    private void Update()
    {
        // Перевіряємо, чи змінилася роздільна здатність екрану
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            // Оновлюємо роздільну здатність
            screenWidth = Screen.width;
            screenHeight = Screen.height;

            // Перевіряємо та оновлюємо положення кнопок відповідно до роздільної здатності
            SetButtonPositions();
        }
    }

    private void SetSafeAreaPanelSize(Rect safeArea)
    {
        // Встановлюємо розміри safeAreaPanel відповідно до SafeArea
        safeAreaPanel.anchoredPosition = safeArea.position;
        safeAreaPanel.sizeDelta = safeArea.size;

        // Встановлюємо масштаб safeAreaPanel відповідно до SafeArea
        float scaleX = safeArea.size.x / Screen.width;
        float scaleY = safeArea.size.y / Screen.height;
        safeAreaPanel.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void SetButtonPositions()
    {
        // Визначаємо розміри SafeArea при кожному виклику методу
        Rect safeArea = Screen.safeArea;

        // Знаходимо координати точки середини SafeArea
        Vector2 safeAreaCenter = new Vector2(safeArea.x + safeArea.width * 0.5f, safeArea.y + safeArea.height * 0.5f);

        // Перевіряємо, чи співвідношення сторін більше 2,2
        float screenAspectRatio = (float)screenWidth / screenHeight;
        bool isWideScreen = screenAspectRatio > 2.2f;

        foreach (Button button in buttons)
        {
            // Отримуємо індекс кнопки у масиві
            int buttonIndex = System.Array.IndexOf(buttons, button);

            // Знаходимо відповідну позицію appearPosition для кнопки
            RectTransform buttonAppearPosition = appearPositions.Length > buttonIndex ? appearPositions[buttonIndex] : null;

            if (buttonAppearPosition != null)
            {
                // Якщо співвідношення екрану більше 2,2, зсуваємо кнопку на 290 пікселів праворуч
                if (isWideScreen)
                {
                    buttonAppearPosition.anchoredPosition += Vector2.right * 290f;
                }

                // Перевіряємо, чи ця кнопка знаходиться в межах SafeArea
                if (buttonAppearPosition.anchoredPosition.x < safeAreaCenter.x)
                {
                    // Зсуваємо кнопку праворуч на 85 пікселів від лівого краю SafeArea
                    buttonAppearPosition.anchoredPosition += Vector2.right * 85f;
                }
            }
        }
    }

}
