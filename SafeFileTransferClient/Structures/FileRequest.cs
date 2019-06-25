using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    /// <summary>
    /// Klasa przechowujaca informacje o prośbie jaki plik chcemy pobrac
    /// </summary>
    class FileRequest
    {
        /// <summary>
        /// Kod obsługi dla serwera
        /// </summary>
        public int RequestCode { get; set; }
        /// <summary>
        /// Nazwa folderu tymczasowego na serwerze
        /// </summary>
        public string FolderName { get; set; }
        /// <summary>
        /// Osoba która wysyłała plik
        /// </summary>
        public string  Sender { get; set; }
    }
}
