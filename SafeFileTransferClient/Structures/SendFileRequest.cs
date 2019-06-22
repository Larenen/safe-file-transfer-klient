using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    class SendFileRequest
    {
        public int RequestCode { get; set; }
        public byte[] EncryptedName { get; set; }
        public byte[] Signature { get; set; }
        public byte[] EncryptedKey { get; set; }
        public byte[] EncryptedIV { get; set; }
        public string Receiver { get; set; }
    }
}
