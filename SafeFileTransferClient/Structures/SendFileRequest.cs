using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa przechowująca informacje o prośbie przesłania pliku na serwer
    /// </summary>
    class SendFileRequest
    {
        /// <summary>
        /// Kod obsługi dla serwera
        /// </summary>
        public int RequestCode { get; set; }
        /// <summary>
        /// Zaszyfrowana nazwa pliku
        /// </summary>
        public byte[] EncryptedName { get; set; }
        /// <summary>
        /// Zaszyfrowany klucz AES
        /// </summary>
        public byte[] EncryptedKey { get; set; }
        /// <summary>
        /// Zaszyfrowany wektor inicjujący AES
        /// </summary>
        public byte[] EncryptedIV { get; set; }
        /// <summary>
        /// Modulo odbiorcy pliku
        /// </summary>
        public string Receiver { get; set; }
    }
}
