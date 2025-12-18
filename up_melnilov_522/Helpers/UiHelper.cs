using System;
using System.Windows;

namespace up_melnilov_522.Helpers
{
    public static class UiHelper
    {
        public static void ShowInfo(string text, string title = "Информация")
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowWarning(string text, string title = "Предупреждение")
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void ShowError(string text, string title = "Ошибка")
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool AskYesNo(string text, string title = "Подтверждение")
        {
            return MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        public static string BuildExceptionText(string actionText, Exception ex)
        {
            // Комментарий: выводим понятный текст пользователю + реальную ошибку (для отладки на защите)
            return actionText + "\n\nТехническая информация:\n" + ex.Message;
        }
    }
}
