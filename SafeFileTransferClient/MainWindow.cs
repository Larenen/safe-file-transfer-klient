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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using SafeFileTransferClient.Structures;

namespace SafeFileTransferClient
{
    public partial class MainWindow : Form
    {
        private TcpClient _tcpClient;
        private RSACng rsa = new RSACng(2048);
        private Config config;
        private bool _active = false;
        delegate void FormProc(UserList userList);
        delegate void FormDel(bool status);
        private Thread _clientThread;

        private const int WaitTime = 5000000;

        private void InvokeChangeUserList(UserList userList)
        {
            Invoke(new FormProc(ChangeUserList), userList);
        }

        private void InvokeUpdateForm(bool status)
        {
            Invoke(new FormDel(UpdateForm), status);
        }

        private void ChangeUserList(UserList userList)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(config.XMLKeys);
            XmlNodeList mod = xml.GetElementsByTagName("Modulus");
            XmlNodeList exp = xml.GetElementsByTagName("Exponent");

            listBoxUsers.Items.Clear();
            listBoxUsers.Items.AddRange(userList.Users.ToArray());

            foreach (User user in listBoxUsers.Items)
            {
                if (user.PublicKeyModulus == mod[0].InnerText && user.PublicKeyExponent == exp[0].InnerText &&
                    user.Username == config.Nickname)
                {
                    listBoxUsers.Items.Remove(user);
                    break;
                }
            }
        }

