using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SafeFileTransferClient
{
    class AuthStruct
    {
        public int RequestCode;
        public string Username;
        public string PublicKey;
    }

    class UserList
    {
        public int ReturnCode;
        public string Message;
        public List<User> Users;
    }

    class User
    {
        public string Username { get; set; }
        public string PublicKey { get; set; }

        public override string ToString()
        {
            return Username + ':' + PublicKey;
        }
    };

    public partial class Form1 : Form
    {
        private readonly TcpClient _tcpClient;

        public Form1()
        {
            InitializeComponent();
            _tcpClient = new TcpClient();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            AuthStruct authStruct = new AuthStruct
            {
                RequestCode = 100,
                PublicKey = "TestKey",
                Username = "TestUsername"
            };

            _tcpClient.Connect(tbServerIp.Text,Convert.ToInt32(numPort.Value));

            string initiateConnection = JsonConvert.SerializeObject(authStruct);

            byte[] dataBytes = Encoding.Default.GetBytes(initiateConnection);

            _tcpClient.GetStream().Write(dataBytes,0,dataBytes.Length);

            while (_tcpClient.Available <= 0 && _tcpClient.Connected)
            {
                //tutaj dodac cos zeby nie czekal w kolko jak pajac
                //wrzucic do innego watku
            }

            byte[] buffor = new byte[_tcpClient.Available];
            _tcpClient.Client.Receive(buffor);

            string jsonUserList = Encoding.Default.GetString(buffor);

            UserList userList = JsonConvert.DeserializeObject<UserList>(jsonUserList);
            if (userList.ReturnCode == 101)
            {
                listBoxUsers.Items.AddRange(userList.Users.ToArray());
            }
            else if(userList.ReturnCode == 102)
            {
                MessageBox.Show(userList.Message, "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
