using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeFileTransferClient.Structures
{
    class FileRequest
    {
        public int RequestCode { get; set; }
        public string FolderName { get; set; }
        public string  Sender { get; set; }
    }
}
