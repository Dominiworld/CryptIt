using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EncodingCombination
{
    class Program
    {

        internal static CngKey senderKeySignature;
        internal static byte[] senderPubKeyBlob;
        static void Main(string[] args)
        {
            CreateKeys();
            Console .Write("Введите сообщение:");


            string senderDataString = Console.ReadLine();

            byte[] senderData = Encrypt(Encoding.UTF8.GetBytes(senderDataString),"baba");
            
            string senderDataCryptString = Encrypt(senderDataString, "baba");
            
            byte[] senderSignature = CreateSignature(senderData, senderKeySignature);

            File.WriteAllBytes("Data.txt",senderData);
            File.WriteAllBytes("Signature.txt",senderSignature);
            File.WriteAllBytes("Pub.txt",senderPubKeyBlob);  
            
          

            Console.WriteLine("Созданная для отправителя цифровая подпись: {0}",
                Convert.ToBase64String(senderSignature));
             

            if (VerifySignature(senderData, senderSignature, senderPubKeyBlob))
            {
                Console.WriteLine("Подпись отправителя была успешно проверена");
                Console.WriteLine("Данные получены. Содержимое: " + Decrypt(senderDataCryptString,"baba"));
            }


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
        

        static void CreateKeys()
        {
            /* Метод Create(): класса CngKey в качестве аргумента получает алгоритм. С помощью метода Export() 
               из пары ключей экспортируется открытый ключ. Этот открытый ключ может быть предоставлен получателю, 
               чтобы он мог проверять действительность подписи.*/
            senderKeySignature = CngKey.Create(CngAlgorithm.ECDsaP256);
            senderPubKeyBlob = senderKeySignature.Export(CngKeyBlobFormat.GenericPublicBlob);
        }
        static byte[] CreateSignature(byte[] data, CngKey key)
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
        static bool VerifySignature(byte[] data, byte[] signature, byte[] pubKey)
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

        public static string Decrypt(string str, string keyCrypt)
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
        private static CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            ICryptoTransform ct = sa.CreateDecryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);
            MemoryStream ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }



        #endregion
    }
}
