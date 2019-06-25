using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa zawierająca dane niezbędne do autoryzacji użytkownika
    /// </summary>
    class AuthStruct
    {
        /// <summary>
        /// Kod prośby na jaką ma zaregować serwer
        /// </summary>
        public int RequestCode{ get; set; }
        /// <summary>
        /// Nazwa użytkownika
        /// </summary>
        public string Username{ get; set; }
        /// <summary>
        /// N klucza publicznego RSA
        /// </summary>
        public string PublicKeyModulus { get; set; }
        /// <summary>
        /// E klucza publicznego RSA
        /// </summary>
        public string PublicKeyExponent { get; set; }
    }
}
