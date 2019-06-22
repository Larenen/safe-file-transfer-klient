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
        private readonly TcpClient _tcpClient;
        private RSACng rsa = new RSACng(2048);
        private string username;
        private Config config;
        private bool _active = false;
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
                    //Wybieramy użytkownika któremu chcemy wysłac
                    var targetUser = ((User) listBoxUsers.SelectedItem);

                    var test = rsa.ExportParameters(false);
                    var testkey = Encoding.UTF8.GetString(test.Modulus);

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

                    byte[] encryptedFile  = AESHelper.FileEncrypt(openFileDialog.FileName, AES.Key, AES.IV);

                    //Encrypt the symmetric key and IV.
                    byte[] encryptedSymmetricKey = rsa.Encrypt(AES.Key,RSAEncryptionPadding.Pkcs1);
                    byte[] encryptedSymmetricIV = rsa.Encrypt(AES.IV, RSAEncryptionPadding.Pkcs1);

                    //Convert name to bytes and encrypt it
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(openFileDialog.FileName));
                    byte[] encryptedName = rsa.Encrypt(fileNameBytes, RSAEncryptionPadding.Pkcs1);

                    /*

                    rsa.FromXmlString(config.XMLKeys);
                    byte[] encryptedName2 = rsa.Encrypt(fileNameBytes, false);
                    var test = Encoding.UTF8.GetString(rsa.Decrypt(encryptedName2, false));
                    
                    */

                    //Generowanie podisu raczej do wyjebanie
                    ////Create hash from encrypted file and ecnrypted key
                    //SHA256 sha256 = SHA256.Create();
                    //byte[] encryptedFileHash = sha256.ComputeHash(encryptedFile);
                    //byte[] encryptedKeyHash = sha256.ComputeHash(encryptedSymmetricKey);
                    //byte[] encryptedIVHash = sha256.ComputeHash(encryptedSymmetricIV);
                    //byte[] encryptedNameHash = sha256.ComputeHash(encryptedName);

                    ////Konkatenacja wszystkich hashy
                    //IEnumerable<byte> concatedHashes = encryptedFileHash.Concat(encryptedKeyHash).Concat(encryptedIVHash).Concat(encryptedNameHash);

                    ////Hashowanie konkatenacji
                    //byte[] concatHash = sha256.ComputeHash(concatedHashes.ToArray());

                    ////Wczytanie klucza prywatnego uzytkownika i popdisanie konkatenacji
                    //Config config = JsonConvert.DeserializeObject<Config>(configFile);
                    //rsa.FromXmlString(config.XMLKeys);
                    //byte [] signature = rsa.SignHash(concatHash, CryptoConfig.MapNameToOID("SHA256"));

                    var request = new SendFileRequest
                    {
                        RequestCode = 105,
                        EncryptedName = encryptedName,
                        EncryptedKey = encryptedSymmetricKey,
                        EncryptedIV = encryptedSymmetricIV,
                        Receiver = targetUser.PublicKeyModulus 
                    };

                    byte[] requestBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));

                    _tcpClient.GetStream().Write(requestBytes,0,requestBytes.Length);

                    if(_tcpClient.Client.Poll(100000,SelectMode.SelectRead))
                    {
                        byte[] buffor = new byte[_tcpClient.Available];
                        _tcpClient.Client.Receive(buffor);
                        int statusCode = Convert.ToInt32(Encoding.UTF8.GetString(buffor));
                        if (statusCode == 106)
                        {
                            using (MemoryStream fsSource = new MemoryStream(encryptedFile))
                            {
                                const int bufferSize = 1024;
                                byte[] bytes = new byte[bufferSize];
                                int numBytesToRead = (int)fsSource.Length;
                                int numBytesRead = 0;

                                while (numBytesToRead > 0)
                                {
                                    // Read may return anything from 0 to numBytesToRead.
                                    int n = fsSource.Read(bytes, 0, bufferSize);

                                    // Break when the end of the file is reached.
                                    if (n == 0)
                                        break;

                                    _tcpClient.GetStream().Write(bytes,0,n);

                                    numBytesRead += n;
                                    numBytesToRead -= n;
                                }

                                
                            }
                        }
                    }

                    else
                    {
                        //TODO SERWER NIE ODPOWIEDZAIL OBSLUZYC
                    }
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

            if(_tcpClient.Client.Poll(100000,SelectMode.SelectRead))
            {
                byte[] buffor = new byte[_tcpClient.Available];
                _tcpClient.Client.Receive(buffor);

                string jsonUserList = Encoding.Default.GetString(buffor);

                UserList userList = JsonConvert.DeserializeObject<UserList>(jsonUserList);

                if (userList.ReturnCode == 101)
                {
                    listBoxUsers.Items.AddRange(userList.Users.ToArray());
                    _active = true;
                    new Thread(ReceivingThread).Start();
                    return true;
                }

                if (userList.ReturnCode == 102)
                {
                    MessageBox.Show(userList.Message, "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                //TODO PRZEZ 10 SEKUND SERWER NIE ODPOWIEDZAIL OBSLUZYC
            }

            return false;
        }

        private void LoadConfig()
        {
            string configFile = File.ReadAllText("config.sft");
            config = JsonConvert.DeserializeObject<Config>(configFile);
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
                username = selectedNickname;

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
                if (_tcpClient.Client.Poll(100000,SelectMode.SelectRead))
                {
                    int size = _tcpClient.Available;

                    byte[] buffor = new byte[size];
                    _tcpClient.Client.Receive(buffor);
                    FileRequest fileRequest;
                    try
                    {
                        fileRequest = JsonConvert.DeserializeObject<FileRequest>(Encoding.UTF8.GetString(buffor));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }

                    if(fileRequest == null)
                        continue;

                    if (fileRequest.RequestCode == 107)
                    {
                        if (MessageBox.Show("Czy chcesz odebrać plik przesylany wyslany od tutaj wstawic", "Nowy plik",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            var request = new FileRequest
                            {
                                FolderName = fileRequest.FolderName,
                                RequestCode = 108
                            };

                            byte[] requestCode = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
                            _tcpClient.GetStream().Write(requestCode,0,requestCode.Length);

                            if(_tcpClient.Client.Poll(100000,SelectMode.SelectRead))
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

                                    if (_tcpClient.Client.Poll(100000, SelectMode.SelectRead))
                                    {
                                        using (var output = File.Create(decryptedFileName + ".aes"))
                                        {
                                            var buffer = new byte[1024];
                                            int bytesRead;
                                            while ((bytesRead = _tcpClient.GetStream().Read(buffer, 0, buffer.Length)) >0)
                                            {
                                                output.Write(buffer, 0, bytesRead);
                                            }
                                            output.Close();
                                        }

                                        byte[] keyBytes = rsa.Decrypt(fileInfo.EncryptedKey, RSAEncryptionPadding.Pkcs1);
                                        byte[] ivBytes = rsa.Decrypt(fileInfo.EncryptedIV, RSAEncryptionPadding.Pkcs1);
                                        AESHelper.FileDecrypt(decryptedFileName + ".aes", decryptedFileName, keyBytes,
                                            ivBytes);

                                    }
                                }
                            }
                            else
                            {
                                //TODO PRZEZ 10 SEKUND SERWER NIE ODPOWIEDZAIL OBSLUZYC
                            }

                        }
                    }
                   
                }
            }
        }

    }
}
