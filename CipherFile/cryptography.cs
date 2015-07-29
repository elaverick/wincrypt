using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twofish_NET;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Crypt
{
    class cryptography
    {
        private SymmetricAlgorithm twoFish;
        private bool _bail = false;
        private bool isConsole;
        private float _encryptionProgress;
        private bool oldFormatKeyExpansion = false;

        public float encryptionProgress
        {
            get { return _encryptionProgress; }
        }

        public bool bail
        {
            set { _bail = value; }
        }

        public cryptography(bool IsConsole, bool oldExpansionMethod = false)
        {
            isConsole = IsConsole;
            oldFormatKeyExpansion = oldExpansionMethod;
            twoFish = new Twofish();
            twoFish.Mode = CipherMode.CBC;
            twoFish.GenerateIV();
            twoFish.GenerateKey();
        }

        /// <summary>
        /// Ensure key is valid with paddding (old style) or scrypt
        /// </summary>
        private byte[] getValidKey(string Key)
        {
            if (oldFormatKeyExpansion)
            {
                string sTemp;
                if (twoFish.LegalKeySizes.Length > 0)
                {
                    int lessSize = 0, moreSize = twoFish.LegalKeySizes[0].MinSize;
                    // key sizes are in bits

                    while (Key.Length * 8 > moreSize &&
                           twoFish.LegalKeySizes[0].SkipSize > 0 &&
                           moreSize < twoFish.LegalKeySizes[0].MaxSize)
                    {
                        lessSize = moreSize;
                        moreSize += twoFish.LegalKeySizes[0].SkipSize;
                    }

                    if (Key.Length * 8 > moreSize)
                        sTemp = Key.Substring(0, (moreSize / 8));
                    else
                        sTemp = Key.PadRight(moreSize / 8, ' ');
                }
                else
                    sTemp = Key;
                // convert the secret key to byte array
                return ASCIIEncoding.ASCII.GetBytes(sTemp);
            }
            else
            {
                byte[] salt =   {52, 198, 227, 40, 63, 218, 188, 19,
                            210, 181, 50, 150, 145, 70, 133, 219,
                            150, 63, 156, 105, 157, 202, 64, 88,
                            109, 135, 148, 0, 15, 173, 120, 156,
                            152, 204, 234, 78, 86, 238, 108, 204,
                            250, 153, 17, 109, 129, 201, 103, 201,
                            183, 116, 188, 35, 157, 37, 234, 206,
                            208, 104, 12, 179, 248, 114, 63, 71};
                byte[] key = CryptSharp.SCrypt.ComputeDerivedKey(Encoding.Unicode.GetBytes(Key), salt, 262144, 8, 1, null, 32);
                return key;
            }
        }

        /// <summary>
        /// Ensure IV is valid with padding
        /// </summary>
        private byte[] getValidIV(String InitVector, int ValidLength)
        {
            if (InitVector.Length > ValidLength)
            {
                return ASCIIEncoding.ASCII.GetBytes(InitVector.Substring(0, ValidLength));
            }
            else
            {
                return ASCIIEncoding.ASCII.GetBytes(InitVector.PadRight(ValidLength, ' '));
            }
        }

        /// <summary>
        /// Check if we still have quota on random.org
        /// </summary>
        private bool checkRandomORGQuota()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.random.org/quota/?format=plain");
            try
            {
                using (var response = request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a random init value.
        /// </summary>
        /// <param name="internetSource">Use random.org for random source</param>
        /// <returns>Byte array IV</returns>
        private byte[] generateValidIV(bool internetSource)
        {
            byte[] result = new byte[16];

            if (!internetSource)
            {
                RandomNumberGenerator rng = new RNGCryptoServiceProvider();
                rng.GetBytes(result);
            }
            else
            {
                if (checkRandomORGQuota())
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.random.org/integers/?num=16&min=0&max=255&col=1&base=10&format=plain&rnd=new");
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream receiveStream = response.GetResponseStream();
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                    StreamReader readStream = new StreamReader(receiveStream, encode);
                    string[] randomNumbers = (readStream.ReadToEnd()).Trim().Split('\n');
                    for (int i = 0; i < 16; i++)
                    {
                        result[i] = Convert.ToByte(randomNumbers[i]);
                    }
                    response.Close();
                    X509Certificate cert = request.ServicePoint.Certificate;
                    X509Certificate2 cert2 = new X509Certificate2(cert);
                    
                    if (isConsole)
                    {
                        util.writeFullWidth("Random.org Certificate Information:", ConsoleColor.White, ConsoleColor.DarkBlue);
                        Console.WriteLine("\n" + cert2.SubjectName.Decode(X500DistinguishedNameFlags.UseNewLines) + "\n");
                    }

                    if (cert2.Verify())
                    {
                        if (isConsole)
                            Console.WriteLine("OK!");
                    }
                    else
                        throw new Exception("Certificate Chain Cannot Be Verified!");
                }
                else 
                    throw new Exception("Exceeded Random.Org Quota");
            }

            return result;
        }

        public string randomString(int size)
        {
            Random random = new Random((int)DateTime.Now.Ticks);

            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                switch (random.Next(0, 3))
                {
                    case 0:
                        ch = (char)random.Next('a', 'z' + 1);
                        break;
                    case 1:
                        ch = (char)random.Next('A', 'Z' + 1);
                        break;
                    default:
                        ch = (char)random.Next('0', '9' + 1);
                        break;
                }
                builder.Append(ch);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Main Encryption algorythm
        /// </summary>
        /// <param name="filename">Files to encrypt</param>
        /// <param name="key">Pass key</param>
        /// <param name="internetSource">Use an internet based PRNG</param>
        /// <param name="outputFilename">Vault file</param>
        /// <param name="IVFilename">Optional seperate IV file</param>
        public void encrypt(string[] filenames, string key, bool internetSource = false, string outputFilename = null, string IVFilename = null)
        {
            FileStream sin, sout;
            CryptoStream encStream;
            byte[] buf = new byte[3];
            long lLen;
            int nRead, nReadTotal;
            string embeddedFilename;
            int progressLine = 0;

            twoFish.Key = getValidKey(key);
            try
            {
                twoFish.IV = generateValidIV(internetSource);
            }
            catch (Exception e)
            {
                throw;
            }

            if (outputFilename == null)
                outputFilename = randomString(16) + ".dat";

            if (!string.IsNullOrEmpty(IVFilename))
            {
                try
                {
                    using (FileStream ivsout = new FileStream(IVFilename, FileMode.Create, FileAccess.Write))
                    {
                        ivsout.Write(twoFish.IV, 0, 16);
                    }
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            using (sout = new FileStream(outputFilename, FileMode.Create, FileAccess.Write))
            {
                if (IVFilename == null)
                    sout.Write(twoFish.IV, 0, 16);

                encStream = new CryptoStream(sout,
                        twoFish.CreateEncryptor(),
                        CryptoStreamMode.Write);

                foreach (string filename in filenames)
                {
                    embeddedFilename = Path.GetFileName(filename);
                    if (embeddedFilename.Length > 255)
                        throw new Exception("Filename too long");

                    try
                    {
                        encStream.Write(ASCIIEncoding.ASCII.GetBytes(embeddedFilename), 0, embeddedFilename.Length);
                        encStream.WriteByte(0x03); // End of Filename Marker
                        long fileLength = new FileInfo(filename).Length;
                        encStream.WriteByte((byte)fileLength);
                        encStream.WriteByte((byte)(fileLength >> 8));
                        encStream.WriteByte((byte)(fileLength >> 16));
                        encStream.WriteByte((byte)(fileLength >> 24));
                        encStream.WriteByte((byte)(fileLength >> 32));
                        encStream.WriteByte((byte)(fileLength >> 40));
                        encStream.WriteByte((byte)(fileLength >> 48));
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
                try
                {
                    encStream.WriteByte((byte)0x1C); //End of Names Marker
                }
                catch (Exception e)
                {
                    throw;
                }
                foreach (string filename in filenames)
                {
                    try
                    {
                        using (sin = new FileStream(filename, FileMode.Open, FileAccess.Read))
                        {
                            if (isConsole)
                            {
                                Console.WriteLine();
                                progressLine = Console.CursorTop;
                                Console.Write("[                                                  ]");
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.CursorVisible = false;
                            }

                            lLen = sin.Length;
                            nReadTotal = 0;
                            while (nReadTotal < lLen)
                            {
                                if (nReadTotal % 100 == 0)
                                {
                                    if (isConsole)
                                    {
                                        Console.SetCursorPosition(1, progressLine);
                                        for (int i = 0; i < ((float)nReadTotal / (float)lLen) * 50; i++)
                                            Console.Write("#");
                                        Console.SetCursorPosition(53, progressLine);
                                        Console.WriteLine(String.Format("{0:0.00}", ((float)nReadTotal / (float)lLen) * 100f) + "%");
                                    }
                                    else
                                        _encryptionProgress = ((float)nReadTotal / (float)lLen) * 100f;

                                }

                                nRead = sin.Read(buf, 0, buf.Length);
                                encStream.Write(buf, 0, nRead);
                                nReadTotal += nRead;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
                encStream.Close();
            }
            if (isConsole)
            {
                Console.CursorVisible = true;
                Console.ResetColor();
            }
        }


        /// <summary>
        /// Read the IV
        /// </summary>
        /// <param name="fs">Open Filestream to read from</param>
        /// <returns>IV as a byte array</returns>
        private byte[] getStoredIV(FileStream fs)
        {
            byte[] iv = new byte[16];
            fs.Read(iv, 0, 16);
            return iv;
        }

        /// <summary>
        /// Decrypts the file table only.  Used for listing the contents of vaults
        /// </summary>
        /// <param name="filename">File to open</param>
        /// <param name="key">Pass key</param>
        /// <param name="IVFilename">Optional filename for the IV file</param>
        /// <returns>Dictionary containing filenames and their lengths</returns>
        public Dictionary<string, long> decryptFileTable(string filename, string key, string IVFilename = null)
        {            
            FileStream sin;
            CryptoStream decStream;
            byte[] buf = new byte[3];
            Dictionary<string,long> fileTable = new Dictionary<string,long>();

            List<long> fileLengths = new List<long>();

            twoFish.Key = getValidKey(key);

            if(IVFilename!=null)
                using (FileStream ivsin = new FileStream(IVFilename, FileMode.Open, FileAccess.Read))
                {
                    twoFish.IV = getStoredIV(ivsin);
                }

            using (sin = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                if(IVFilename==null)
                    twoFish.IV = getStoredIV(sin);

                decStream = new CryptoStream(sin,
                            twoFish.CreateDecryptor(),
                            CryptoStreamMode.Read);

                int readChar = 0;
                while (readChar != -1 || readChar == (byte)0x1C)
                {
                    string embeddedOutFilename = "";
                    while (readChar != -1)
                    {
                        readChar = decStream.ReadByte();
                        if (readChar == (byte)0x03 || readChar == (byte)0x1C)
                            break;
                        embeddedOutFilename += (char)readChar;
                    }
                    if (readChar == (byte)0x1C)
                        break;
                    
                    byte[] fileLengthByteArray = new byte[7];
                    for (int i = 0; i < 7; i++)
                    {
                        int b = decStream.ReadByte();
                        if (b == -1)
                        {
                            throw new Exception("Error in file!");
                        }
                        fileLengthByteArray[i] = (byte)b;
                    }
                    long fileLength = 0;
                    for (int i = 6, j = 48; i >= 0; i--, j -= 8)
                    {
                        fileLength += (long)(fileLengthByteArray[i] << j);
                    }
                    fileTable.Add(embeddedOutFilename,fileLength);
                }
            }
            return fileTable;
        }

        /// <summary>
        /// Main decryption algorythm
        /// </summary>
        /// <param name="filename">File to decrypt</param>
        /// <param name="key">Pass key</param>
        /// <param name="IVFilename">Optional external IV File</param>
        /// <param name="outputPath">Path to create decrypted files in</param>
        /// <param name="filesToDecrypt">List of file names to decrypt</param>
        public void decrypt(string filename, string key, string IVFilename=null, string outputPath="",List<string> filesToDecrypt = null)
        {
            FileStream sin,sout;
            CryptoStream decStream;
            byte[] buf = new byte[3];
            List<string> fileNames = new List<string>();
            int nRead, nReadTotal;
            int progressLine = 0;
            List<long> fileLengths = new List<long>();

            twoFish.Key = getValidKey(key);


            if(IVFilename!=null)
                using (FileStream ivsin = new FileStream(IVFilename, FileMode.Open, FileAccess.Read))
                {
                    twoFish.IV = getStoredIV(ivsin);
                }

            using (sin = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                if(IVFilename==null)
                    twoFish.IV = getStoredIV(sin);

                decStream = new CryptoStream(sin,
                            twoFish.CreateDecryptor(),
                            CryptoStreamMode.Read);

                int readChar = 0;
                while (readChar != -1 || readChar == (byte)0x1C)
                {
                    string embeddedOutFilename = "";
                    while (readChar != -1)
                    {
                        readChar = decStream.ReadByte();
                        if (readChar == 3 || readChar == (byte)0x1C)
                            break;
                        embeddedOutFilename += (char)readChar;
                    }
                    if (readChar == (byte)0x1C)
                        break;
                    fileNames.Add(embeddedOutFilename);


                    byte[] fileLengthByteArray = new byte[7];
                    for (int i = 0; i < 7; i++)
                    {
                        int b = decStream.ReadByte();
                        if (b == -1)
                        {
                            throw new Exception("Error in file!");
                        }
                        fileLengthByteArray[i] = (byte)b;
                    }
                    long fileLength = 0;
                    for (int i = 6, j = 48; i >= 0; i--, j -= 8)
                    {
                        fileLength += (long)(fileLengthByteArray[i] << j);
                    }
                    fileLengths.Add(fileLength);
                }
                
                for (int i = 0; i < fileNames.Count; i++)
                {
                    if (isConsole)
                    {
                        Console.WriteLine();
                        progressLine = Console.CursorTop;
                        Console.Write("[                                                  ]");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.CursorVisible = false;
                    }

                    if (filesToDecrypt != null)
                        if (filesToDecrypt.Count == 0)
                            continue;

                    nReadTotal = 0;

                    using (sout = new FileStream(Path.GetTempPath() + "\\" + Path.GetFileNameWithoutExtension(filename) + ".dcodetmp", FileMode.Create, FileAccess.Write))
                    {
                        while ((nReadTotal < fileLengths[i]) && !_bail)
                        {
                            if (isConsole)
                            {
                                if (nReadTotal % 100 == 0)
                                {
                                    Console.SetCursorPosition(1, progressLine);
                                    for (int j = 0; j < ((float)nReadTotal / (float)fileLengths[i]) * 50; j++)
                                        Console.Write("#");
                                    Console.SetCursorPosition(53, progressLine);
                                    Console.WriteLine(String.Format("{0:0.00}", ((float)nReadTotal / (float)fileLengths[i]) * 100f) + "%");
                                }
                            }
                            else
                                _encryptionProgress = ((float)nReadTotal / (float)fileLengths[i]) * 100f;

                            if (nReadTotal + buf.Length < fileLengths[i])
                                nRead = decStream.Read(buf, 0, buf.Length);
                            else
                                nRead = decStream.Read(buf, 0, (int)fileLengths[i] - nReadTotal);
                            if (0 == nRead) break;

                            if (filesToDecrypt != null)
                                if (filesToDecrypt.IndexOf(fileNames[i]) != -1 || filesToDecrypt.Count==0)
                                    sout.Write(buf, 0, nRead);
                            
                            nReadTotal += nRead;
                        }
                        if (_bail)
                        {
                            decStream.Close();
                            sout.Close();
                            return;
                        }
                    }
                    if (filesToDecrypt != null)
                        if (filesToDecrypt.IndexOf(fileNames[i]) != -1)
                        {
                            File.Move(Path.GetTempPath() + "\\" + Path.GetFileNameWithoutExtension(filename) + ".dcodetmp", outputPath + fileNames[i]);
                            filesToDecrypt.Remove(fileNames[i]);
                        }
                        else
                            File.Delete(Path.GetTempPath() + "\\" + Path.GetFileNameWithoutExtension(filename) + ".dcodetmp");

                }
                decStream.Close();
                
                
            }
        }
    }
}
