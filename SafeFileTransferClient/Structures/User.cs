using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa przechowująca informacje o aktywnym użytkowniku na serwerze
    /// </summary>
    class User
    {
        /// <summary>
        /// Nazwa użytkownika
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// N klucza publicznego RSA
        /// </summary>
        public string PublicKeyModulus { get; set; }
        /// <summary>
        /// E klucza publicznego RSA
        /// </summary>
        public string PublicKeyExponent { get; set; }
        /// <summary>
        /// Odpowiada za poprawne wyświetlanie użytkowników na liście
        /// </summary>
        /// <returns>Zwraca stringa</returns>
        public override string ToString()
        {
            return Username + ':' + PublicKeyModulus;
        }
    }
}
