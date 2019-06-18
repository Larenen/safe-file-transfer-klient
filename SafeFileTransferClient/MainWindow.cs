using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using SafeFileTransferClient.Structures;

namespace SafeFileTransferClient
{
    public partial class MainWindow : Form
    {
        private readonly TcpClient _tcpClient;
        private RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
        private string username;
        public MainWindow()
        {
            InitializeComponent();
            _tcpClient = new TcpClient();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            InitializeConnection();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.sft"))
            {
                LoadConfig();
            }
            else
            {
                CreateConfig();
            }
        }

        private void ZresetujNickKluczeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Czy na pewno chcesz dokonać resetu? Jest on bezpowrotny","Uwaga!",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning) == DialogResult.OK)
                CreateConfig();
        }

        private void ButtonChooseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    labelFileName.Text = openFileDialog.FileName;
                    string filContent = File.ReadAllText(openFileDialog.FileName);

                    var targetUser = ((User) listBoxUsers.SelectedItem);

                    //Create a new instance of RSAParameters.
                    RSAParameters RSAKeyInfo = new RSAParameters();

                    //Set RSAKeyInfo to the public key values. 
                    RSAKeyInfo.Modulus = Encoding.UTF8.GetBytes(targetUser.PublicKeyModulus);
                    RSAKeyInfo.Exponent = Encoding.UTF8.GetBytes(targetUser.PublicKeyExponent);

                    //Import key parameters into RSA.
                    rsa.ImportParameters(RSAKeyInfo);

                    byte[] encryptBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(filContent), false);

                    Debug.Print(Encoding.UTF8.GetString(encryptBytes));
                }
            }
        }

        private bool InitializeConnection()
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(rsa.ToXmlString(false));

            XmlNodeList mod = xml.GetElementsByTagName("Modulus");
            XmlNodeList exp = xml.GetElementsByTagName("Exponent");

            AuthStruct authStruct = new AuthStruct
            {
                RequestCode = 100,
                PublicKeyModulus = mod[0].InnerText,
                PublicKeyExponent = exp[0].InnerText,
                Username = username
            };

            try
            {
                _tcpClient.Connect(tbServerIp.Text, Convert.ToInt32(numPort.Value));
            }
            catch (Exception e)
            {
                //TODO Serwer nie odpowiada oblsluzyc
            }

            string initiateConnection = JsonConvert.SerializeObject(authStruct);

            byte[] dataBytes = Encoding.Default.GetBytes(initiateConnection);

            _tcpClient.GetStream().Write(dataBytes, 0, dataBytes.Length);

            while (_tcpClient.Available <= 0 && _tcpClient.Connected)
            {
                //TODO Obsluge czekania na polaczenie sie z serwerem tutaj zrobic po x czasie przestac czekac i zwrocic false ze sie nie udalo 
            }

            byte[] buffor = new byte[_tcpClient.Available];
            _tcpClient.Client.Receive(buffor);

            string jsonUserList = Encoding.Default.GetString(buffor);

            UserList userList = JsonConvert.DeserializeObject<UserList>(jsonUserList);

            if (userList.ReturnCode == 101)
            {
                listBoxUsers.Items.AddRange(userList.Users.ToArray());
                return true;
            }

            if (userList.ReturnCode == 102)
            {
                MessageBox.Show(userList.Message, "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return false;
        }

        private void LoadConfig()
        {
            string configFile = File.ReadAllText("config.sft");
            Config config = JsonConvert.DeserializeObject<Config>(configFile);
            username = config.Nickname;
            try
            {
                rsa.FromXmlString(config.XMLKeys);
            }
            catch (Exception)
            {
                if (MessageBox.Show(
                        "Nie prawidłowy plik konfiguracjny, czy chcesz wygenerowac nowy? W przypadku odmowy aplikacja zostanie zamknięta",
                        "Błąd wczytywania klucza", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    CreateConfig();
                }
                else
                {
                    Close();
                }
            }
        }

        private void CreateConfig()
        {
            SelectName selectName = new SelectName();

            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            if (selectName.ShowDialog(this) == DialogResult.OK)
            {
                // Read the contents of testDialog's TextBox.
                string selectedNickname = selectName.textBoxNickname.Text;
                rsa = new RSACryptoServiceProvider(2048);
                string xmlKeys = rsa.ToXmlString(true);

                Config config = new Config
                {
                    Nickname = selectedNickname,
                    XMLKeys = xmlKeys
                };

                var fs = CreateFile();

                string configJson = JsonConvert.SerializeObject(config);
                var configJsonByte = Encoding.UTF8.GetBytes(configJson);
                fs.Write(configJsonByte, 0, configJsonByte.Length);
                username = selectedNickname;

                fs.Close();
            }
            else
            {
                MessageBox.Show(
                    "Nie podano nazwy użytkownika, żeby przejść dalej włącz progam ponownie i podaj nazwę" +
                    Environment.NewLine, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }

            selectName.Dispose();
        }

        private FileStream CreateFile()
        {
            FileStream fs = null;

            try
            {
                fs = File.Create("config.sft");
            }
            catch (Exception)
            {
                if (MessageBox.Show(
                        "Nie udało się utworzyć pliku konfiguracyjnego na dysku czy chcesz ponowić próbę?",
                        "Błąd wczytywania klucza", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) ==
                    DialogResult.Retry)
                {
                    CreateFile();
                }
                else
                {
                    Close();
                }
            }

            return fs;
        }

    }
}
