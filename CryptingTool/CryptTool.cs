using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptingTool;

namespace CryptingTool
{
   public class CryptTool
    {
        public byte[] keyRSAPublic;
        public byte[] keyRSAPrivate;
        public byte[] keyRSARemote;

        private  CngKey senderKeySignature;
        private  byte[] senderPubKeyBlob;

       public string _isCryptedFlag = "ъйьz";

        public static CryptTool Instance { get; } = new CryptTool();

       protected CryptTool()
       {
           keyRSARemote = null;
           keyRSAPublic = null;
           keyRSAPrivate = null;

           //CreateRSAKey();
           //keyRSARemote = keyRSAPrivate;
       }
        public void CreateRSAKey()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            keyRSAPrivate = rsa.ExportCspBlob(true);
            keyRSAPublic = rsa.ExportCspBlob(false);
        }

        public string SendRSAKey(byte[] keyRSA)
        {
            return Encoding.Default.GetString(keyRSA);
        }
        public void SetRSAKey(string RSAKey)
        {
            keyRSARemote = Encoding.Default.GetBytes(RSAKey);
        }
        private string EncryptString(string inputString)
        {
            int dwKeySize = 2048;
            byte[] xmlString = keyRSARemote;
            RSACryptoServiceProvider rsaCryptoServiceProvider =
                                          new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.ImportCspBlob(xmlString);
            int keySize = dwKeySize / 8;
            byte[] bytes = Encoding.UTF32.GetBytes(inputString);
            
            int maxLength = keySize - 42;
            int dataLength = bytes.Length;
            int iterations = dataLength / maxLength;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i <= iterations; i++)
            {
                byte[] tempBytes = new byte[
                        (dataLength - maxLength * i > maxLength) ? maxLength :
                                                      dataLength - maxLength * i];
                Buffer.BlockCopy(bytes, maxLength * i, tempBytes, 0,
                                  tempBytes.Length);
                byte[] encryptedBytes = rsaCryptoServiceProvider.Encrypt(tempBytes,
                                                                          true);

                Array.Reverse(encryptedBytes);

                stringBuilder.Append(Convert.ToBase64String(encryptedBytes));
            }
            return stringBuilder.ToString();
        }

        private string DecryptString(string inputString)
        {
            int dwKeySize = 2048;
            byte[] xmlString = keyRSAPrivate;
            RSACryptoServiceProvider rsaCryptoServiceProvider
                                     = new RSACryptoServiceProvider(dwKeySize);
            rsaCryptoServiceProvider.ImportCspBlob(xmlString);
            int base64BlockSize = ((dwKeySize / 8) % 3 != 0) ?
              (((dwKeySize / 8) / 3) * 4) + 4 : ((dwKeySize / 8) / 3) * 4;
            int iterations = inputString.Length / base64BlockSize;
            ArrayList arrayList = new ArrayList();
            try
            {
               for (int i = 0; i < iterations; i++)
               {
                   byte[] encryptedBytes = Convert.FromBase64String(
                        inputString.Substring(base64BlockSize * i, base64BlockSize));
   
                   Array.Reverse(encryptedBytes);
                   arrayList.AddRange(rsaCryptoServiceProvider.Decrypt(
                                       encryptedBytes, true));
               }
            }
             catch (Exception)
            {
                return inputString;
            }
            return Encoding.UTF32.GetString(arrayList.ToArray(Type.GetType("System.Byte")) as byte[]);
        }
        private string GetRandomString(int length)
        {
            var result = new char[length];
            var r = new Random();
            for (int i = 0; i < result.Length; i++)
            {
                do
                    result[i] = (char)r.Next(127);
                while (result[i] < '!');
            }
            return new string(result);
        }
       

        #region Работаем с цифровой подписью(Алгоритм ECDsa)
        /*Описание: Далее идет алгоритм создания цифровой подписи с использованием Алгоритма ECDsa.
         Отправитель создает подпись, которая шифруется с помощью его секретного ключа и может быть расшифрована с применением его открытого ключа. 
         Такой подход гарантирует, что подпись действительно принадлежит отправителю.
        Ниже представлены след. Функции:
         1)Создание ключей
         2)Подпись данных
         3)Проверка принадлежности подписи отправителю, 
         за счет применения его открытого ключа.
        */


        private void CreateKeys()
        {
            /* Метод Create(): класса CngKey в качестве аргумента получает алгоритм. С помощью метода Export() 
               из пары ключей экспортируется открытый ключ. Этот открытый ключ может быть предоставлен получателю, 
               чтобы он мог проверять действительность подписи.*/
            senderKeySignature = CngKey.Create(CngAlgorithm.ECDsaP256);
            senderPubKeyBlob = senderKeySignature.Export(CngKeyBlobFormat.GenericPublicBlob);
        }
        private  byte[] CreateSignature(byte[] data, CngKey key)
        {
            /*Имея в распоряжении пару ключей, отправитель может создать подпись с помощью класса ECDsaCng. 
              Конструктор этого класса принимает объект CngKey, в котором содержится открытый и секретный ключи. 
              Далее этот секретный ключ используется для подписания данных вызовом метода SignData()   
             */
            byte[] signature;
            var signingAlg = new ECDsaCng(key);
            signature = signingAlg.SignData(data);
            signingAlg.Clear();
            return signature;
        }
        private  bool VerifySignature(byte[] data, byte[] signature, byte[] pubKey)
        {
            /*Для проверки, действительно ли подпись принадлежит отправителю, получатель извлекает ее с применением полученного 
              от отправителя открытого ключа. Для этого сначала массив байтов, содержащий этот открытый ключ, импортируется 
              в объект CngKey с помощью статического метода Import(), а затем для верификации подписи вызывается метод VerifyData() 
              класса ECDsaCng
            */
            bool retValue = false;
            using (CngKey key = CngKey.Import(pubKey, CngKeyBlobFormat.GenericPublicBlob))
            {
                var signingAlg = new ECDsaCng(key);
                retValue = signingAlg.VerifyData(data, signature);
                signingAlg.Clear();
            }
            return retValue;
        }
        #endregion

        #region Шифруем данные(Алгоритм AES)

        private  string Encrypt(string str, string keyCrypt)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(str), keyCrypt));
        }

        private  byte[] Encrypt(byte[] key, string value)
        {
            SymmetricAlgorithm Sa = Rijndael.Create();
            ICryptoTransform Ct = Sa.CreateEncryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);
            MemoryStream Ms = new MemoryStream();
            CryptoStream Cs = new CryptoStream(Ms, Ct, CryptoStreamMode.Write);
            Cs.Write(key, 0, key.Length);
            Cs.FlushFinalBlock();
            byte[] Result = Ms.ToArray();
            Ms.Close();
            Ms.Dispose();
            Cs.Close();
            Cs.Dispose();
            Ct.Dispose();
            return Result;
        }

        private string Decrypt(byte[] str, string keyCrypt)
        {
            string Result;
            try
            {
                CryptoStream Cs = InternalDecrypt(str, keyCrypt);
                StreamReader Sr = new StreamReader(Cs);
                Result = Sr.ReadToEnd();
                Cs.Close();
                Cs.Dispose();
                Sr.Close();
                Sr.Dispose();
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Содержимое сообщения неизвестно");
                return null;
            }

            return Result;
        }

        private string Decrypt(string str, string keyCrypt)
        {
            string Result;
            try
            {
                CryptoStream Cs = InternalDecrypt(Convert.FromBase64String(str), keyCrypt);
               

                StreamReader Sr = new StreamReader(Cs);
                Result = Sr.ReadToEnd();
                Cs.Close();
                Cs.Dispose();
                Sr.Close();
                Sr.Dispose();
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Содержимое сообщения неизвестно");
                return null;
            }

            return Result;
        }

        private  CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            ICryptoTransform ct = sa.CreateDecryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);
            MemoryStream ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }



        #endregion

        #region Вспомогательные функции распаковки и упаковки сообщений

        /// <summary>
        /// Расшифровка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string SplitAndUnpackReceivedMessage(string message)
        {

            if (string.IsNullOrEmpty(message) || message.Length < _isCryptedFlag.Length)
            {
                return message;
            }
            string des = message.Substring(0, _isCryptedFlag.Length);

            if (des != _isCryptedFlag)
                return message;

            message = message.Substring(_isCryptedFlag.Length);
            message = message.FromBase64();

            byte[] receivedSignature = Encoding.Default.GetBytes(message.Substring(0, 64));
            byte[] receivedPubKey = Encoding.Default.GetBytes(message.Substring(64, 72));
            string encryptedSymmetricKey = message.Substring(136, 344);
            byte[] receivedData = Encoding.Default.GetBytes(message.Substring(480));
            string symmetricKey = DecryptString(encryptedSymmetricKey);

            if (VerifySignature(receivedData, receivedSignature, receivedPubKey))
            {
                // Console.WriteLine("Подпись отправителя была успешно проверена");
                // Console.WriteLine("Данные получены. Содержимое: " + Decrypt(receivedData, symmetricKey));
                return Decrypt(receivedData, symmetricKey);
            }

            return message;
            //else Console.WriteLine("Не шифрованое сообщение:" + message);
        }
        /// <summary>
        /// Шифровка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns>
        /// </returns>
        public string MakingEnvelope(string message)         //TODO Добавить в функцию строковую переменную ключа для шифрования данных
        {
            CreateKeys();

            string symmetricKey = GetRandomString(8);

            string encryptedSymmetricKey=EncryptString(symmetricKey);
            Console.WriteLine(encryptedSymmetricKey.Length);

            byte[] senderData = Encrypt(Encoding.UTF8.GetBytes(message),symmetricKey);

            byte[] senderSignature = CreateSignature(senderData, senderKeySignature);

            string envelope =  Encoding.Default.GetString(senderSignature) +
                             Encoding.Default.GetString(senderPubKeyBlob)+ encryptedSymmetricKey + Encoding.Default.GetString(senderData);
            return _isCryptedFlag + envelope.ToBase64();
        }
        /// <summary>
        /// Возвращает ключ для дешифровки, который надо передать в сообщение
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <returns>
        /// 
        /// </returns>
        public string EncryptFile(string inputFile, string outputFile)  
        {          
                string symmetricKey = GetRandomString(8);
                string encryptedSymmetricKey = EncryptString(symmetricKey);

                //Шифруем любой тип файлов по алгоритму aes
                RijndaelManaged aes = new RijndaelManaged();
                byte[] IV = ASCIIEncoding.UTF8.GetBytes("ahjsyehwgsbckfbd");
                try
                {
                    byte[] key = ASCIIEncoding.UTF8.GetBytes(symmetricKey); //symmetricKey минимум 8 символов
                    using (FileStream fsCrypt = new FileStream(outputFile, FileMode.Create))
                    {
                        using (CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateEncryptor(key, IV),
                            CryptoStreamMode.Write))
                        {
                            using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                            {
                                int data;
                                while ((data = fsIn.ReadByte()) != -1)
                                {
                                    cs.WriteByte((byte)data);
                                }
                                aes.Clear();
                            }
                        }
                    }
                    return encryptedSymmetricKey;
                }
                catch (Exception)
                {
                    //Console.WriteLine(ex.Message);
                    aes.Clear();
                    return null;
                }

        }

        public void DecryptFile(string inputFile,string outputFile, string encryptedSymmetricKey)
        {
            if (File.Exists(inputFile))
            {
               // var fi = new FileInfo(inputFile);
                //var encryptedSymmetricKey = extension.Substring(0,344);
               // extension = extension.Substring(344);
                string symmetricKey = DecryptString(encryptedSymmetricKey);
                //string res = fi.Directory + @"\res" + extension;
                RijndaelManaged aes = new RijndaelManaged();
                try
                {
                    byte[] key = ASCIIEncoding.UTF8.GetBytes(symmetricKey);

                    //byte[] ckey = mkey;
                    byte[] cIV = ASCIIEncoding.UTF8.GetBytes("ahjsyehwgsbckfbd");

                    using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
                    {
                        using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                        {
                            using (
                                CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateDecryptor(key, cIV),
                                    CryptoStreamMode.Read))
                            {
                                int data;
                                while ((data = cs.ReadByte()) != -1)
                                {
                                    fsOut.WriteByte((byte)data);
                                }
                                aes.Clear();
                            }
                        }
                    }
                    File.Delete(inputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    aes.Clear();
                }
            }
        }

        #endregion

        private void BmpWrite(string message, Bitmap image)
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

        private string BmpRead(Bitmap image)
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

        public  Bitmap EncryptPicture(string path, string message)
        {
            string symmetricKey = GetRandomString(8);
            string encryptedSymmetricKey=EncryptString(symmetricKey);
            
            Bitmap image = (Bitmap)Image.FromFile(path);
            message = Encrypt(message, symmetricKey);
            message = encryptedSymmetricKey + message;
            BmpWrite(message, image);
            return image;

        }
        public  string DecryptPicture(Bitmap image)
        {
            string encryptedMessage = BmpRead(image);
            string encryptedSymmetricKey = encryptedMessage.Substring(0, 344);
            string message = encryptedMessage.Substring(344);
            string symmetricKey = DecryptString(encryptedSymmetricKey);
            string result = Decrypt(message, symmetricKey);
            image.Dispose();
            return result;
        }

      
    }
}