        private void UpdateForm(bool status)
        {
            tbServerIp.Enabled = !status;
            numPort.Enabled = !status;
            if (status)
                btnConnect.Text = "Rozłącz";
            else
                btnConnect.Text = "Połącz";

            buttonChooseFile.Enabled = status;
            listBoxUsers.Enabled = status;
            listBoxUsers.Items.Clear();
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {            
            if(_tcpClient == null)
                InitializeConnection();
            else
            {
                CloseConnection();
            }
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
                    SendFile(openFileDialog);
                }
            }
        }

        private void SendFile(OpenFileDialog openFileDialog)
        {
            _clientThread.Abort();
            //Wybieramy użytkownika któremu chcemy wysłac
            var targetUser = ((User) listBoxUsers.SelectedItem);

            //Create a new instance of RSAParameters.
            RSAParameters RSAKeyInfo = new RSAParameters();

            //Set RSAKeyInfo to the public key values. 
            RSAKeyInfo.Modulus = Convert.FromBase64String(targetUser.PublicKeyModulus);
            RSAKeyInfo.Exponent = Convert.FromBase64String(targetUser.PublicKeyExponent);

            //Import key parameters into RSA.
            rsa.ImportParameters(RSAKeyInfo);

            //Instancja AESA
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.GenerateKey();

            AESHelper.FileEncrypt(openFileDialog.FileName, AES.Key, AES.IV);

            //Encrypt the symmetric key and IV.
            byte[] encryptedSymmetricKey = rsa.Encrypt(AES.Key, RSAEncryptionPadding.Pkcs1);
            byte[] encryptedSymmetricIV = rsa.Encrypt(AES.IV, RSAEncryptionPadding.Pkcs1);

            //Convert name to bytes and encrypt it
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(openFileDialog.FileName));
            byte[] encryptedName = rsa.Encrypt(fileNameBytes, RSAEncryptionPadding.Pkcs1);

            var request = new SendFileRequest
            {
                RequestCode = 105,
                EncryptedName = encryptedName,
                EncryptedKey = encryptedSymmetricKey,
                EncryptedIV = encryptedSymmetricIV,
                Receiver = targetUser.PublicKeyModulus
            };

            byte[] requestBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));

            _tcpClient.GetStream().Write(requestBytes, 0, requestBytes.Length);

            if (_tcpClient.Client.Poll(WaitTime, SelectMode.SelectRead))
            {
                byte[] buffor = new byte[_tcpClient.Available];
                _tcpClient.Client.Receive(buffor);
                int statusCode = Convert.ToInt32(Encoding.UTF8.GetString(buffor));
                if (statusCode == 106)
                {
                    using (FileStream fsSource = new FileStream(openFileDialog.FileName + ".aes", FileMode.Open))
                    {
                        const int bufferSize = 1024;
                        byte[] bytes = new byte[bufferSize];
                        int numBytesToRead = (int) fsSource.Length;
                        int numBytesRead = 0;

                        while (numBytesToRead > 0)
                        {
                            // Read may return anything from 0 to numBytesToRead.
                            int n = fsSource.Read(bytes, 0, bufferSize);

                            // Break when the end of the file is reached.
                            if (n == 0)
                                break;

                            _tcpClient.GetStream().Write(bytes, 0, n);

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }

                        CloseConnection();
                        MessageBox.Show("Plik wysłany poprawnie, nastąpiło rozłączanie z serwerem!", "Sukces",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    File.Delete(openFileDialog.FileName + ".aes");
                }
            }
            else
            {
                MessageBox.Show("Błąd podczas połączenia z serwerem", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseConnection();
            }
        }

        private bool InitializeConnection()
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(config.XMLKeys);

            XmlNodeList mod = xml.GetElementsByTagName("Modulus");
            XmlNodeList exp = xml.GetElementsByTagName("Exponent");
            
            AuthStruct authStruct = new AuthStruct
            {
                RequestCode = 100,
                PublicKeyModulus = mod[0].InnerText,
                PublicKeyExponent = exp[0].InnerText,
                Username = config.Nickname
            };

            try
            {
                _tcpClient = new TcpClient();
                if (!_tcpClient.ConnectAsync(tbServerIp.Text, Convert.ToInt32(numPort.Value)).Wait(1000))
                {
                    MessageBox.Show("Błąd łączenie sie z serwerem", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateForm(false);
                    _tcpClient.Dispose();
                    _tcpClient = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Podany adres nie jest adresem IP", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm(false);
                return false;
            }

            string initiateConnection = JsonConvert.SerializeObject(authStruct);

            byte[] dataBytes = Encoding.Default.GetBytes(initiateConnection);

            _tcpClient.GetStream().Write(dataBytes, 0, dataBytes.Length);

            if(_tcpClient.Client.Poll(WaitTime,SelectMode.SelectRead))
            {
                byte[] buffor = new byte[_tcpClient.Available];
                _tcpClient.Client.Receive(buffor);

                int returnCode = Convert.ToInt32(Encoding.UTF8.GetString(buffor));

                if (returnCode == 101)
                {
                    _active = true;
                    _clientThread = new Thread(ReceivingThread);
                    _clientThread.Start();
                    byte[] ok = Encoding.UTF8.GetBytes("OK");
                    _tcpClient.GetStream().Write(ok,0,ok.Length);
                    UpdateForm(true);
                    return true;
                }

                if (returnCode == 102)
                {
                    MessageBox.Show("Błąd połączenia z serwerem, serwer pełny, proszę spróbować pozniej", "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateForm(false);
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Błąd łączenie sie z serwerem", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateForm(false);
            }

            return false;
        }

        private void LoadConfig()
        {
            string configFile = File.ReadAllText("config.sft");
            config = JsonConvert.DeserializeObject<Config>(configFile);
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
                rsa = new RSACng(2048);
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

                fs.Close();

                var configFile = File.ReadAllText("config.sft");
                this.config = JsonConvert.DeserializeObject<Config>(configFile);
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

        private void ReceivingThread()
        {
            while(_active)
            {
                if (_tcpClient.Client.Poll(WaitTime,SelectMode.SelectRead))
                {
                    int size = _tcpClient.Available;

                    if (size <= 0)
                        break;

                    byte[] buffor = new byte[size];
                    _tcpClient.Client.Receive(buffor);
                    FileRequest fileRequest;
                    UserList userList;

                    try
                    {
                        fileRequest = JsonConvert.DeserializeObject<FileRequest>(Encoding.UTF8.GetString(buffor));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }

                    try
                    {
                        userList = JsonConvert.DeserializeObject<UserList>(Encoding.UTF8.GetString(buffor));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }

                    if (fileRequest != null && fileRequest.RequestCode == 107)
                    {
                        if (MessageBox.Show("Czy chcesz odebrać plik przesylany wyslany od " + fileRequest.Sender, "Nowy plik",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            var request = new FileRequest
                            {
                                FolderName = fileRequest.FolderName,
                                RequestCode = 108
                            };

                            byte[] requestCode = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
                            _tcpClient.GetStream().Write(requestCode,0,requestCode.Length);

                            if(_tcpClient.Client.Poll(WaitTime,SelectMode.SelectRead))
                            {
                                buffor = new byte[_tcpClient.Available];
                                _tcpClient.Client.Receive(buffor);

                                string jsonFileInfo = Encoding.UTF8.GetString(buffor);
                                ReceiveFile fileInfo = JsonConvert.DeserializeObject<ReceiveFile>(jsonFileInfo);

                                rsa.FromXmlString(config.XMLKeys);

                                byte[] fileName = rsa.Decrypt(fileInfo.EncryptedName,RSAEncryptionPadding.Pkcs1);
                                var decryptedFileName = Encoding.UTF8.GetString(fileName);

                                if(fileInfo.ReturnCode == 110)
                                {
                                    requestCode = Encoding.UTF8.GetBytes("111");
                                    _tcpClient.GetStream().Write(requestCode,0,requestCode.Length);

                                    if (_tcpClient.Client.Poll(5*WaitTime, SelectMode.SelectRead))
                                    {
                                        ReceiveFile(decryptedFileName, fileInfo);
                                        return;
                                    }
                                    MessageBox.Show("Błąd serwera", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    break;
                                }
                                MessageBox.Show("Błąd serwera", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                            MessageBox.Show("Błąd serwera", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                        else
                        {
                            var request = new FileRequest
                            {
                                FolderName = fileRequest.FolderName,
                                RequestCode = 109
                            };

                            byte[] requestCode = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
                            _tcpClient.GetStream().Write(requestCode,0,requestCode.Length);
                        }
                    }
                    if (userList != null && userList.ReturnCode == 112)
                    {
                        //Akutalizowanie listy klientow
                        InvokeChangeUserList(userList);
                    }                   
                }
            }
            //Wyszedl z petli znaczy ze polaczenie z serwerem przerwane
            _tcpClient.Close();
            _tcpClient = null;
            InvokeUpdateForm(false);
            _active = false;
            MessageBox.Show("Połączenie z serwerem zostało zerwane", "Rozłączono", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void ReceiveFile(string decryptedFileName, ReceiveFile fileInfo)
        {
            using (var output = File.Create(decryptedFileName + ".aes"))
            {
                var buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = _tcpClient.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }

                output.Close();
            }

            byte[] keyBytes = rsa.Decrypt(fileInfo.EncryptedKey, RSAEncryptionPadding.Pkcs1);
            byte[] ivBytes = rsa.Decrypt(fileInfo.EncryptedIV, RSAEncryptionPadding.Pkcs1);
            AESHelper.FileDecrypt(decryptedFileName + ".aes", decryptedFileName, keyBytes,
                ivBytes);

            File.Delete(decryptedFileName + ".aes");
            _tcpClient.Close();
            _tcpClient = null;
            InvokeUpdateForm(false);
            _active = false;
            MessageBox.Show(
                "Plik został pobrany do katalogu z którego został uruchomoiny program, połączenie z serwerm zostało zakończone",
                "Rozłączono", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(_clientThread != null)
                _clientThread.Abort();
            _active = false;
        }

        private void CloseConnection()
        {
            _clientThread.Abort();
            _tcpClient.Close();
            _tcpClient = null;
            UpdateForm(false);
            _active = false;
        }
    }
}
