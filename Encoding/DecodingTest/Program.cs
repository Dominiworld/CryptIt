using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DecodingTest
{
    class Program
    {
        internal static CngKey receiverKeySignature;
        internal static byte[] receiverPubKeyBlob;
        static void Main(string[] args)
        {
            CreateKeys();
           
            byte[] data = File.ReadAllBytes("Data.txt");
            byte[] signature = File.ReadAllBytes("Signature.txt");
            byte [] pubKey=File.ReadAllBytes("Pub.txt");
            

            if (VerifySignature(data, signature, pubKey))
            {
                Console.WriteLine("Полученные данные: " + Decrypt(Convert.ToBase64String(data), "baba"));
            }


        }
        static bool VerifySignature(byte[] data, byte[] signature, byte[] pubKey)
        {
            bool retValue = false;
            using (CngKey key = CngKey.Import(pubKey, CngKeyBlobFormat.GenericPublicBlob))
            {
                var signingAlg = new ECDsaCng(key);
                retValue = signingAlg.VerifyData(data, signature);
                signingAlg.Clear();
            }
            if (retValue)
            {
                Console.WriteLine("Успех");
                return true;
            }
            else
            {
                Console.WriteLine("Неудача");
                return false;
            }
            

        }
        static void CreateKeys()
        {
            receiverKeySignature = CngKey.Create(CngAlgorithm.ECDsaP256);
            receiverPubKeyBlob = receiverKeySignature.Export(CngKeyBlobFormat.GenericPublicBlob);
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

    }
}
