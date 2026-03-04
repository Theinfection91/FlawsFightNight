using FlawsFightNight.Core.Attributes;
using FlawsFightNight.IO.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class FTPCredential : IEncryptable
    {
        public int Id { get; set; }
        public string ServerName { get; set; }
        public string? IPAddress { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }

        private string? _encryptedPassword;

        [JsonIgnore]
        public string? Password
        {
            get => string.IsNullOrEmpty(_encryptedPassword) ? null : IEncryptable.Decrypt(_encryptedPassword);
            set => _encryptedPassword = string.IsNullOrEmpty(value) ? null : IEncryptable.Encrypt(value);
        }

        // For JSON serialization
        public string? EncryptedPassword
        {
            get => _encryptedPassword;
            set => _encryptedPassword = value;
        }

        public string? UserLogsDirectoryPath { get; set; }

        public FTPCredential() { }
    }
}
