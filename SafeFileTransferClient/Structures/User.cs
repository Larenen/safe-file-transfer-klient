using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    class User
    {
        public string Username { get; set; }
        public string PublicKeyModulus { get; set; }
        public string PublicKeyExponent { get; set; }

        public override string ToString()
        {
            return Username + ':' + PublicKeyModulus;
        }
    }
}
