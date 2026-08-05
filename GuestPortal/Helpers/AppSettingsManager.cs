using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Configuration;

namespace CheckinPortal.BackOffice.Helpers
{
    public static class AppSettingsManager
    {
        private static readonly string EncryptionKey = "DotsSecureKey@2025#xjf_Egdewcjd!"; // Must be 32 bytes for AES256

        public static string GetDecryptedSetting(string key)
        {
            var encryptedValue = ConfigurationManager.AppSettings[key];
            return Decrypt(encryptedValue);
        }

        private static string Decrypt(string cipherText)
        {

            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(buffer))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader reader = new StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return cipherText;
            }
        }

        private static string Encrypt(string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter writer = new StreamWriter(cs))
                    {
                        writer.Write(plainText);
                    }
                    array = ms.ToArray();
                }
            }

            return Convert.ToBase64String(array);
        }

    }
}