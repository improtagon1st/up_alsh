using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace up_melnilov_522.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage GetImageOrStub(string photo)
        {
            var stub = LoadFromPackUri("pack://application:,,,/Resources/picture.png");
            if (stub == null) stub = new BitmapImage();

            try
            {
                if (string.IsNullOrWhiteSpace(photo))
                    return stub;

                // 0) Попытка загрузить как ресурс из папки Resources (внутри проекта)
                // В БД лежит "1.jpg" => пробуем "Resources/1.jpg"
                var fromRes = LoadFromPackUri("pack://application:,,,/Resources/" + photo.Trim());
                if (fromRes != null)
                    return fromRes;

                // 1) абсолютный путь
                if (File.Exists(photo))
                    return LoadFromFile(photo);

                // 2) относительный путь (например "Images/1.jpg")
                string relative = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photo.Replace("/", "\\"));
                if (File.Exists(relative))
                    return LoadFromFile(relative);

                // 3) только имя файла (ищем в папке Images рядом с exe)
                string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", photo);
                if (File.Exists(local))
                    return LoadFromFile(local);

                return stub;
            }
            catch
            {
                return stub;
            }
        }

        private static BitmapImage LoadFromFile(string path)
        {
            // чтобы файл не блокировался
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private static BitmapImage LoadFromPackUri(string packUri)
        {
            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(packUri, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }
}
