using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Interfaces
{
    public interface IEncryptable
    {
        public static byte[] GetEncryptionKey()
        {
            string machineKey = $"{Environment.MachineName}{Environment.UserName}";
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
        }

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV + encrypted data
            byte[] result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] fullData = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = GetEncryptionKey();

            // Extract IV from the beginning
            byte[] iv = new byte[aes.IV.Length];
            byte[] encryptedBytes = new byte[fullData.Length - iv.Length];

            Buffer.BlockCopy(fullData, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
