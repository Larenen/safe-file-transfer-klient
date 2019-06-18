using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    class AuthStruct
    {
        public int RequestCode{ get; set; }
        public string Username{ get; set; }
        public string PublicKeyModulus { get; set; }
        public string PublicKeyExponent { get; set; }
    }
}
