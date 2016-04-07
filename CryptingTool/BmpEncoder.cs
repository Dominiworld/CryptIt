using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Text;

namespace CryptingTool
{
    public static class BmpEncoder
    {
        
        private static void BmpWrite(string message, Bitmap image)
        {
            // Пиксель изображения
            Color pixel;
            int x = 0;
            // Читаем сообщение*
            byte[] B = Encoding.GetEncoding(1251).GetBytes(message + '$');
            bool f = false;
            // Проходим по изображению
            for (int i = 0; i < image.Width; i++)
            {
                if (f) break;
                for (int j = 0; j < image.Height; j++)
                {
                    // Берем пиксель
                    pixel = image.GetPixel(i, j);
                    // Если зашифровали все сообщение, выходим
                    if (x == B.Length) { f = true; break; }
                    // Представляем байт сообщения в виде массива бит (см. выше пример 11001100)
                    Bits m = new Bits(B[x++]);
                    // Дополняем до 8 бит
                    while (m.Length != 8) m.Insert(0, 0);
                    // Берем каждый цвет RGB и если нужно, дополняем до 8 бит
                    Bits r = new Bits(pixel.R); while (r.Length != 8) r.Insert(0, 0);
                    Bits g = new Bits(pixel.G); while (g.Length != 8) g.Insert(0, 0);
                    Bits b = new Bits(pixel.B); while (b.Length != 8) b.Insert(0, 0);

                    // Заменяем соответствующие младшие биты битами нашего сообщения
                    r[6] = m[0];
                    r[7] = m[1];

                    g[5] = m[2];
                    g[6] = m[3];
                    g[7] = m[4];

                    b[5] = m[5];
                    b[6] = m[6];
                    b[7] = m[7];

                    // Записываем пиксель обратно в изображение
                    image.SetPixel(i, j, Color.FromArgb(r.Number, g.Number, b.Number));
                }
            }
        }
       
        private static string BmpRead(Bitmap image)
        {
            // Пиксель изображения
            Color pixel;
            // Байты считываемого сообщения
            ArrayList array = new ArrayList();
            bool f = false;
            // Проходим по изображению
            for (int i = 0; i < image.Width; i++)
            {
                if (f) break;
                for (int j = 0; j < image.Height; j++)
                {
                    // Берем пиксель
                    pixel = image.GetPixel(i, j);
                    // Текущий считываемый байт
                    Bits m = new Bits(255);
                    // Берем каждый цвет RGB и если нужно, дополняем до 8 бит
                    Bits r = new Bits(pixel.R); while (r.Length != 8) r.Insert(0, 0);
                    Bits g = new Bits(pixel.G); while (g.Length != 8) g.Insert(0, 0);
                    Bits b = new Bits(pixel.B); while (b.Length != 8) b.Insert(0, 0);
                    // Читаем младшие биты
                    m[0] = r[6];
                    m[1] = r[7];

                    m[2] = g[5];
                    m[3] = g[6];
                    m[4] = g[7];

                    m[5] = b[5];
                    m[6] = b[6];
                    m[7] = b[7];

                    // Если встретили наш спецсимвол, то достигли конца сообщения, выходим
                    if (m.Char == '$') { f = true; break; }
                    // Считываемый байт переводим в число
                    array.Add(m.Number);
                }
            }
            byte[] msg = new byte[array.Count];

            // Переводим сообщение в байты, т.к. мы получили сообщение в числовом представлении байта
            for (int i = 0; i < array.Count; i++)
                msg[i] = Convert.ToByte(array[i]);

            // А вот и наше сообщение
            string message = Encoding.GetEncoding(1251).GetString(msg);
            return message;
        }

        public static Bitmap EncryptPicture(string path, string message,string key)
        {

                Bitmap image = (Bitmap) Image.FromFile(path);
                //Берем bmp картинку и записываем в нее наше сообщение, предварительно зашифровав его
            //-------------------------------------------------------------
                //image.Save(@"test.jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
                //Bitmap image2 = (Bitmap)Image.FromFile("test.jpeg");
            //----------------------------------------------------------
                message = SignAndData.Encrypt(message, key);
                BmpWrite(message, image);
                //BmpWrite(message, image2);
                //image2.Save(@"test.jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
              
                //Bitmap imageRes = (Bitmap)Image.FromFile("test.jpeg");

                //image2.Dispose();
               // image.Dispose(); 

                //File.Delete("test.bmp");
                // return imageRes;
                return image;
            
        }
        public static string DecryptPicture(Bitmap image, string key)
        {
                //image.Save(@"test.bmp",System.Drawing.Imaging.ImageFormat.Bmp);
                //Bitmap image2 = (Bitmap)Image.FromFile("test.bmp");
                //string result = SignAndData.Decrypt(BmpRead(image2), key);
                string result = SignAndData.Decrypt(BmpRead(image), key);
                image.Dispose();
                //File.Delete(@"test.bmp");
                return result;
        }
    }
}
