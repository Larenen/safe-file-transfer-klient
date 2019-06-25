using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa zawierająca informacje niezbędne do prawidłowego odszyfrowania pliku
    /// </summary>
    class ReceiveFile
    {
        /// <summary>
        /// Kod obsługi dla serwera
        /// </summary>
        public int ReturnCode { get; set; }
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
    }
}
