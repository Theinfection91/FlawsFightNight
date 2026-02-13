using FlawsFightNight.Data.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.DataModels
{
    public class FTPCredential : IEncryptable
    {
        public string? IPAddress { get; set; }
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

        public string? UserLogsDirectory { get; set; }

        public FTPCredential() { }
    }
}
