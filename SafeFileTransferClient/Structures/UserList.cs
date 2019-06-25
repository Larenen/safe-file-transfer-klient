using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa zawiera liste użytkowników która wysyła serwer
    /// </summary>
    class UserList
    {
        /// <summary>
        /// Kod powrotu z serwera
        /// </summary>
        public int ReturnCode;
        /// <summary>
        /// Wiadomość zwrotna z serwera
        /// </summary>
        public string Message;
        /// <summary>
        /// Lista aktywnych użytkowników zwrócona przez serwer
        /// </summary>
        public List<User> Users;
    }
}
