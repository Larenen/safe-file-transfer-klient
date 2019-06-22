using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    class ReceiveFile
    {
        public int ReturnCode { get; set; }
        public byte[] EncryptedName { get; set; }
        public byte[] EncryptedKey { get; set; }
        public byte[] EncryptedIV { get; set; }
    }
}
