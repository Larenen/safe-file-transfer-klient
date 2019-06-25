using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Obiekt przechowujący informacje o konfiguracji programu
    /// </summary>
    class Config
    {
        /// <summary>
        /// Nazwa użytkownika
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// Para kluczy publiczny i prywatny użytkownika w postaci XML'a
        /// </summary>
        public string XMLKeys { get; set; }
    }
}
