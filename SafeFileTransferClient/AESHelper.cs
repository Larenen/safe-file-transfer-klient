using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SafeFileTransferClient
{
    static class AESHelper
    {
        /// <summary>
        /// Szyfruje wybrany plik do pliku .aes
        /// Funkcja tworzy nowy plik o tej samej nazwie z rozszerzeniem .aes i go otwiera.
        /// Konfiguruje odpowiednio instancje AES.
        /// Funkcja wczytuje plik i go szyfruje rownocześnie zapisując go do nowego pliku wyjsciowego.
        /// </summary>
        /// <param name="inputFile">Nazwa pliku do zaszyfrowania</param>
        /// <param name="keyBytes">Klucz AES</param>
        /// <param name="IvBytes">Wektor inicjujący AES</param>
        public static void FileEncrypt(string inputFile, byte[] keyBytes, byte[] IvBytes)
        {
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

            //create output file name
            //TODO TRY AND CATCH
            FileStream fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);

            //Set Rijndael symmetric encryption algorithm
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            AES.Key = keyBytes;
            AES.IV = IvBytes;

            //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
            AES.Mode = CipherMode.CFB;


            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents(); // -> for responsive GUI, using Task will be better!
                    cs.Write(buffer, 0, read);
                }

                // Close up
                fsIn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
            }
        }

        /// <summary>
        /// Deszyfruje plik
        /// Funkcja otwiera zaszyfrowany plik, nastepnie tworzy instancje AES odpowiednio ją konfigurując
        /// Tworzy plik wyjsciowy ktorego nazwa podowana jest w argumencie funkcji
        /// Deszyfruje plik, zapisując go przy tym do pliku wyjściowego
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <param name="password"></param>
        public static void FileDecrypt(string inputFile, string outputFile, byte[] keyBytes, byte[] IvBytes)
        {

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Key = keyBytes;
            AES.IV = IvBytes;
            AES.Padding = PaddingMode.Zeros;
            AES.Mode = CipherMode.CFB;

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents();
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }
    }
}
