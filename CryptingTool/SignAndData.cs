using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;


namespace CryptingTool
{
    public class SignAndData
    {

        private static string _isCryptedFlag = "ъйьz";

        public static CngKey senderKeySignature;
        public static byte[] senderPubKeyBlob;

        public static byte[] IV;

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


       private static void CreateKeys()
        {
            /* Метод Create(): класса CngKey в качестве аргумента получает алгоритм. С помощью метода Export() 
               из пары ключей экспортируется открытый ключ. Этот открытый ключ может быть предоставлен получателю, 
               чтобы он мог проверять действительность подписи.*/
            senderKeySignature = CngKey.Create(CngAlgorithm.ECDsaP256);
            senderPubKeyBlob = senderKeySignature.Export(CngKeyBlobFormat.GenericPublicBlob);
        }
       private static byte[] CreateSignature(byte[] data, CngKey key)
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
       private static bool VerifySignature(byte[] data, byte[] signature, byte[] pubKey)
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

        public static string Encrypt(string str, string keyCrypt)
       {
           return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(str), keyCrypt));
       }

        private static byte[] Encrypt(byte[] key, string value)
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

        private static string Decrypt(byte[] str, string keyCrypt)
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
                //Console.WriteLine("Содержимое сообщения неизвестно");
                return null;
            }

            return Result;
        }

        public static string Decrypt(string str, string keyCrypt)
        {
            string Result;
            try
            {
                CryptoStream Cs = InternalDecrypt(Convert.FromBase64String(str), keyCrypt);
                //CryptoStream Cs = InternalDecrypt(Encoding.ASCII.GetBytes(str), keyCrypt);
                
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

        private static CryptoStream InternalDecrypt(byte[] key, string value)
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
        public static string SplitAndUnpackReceivedMessage(string message)
        {
            if (string.IsNullOrEmpty(message) || message.Length < _isCryptedFlag.Length)
            {
                return message;
            }
            string des = message.Substring(0, _isCryptedFlag.Length);

            if (des!=_isCryptedFlag)
                return message;

            message = message.Substring(_isCryptedFlag.Length);
            message = message.FromBase64();

            byte[] receivedSignature = Encoding.Default.GetBytes(message.Substring(0, 64));
            byte[] receivedPubKey = Encoding.Default.GetBytes(message.Substring(64, 72));
            byte[] receivedData = Encoding.Default.GetBytes(message.Substring(136));

            if (VerifySignature(receivedData, receivedSignature, receivedPubKey))
            {
                return Decrypt(receivedData, "baba");               
            }
            return message;
        }

        /// <summary>
        /// Шифровка сообщения
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string MakingEnvelope(string message)         //TODO Добавить в функцию строковую переменную ключа для шифрования данных
        {
            CreateKeys();
            
            byte[] senderData = Encrypt(Encoding.UTF8.GetBytes(message), "baba");

            byte[] senderSignature = CreateSignature(senderData, senderKeySignature);
            var data = Encoding.Default.GetString(senderSignature) +
                        Encoding.Default.GetString(senderPubKeyBlob) +
                        Encoding.Default.GetString(senderData);

            return _isCryptedFlag + data.ToBase64();
        }
        public static void ChangingFile(string path)         //TODO Добавить в функцию строковую переменную ключа для шифрования данных
        {
            CreateKeys();
            try
            {
                StreamReader sr = new StreamReader(path,Encoding.ASCII);
                
                byte[] senderData = Encrypt(Encoding.UTF8.GetBytes(sr.ReadToEnd()), "baba");

                byte[] senderSignature = CreateSignature(senderData, SignAndData.senderKeySignature);

                string hiddenFileContent = Encoding.Default.GetString(senderSignature) +
                             Encoding.Default.GetString(SignAndData.senderPubKeyBlob) + Encoding.Default.GetString(senderData);

                File.WriteAllText("probe.crypt", hiddenFileContent);
                
            }
            catch (FileNotFoundException)
            {

                Console.WriteLine("Нет файла");
            }
        }
        public static void RetreivingFile(string path)         //TODO Добавить в функцию строковую переменную ключа для шифрования данных
        {
            try
            {
                string message = File.ReadAllText(path);
                int n = message.Length;
                byte[] receivedSignature = Encoding.Default.GetBytes(message.Substring(0, 64));
                byte[] receivedPubKey = Encoding.Default.GetBytes(message.Substring(64, 72));
                string nut = message.Substring(136);
                int m = nut.Length;
                byte[] receivedData = Encoding.Default.GetBytes(message.Substring(136));

                if (VerifySignature(receivedData, receivedSignature, receivedPubKey))
                {
                    Console.WriteLine("123");
                    string data = Decrypt(receivedData, "baba");
                    Console.WriteLine(data.Length);
                    File.WriteAllText("res.rar",data,Encoding.UTF8);

                }
            }
            catch (FileNotFoundException)
            {

                Console.WriteLine("Нет файла");
            }
        }

        public static void EncryptFile(string inputFile, string outputFile, string skey) //TODO Разобраться с вектором инициализации: либо он всегда одинаковый, либо создавать его динамически и отправлять вместе с файлом
            {
            //Шифруем любой тип файлов по алгоритму aes
            RijndaelManaged aes = new RijndaelManaged();
            aes.GenerateIV();
            IV = aes.IV;
            try
            {
                byte[] key = ASCIIEncoding.UTF8.GetBytes(skey);//skey минимум 8 символов

                //byte[] IV = ASCIIEncoding.UTF8.GetBytes(sIV);
                using (FileStream fsCrypt = new FileStream(outputFile, FileMode.Create))
                {
                    using (CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateEncryptor(key, aes.IV),
                        CryptoStreamMode.Write))
                    //using (CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateEncryptor(key, key),
                    //    CryptoStreamMode.Write))
                    {
                        using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                        {
                            int data;
                            while ((data = fsIn.ReadByte()) != -1)
                            {
                                cs.WriteByte((byte) data);
                            }
                            aes.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                aes.Clear();
            }
        }

        public static void DecryptFile(string inputFile, string outputFile, string skey)
        {
            RijndaelManaged aes = new RijndaelManaged();
            try
            {
                byte[] key = ASCIIEncoding.UTF8.GetBytes(skey);
                //byte[] ckey = mkey;
                byte[] cIV = IV;

                using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
                {
                    using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                    {
                        using (
                            CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateDecryptor(key, cIV),
                                CryptoStreamMode.Read))
                              //CryptoStream cs = new CryptoStream(fsCrypt, aes.CreateDecryptor(key, key),
                              //  CryptoStreamMode.Read))
                        {
                            int data;
                            while ((data = cs.ReadByte()) != -1)
                            {
                                fsOut.WriteByte((byte) data);
                            }
                            aes.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                aes.Clear();
            }
        }

        #endregion
    }
}
