using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Community.CsharpSqlite;
using System.Reflection;
using System.Runtime.InteropServices;
using WinapiMarshal;
using System.ComponentModel;
#if XP
using VSS;
using System.Threading;
#endif
namespace DatIndexParser
{
    public class StringValue : System.Attribute
    {
        private string _value;

        public StringValue(string value)
        {
            _value = value;
        }

        public string Value
        {
            get { return _value; }
        }

    }
    public class Program
    {

        //Size of struct is variable bytesize 0x7F / block, unless there is not enough room then it doubles
        // until the size accomodates
        // proper Internet Cache structure is at 
        // http://msdn.microsoft.com/en-us/library/aa385134(VS.85).aspx
        static DatFile dat;
        //static Database conn = new Database("temp");
        static Database connSafari = new Database("temp");
        static Database connIE = new Database("temp");
        static Database connFF = new Database("temp");
        static Database connChrome = new Database("temp");
        static Database connIe10 = new Database("temp");
        static WinapiMarshal.IVssBackupComponents iVss;
#if XP
        static VSS.IVssAsync iVssAsyncXp;
#endif
        static long numDeletedSnapshots;
        static Guid deletedSnapShotId;
        static Guid vssId;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.FILETIME ftLastWriteTime;
            public int nFileSizeHigh;
            public int nFileSizeLow;
            public int dwReserved0;
            public int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            // not using this
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternate;
        }
        struct HashTable
        {
            public int length;
            public int nextTable;
            public List<int> recordPositions;
        }
        struct DatFile
        {
            public string datFileLocation;
            public List<string> cacheDirectories;
            public List<HashTable> hashTables;
            public List<URLBlock> urlBlocks;
            public List<REDRBlock> redrBlock;
        }
        struct LEAKBlock
        {
            public string header;
            public int length;
        }
        struct REDRBlock
        {
            public string header;
            public string URL;
        }
        struct URLBlock
        {
            /*
             * Key:
             * SS = Same file with the same value for this field in each unique entry
             * DS = Different files with this entry the same value
             * SD = Same file with a different value for this field in each unique entry
             * DD = Different files have a different value for this field
             */
            //public unsafe fixed char header[4];                // 0x00 - 0x03 URL or REDR
            public int length;                // 0x04 - 0x07 Length in blocks (1 block = 0x80 bytes)
            public string lastModified;          // 0x08 - 0x0F Little-Endian DateTime
            public string lastAccessed;          // 0x10 - 0x17 Little-Endian DateTime
            public long startOfEntryBlock;     // 0x30 - 0x33 Not null 0x00000060 SD,SS
            public long startOfURLBlock;       // 0x34 - 0x37 Offset to entry strings
            public byte cacheFolderNumber;   // 0x38 - 0x39 Number of folder contained in Content.IE5, in which the file 
            //              specified at by offset value at 0x3C (i.e. 1 for the first folder
            //              and 0x05 for the fifth) 
            public int fileNameOffset;        // 0x3C - 0x3F Offset of downloaded filename (null if none)
            public byte localCacheIndex;        //0x38 - 0x39 local directory where the cache files are kept
            public long httpHeaderOffset;      // 0x44 - 0x47 Offset to HTTP headers (if none, value is null)
            public string url;                 // Self-explanatory null terminated
            public string fileName;            // beginning of block + fileNameOffset null terminated
            public string httpHeaders;         // beginning of block + httpHeaderOffset
            // 0x?? - End of Entry Block - filled with padding variable until block size is even
        }

        struct _FILETIME
        {
            public UInt32 dwLowDateTime;
            public UInt32 dwHighDateTime;
        }
        static void Usage()
        {
            Console.WriteLine("InternetHistoryParser for Chrome, Safari, Internet Explorer 6-11 and Firefox history files:");
            Console.WriteLine("Output is in SQLite3 Database Format");
            Console.WriteLine("Usage is:");
            Console.WriteLine("InternetHistoryParser.exe <directory of data files> <append to existing file?>");
            Console.WriteLine("InternetHistoryParser.exe \"C:\\IndexDirectory\\Contents\" yes");
            Console.WriteLine("");
        }
        enum _VSS_OBJECT_TYPE
        {
            VSS_OBJECT_UNKNOWN = 0,
            VSS_OBJECT_NONE = VSS_OBJECT_UNKNOWN + 1,
            VSS_OBJECT_SNAPSHOT_SET = VSS_OBJECT_NONE + 1,
            VSS_OBJECT_SNAPSHOT = VSS_OBJECT_SNAPSHOT_SET + 1,
            VSS_OBJECT_PROVIDER = VSS_OBJECT_SNAPSHOT + 1,
            VSS_OBJECT_TYPE_COUNT = VSS_OBJECT_PROVIDER + 1
        }
        static void Main(string[] args)
        {
            //Try to add a line at the beginning of each file to indicate the start of a new index.dat file 
            //remember to pad any additional entries out to the nearest 0x7F bytes
            //TODO: This is shit, take it out and do the marshalling yourself using the interop assistant
            byte[] dat = InternetHistoryParser.Properties.Resources.Interop_VSS;
            using (FileStream fs = new FileStream("Interop.VSS.dll", FileMode.Create))
            {
                fs.Write(dat, 0, dat.Length);
            }
            
            if (args.Length < 2)
            {
                Console.WriteLine("Wrong number of arguments");
                Usage();
                return;
            }
            if (args[1].ToLower() == "yes")
            {
                Database.createNew = false;

            }
            else if (args[1].ToLower() == "no")
            {
                Database.createNew = true;
            }

            connSafari.main_init();
            connSafari.CreateMemory();
            connSafari.Create("safari", new string[]{"intIndex INTEGER PRIMARY KEY ASC",
                            "url TEXT","visited DATETIME","redirected TEXT","visitCount INTEGER","title TEXT","filename TEXT"});
            connIE.Create("ie", new string[]{"intIndex INTEGER PRIMARY KEY ASC",
                            "url TEXT","accessed DATETIME","modified DATETIME","header TEXT","cachedDir TEXT","file TEXT","filename TEXT"});
            connChrome.Create("chrome", new string[]{"intIndex INTEGER PRIMARY KEY ASC",
                            "url TEXT","accessed DATETIME","modified DATETIME","type TEXT","title TEXT","visitCount TEXT","hidden TEXT","downloadComplete TEXT","keywords TEXT","filename TEXT"});
            connFF.Create("ff", new string[]{"intIndex INTEGER PRIMARY KEY ASC",
                            "url TEXT","accessed DATETIME","modified DATETIME","visitCount INTEGER","type TEXT","title TEXT","hidden TEXT","downloadComplete TEXT","filename TEXT"});
            connIe10.Create("ie10", new string[]{"intIndex INTEGER PRIMARY KEY ASC",
                            "url TEXT","accessed DATETIME","creation DATETIME","modified DATETIME","redirectUrl TEXT","filename TEXT"});

            string dir = "";
            try
            {
                DirectoryInfo di = new DirectoryInfo(args[0]);
                //DirectoryInfo di = new DirectoryInfo("C:\\");
                dir = di.FullName;
            }
            catch (Exception ec)
            {
                Usage();
            }
            Console.WriteLine("Parsing Internet History on directory: " + dir);
            if (dir.EndsWith(@"\"))
            {
                try
                {
                    dir=dir.Remove(dir.Length - 1, 1);
                }
                catch
                {
                    Console.WriteLine(@"Please use full path instead of relative paths, i.e. .. . ..\..");
                }
            }
            string vss = GetVSSFile(dir);
            if (vss.Contains(".NET error was: Exception from HRESULT: 0x80042316") ||
                vss.Contains(".NET error was: Exception from HRESULT: 0x80042308"))
            {
                FindInternetHistory(dir);
            }
            else
            {
                Console.WriteLine("VSS Image is: " + vss);
                dir = dir.Remove(0, 2);
                FindInternetHistory(vss + dir);

                if (iVss != null)
                {
                    try
                    {
                        iVss.DeleteSnapshots(vssId, (int)_VSS_OBJECT_TYPE.VSS_OBJECT_SNAPSHOT_SET, false, out numDeletedSnapshots, out deletedSnapShotId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Warning: VSS Snapshot not deleted");
                    }
                }
            }
            Console.WriteLine("Finished, merging memory databases now...");
            connSafari.MemoryMerge();
            Console.WriteLine("<<<Work complete, happy hunting>>>");
            
        }
        #region vss
        // See webpage "http://support.microsoft.com/default.aspx?scid=KB;en-us;q167296"
        // Get 64-bit filetime structure
        // http://support.microsoft.com/kb/188768
        // Remember to Pass the hex in Little Endian format
        /// <summary>
        /// Get Microsoft time from binary date
        /// </summary>
        /// <param name="value">binary date value</param>
        /// <returns>Time string</returns>
        public static string GetTime(long value)
        {
            long resultLE = value;
            if (value < 1)
                return "";
            // Value of the year 1600 as a long
            long msDate = 504911232000000000;
            // Add starting point to the current long year
            resultLE += msDate;
            try
            {
                DateTime dt = new DateTime(resultLE);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return "";
            }

        }
        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }
        static string GetVSSFile(string dir)
        {
            int VSS_S_ASYNC_FINISHED = 0x0004230A;
            try
            {
                if (Environment.OSVersion.Version.Major == 6)
                {
#if !XP
                    Functions winapi = new Functions();
                    
                    int err1 = Functions.CreateVssBackupComponents(out iVss);
                    err1 = iVss.InitializeForBackup(null);
                    WinapiMarshal.IVssAsync iVssA = null;
                    iVss.GatherWriterMetadata(out iVssA);
                    err1 = iVssA.Wait(0);
                    
                    err1 = iVss.StartSnapshotSet(out vssId);
                    Guid snapId;
                    var d = Directory.GetDirectoryRoot(dir);
                    err1 = iVss.AddToSnapshotSet(d, Guid.Empty, out snapId);
                    err1 = iVss.SetBackupState(false, false, 5, false);
                    WinapiMarshal.IVssAsync iVssAprep;
                    err1 = iVss.PrepareForBackup(out iVssAprep);
                    err1 = iVssAprep.Wait(0);
                    WinapiMarshal.IVssAsync iVssABack;
                    err1 = iVss.DoSnapshotSet(out iVssABack);
                    err1 = iVssABack.Wait(0);
                    //iVss.DeleteSnapshots(vssId, (int)VSS._VSS_OBJECT_TYPE.VSS_OBJECT_SNAPSHOT_SET, false, out numDeletedSnapshots, out deletedSnapShotId);
                    
                    VssSnapshotProperties vssProps = winapi.GetSnapshotProperties(iVss, snapId);
                    if (err1 != 0)
                    {
                        return d;
                    }


                    return vssProps.SnapshotDeviceObject;
#endif
                }
                else if (Environment.OSVersion.Version.Major < 6)
                {
                    

#if XP
                    VSSCoordinatorClass vss = new VSSCoordinatorClass();
                    VSS.IVssCoordinator iVss = (VSS.IVssCoordinator)vss;
                    VSS.IVssAdmin iVssA = (VSS.IVssAdmin)vss;
                    iVssA.AbortAllSnapshotsInProgress();
                    iVss.StartSnapshotSet(out vssId);
                    Guid snapId;
                    var d = Directory.GetDirectoryRoot(dir);
                    iVss.AddToSnapshotSet(d, Guid.Empty, out snapId);
                    object callback = null;

                    iVss.DoSnapshotSet(callback, out iVssAsyncXp);
                    while (true)
                    {

                        int hr, x = 0;

                        iVssAsyncXp.QueryStatus(out hr, ref x);

                        Console.Write(".");

                        if (hr == VSS_S_ASYNC_FINISHED)

                            break;

                        Thread.Sleep(1000);

                    }
                    VSS._VSS_SNAPSHOT_PROP snapshotProps;

                    iVss.GetSnapshotProperties(snapId, out snapshotProps);

                    return snapshotProps.m_pwszSnapshotDeviceObject;
                    //if (err1 != 0)
                    //{
                    //    return d;
                    //}

#endif
                }
                throw new NotImplementedException("Your Windows version is older than XP, consider upgrading...");
            }
            catch (Exception ex)
            {
                Win32Exception exe = new Win32Exception(Marshal.GetLastWin32Error());
                return String.Format(".NET error was: {0}\nWin32 error was: {1}", ex.Message, exe.Message);
            }

        }
        #endregion
        #region internetexplorer
        static void ParseInternetExplorer(string filename)
        {

            dat = new DatFile();
            dat.datFileLocation = filename;
            dat.cacheDirectories = new List<string>();
            dat.hashTables = new List<HashTable>();
            dat.redrBlock = new List<REDRBlock>();
            dat.urlBlocks = new List<URLBlock>();
            Functions funcs = new Functions();
            IntPtr hFile = funcs.pFuncCreateFile(filename, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (hFile != IntPtr.Zero && hFile.ToInt32() > 0)
            {
                uint bytesRead = 0;
                IntPtr sizeHigh = IntPtr.Zero;
                uint size = GetFileSize(hFile, sizeHigh);
                byte[] buffer = new byte[size];
                funcs.pFuncReadFile(hFile, buffer, size, out bytesRead, IntPtr.Zero);
                if (bytesRead < 1)
                {
                    funcs.pFuncCloseHandle(hFile);
                    return;
                }
                if (bytesRead != size)
                {
                    funcs.pFuncCloseHandle(hFile);
                    return;
                }
                funcs.pFuncCloseHandle(hFile);
                AddCacheDirectories(ref buffer);
                int firstHashTableLocation = (buffer[0x23] << 24)
                    | (buffer[0x22] << 16)
                    | (buffer[0x21] << 8)
                    | (buffer[0x20]);
                if (firstHashTableLocation + 7 >= buffer.Length || firstHashTableLocation == 0)
                {
                    return;
                }
                int firstHashTableLength = (buffer[firstHashTableLocation + 7] << 24)
                    | (buffer[firstHashTableLocation + 6] << 16)
                    | (buffer[firstHashTableLocation + 5] << 8)
                    | (buffer[firstHashTableLocation + 4]);
                if (firstHashTableLength * 0x80 >= buffer.Length)
                {
                    return;
                }
                byte[] block = new byte[firstHashTableLength * 0x80];
                Array.Copy(buffer, firstHashTableLocation, block, 0, block.Length);
                int retVal = ParseHashTables(ref block, ref buffer);
                while (retVal != 0)
                {
                    int nextHashTableLength = (buffer[retVal + 7] << 24)
                        | (buffer[retVal + 6] << 16)
                        | (buffer[retVal + 5] << 8)
                        | (buffer[retVal + 4]);
                    if (nextHashTableLength * 0x80 >= buffer.Length)
                    {
                        return;
                    }
                    block = new byte[nextHashTableLength * 0x80];
                    Array.Copy(buffer, retVal, block, 0, block.Length);
                    retVal = ParseHashTables(ref block, ref buffer);
                }
                foreach (HashTable hash in dat.hashTables)
                {
                    for (int i = 0; i < hash.recordPositions.Count; i++)
                    {
                        int recordLength = ((buffer[hash.recordPositions[i] + 7] << 24)
                            | (buffer[hash.recordPositions[i] + 6] << 16)
                            | (buffer[hash.recordPositions[i] + 5] << 8)
                            | (buffer[hash.recordPositions[i] + 4])) * 0x80;
                        if (recordLength >= buffer.Length || recordLength <= 0)
                        {
                            continue;
                        }
                        block = new byte[recordLength];
                        Array.Copy(buffer, hash.recordPositions[i], block, 0, block.Length);
                        ParseBlockTables(ref block, filename);
                    }
                }
            }
        }

        /// <summary>
        /// Reads individual blocks of an index.dat file
        /// </summary>
        /// <param name="sr">index.dat BinaryReader</param>
        static void AddCacheDirectories(ref byte[] buffer)
        {
            byte[] cacheName = new byte[8];
            if (buffer[0x50] != 0x00 && buffer.Length > 1024)
            {
                for (int i = 0x50; i < buffer.Length - 12; i += 12)
                {
                    System.Text.Encoding enc = System.Text.Encoding.ASCII;
                    byte[] myByteArray = new byte[8];
                    Array.Copy(buffer, i, myByteArray, 0, 8);
                    string myString = enc.GetString(myByteArray);
                    dat.cacheDirectories.Add(myString);
                    if (buffer[i + 8] == 0)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Parse the URL or REDR block for information
        /// </summary>
        /// <param name="buffer">Block of index.dat file</param>
        static void ParseBlockTables(ref byte[] block, string filename)
        {
            System.Text.Encoding enc = System.Text.Encoding.ASCII;
            byte[] myByteArray = new byte[4];
            Array.Copy(block, 0, myByteArray, 0, 4);
            string myString = enc.GetString(myByteArray);

            if (myString == "REDR")
            {
                try
                {
                    ParseREDRTables(ref block, filename);
                }
                catch (Exception ex)
                {

                }

            }
            else if (myString == "URL ")
            {
                try
                {
                    ParseURLTables(ref block, filename);
                }
                catch (Exception ex)
                {

                }

            }
        }
        static void ParseURLTables(ref byte[] block, string filename)
        {
            URLBlock url = new URLBlock();
            int err = 0;
            url.cacheFolderNumber = block[0x38];
            //Parse last modified
            byte[] lm = { block[0x0F], block[0x0E], block[0x0D], block[0x0C], block[0x0B], block[0x0A], block[0x09], block[0x08] };
            url.lastModified = TimeStamp(lm);

            //Parse last accessed
            byte[] la = { block[0x17], block[0x16], block[0x15], block[0x14], block[0x13], block[0x12], block[0x11], block[0x10] };
            url.lastAccessed = TimeStamp(la);

            //Get URL Offset
            byte[] bo = { block[0x37], block[0x36], block[0x35], block[0x34] };
            url.startOfURLBlock = GetOffset(bo);

            //Get filenameOffset
            byte[] fo = { block[0x3F], block[0x3E], block[0x3D], block[0x3C] };
            url.startOfEntryBlock = GetOffset(fo);

            //Get HTTP header offset
            byte[] ho = { block[0x47], block[0x46], block[0x45], block[0x44] };
            url.httpHeaderOffset = GetOffset(ho);

            int counter = (int)url.startOfURLBlock;
            byte isNull = 0x00;
            string urlUrl = "";
            do
            {

                urlUrl += (char)block[counter];
                isNull = block[counter];
                counter++;
            }
            while (isNull != 0);
            counter = (int)url.httpHeaderOffset;
            string urlHeader = "";
            isNull = 0x00;
            do
            {
                urlHeader += (char)block[counter];
                isNull = block[counter];
                counter++;
            }
            while (isNull != 0);
            counter = (int)url.startOfEntryBlock;
            string urlFile = "";
            isNull = 0x00;
            do
            {
                urlFile += (char)block[counter];
                isNull = block[counter];
                counter++;
            }
            while (isNull != 0);
            string strHeader = urlHeader;
            strHeader = System.Text.RegularExpressions.Regex.Replace(strHeader, @"[^\u0020-\u007E]", "");
            url.httpHeaders = strHeader;

            string strFile = urlFile;
            strFile = System.Text.RegularExpressions.Regex.Replace(strFile, @"[^\u0020-\u007E]", "");
            url.fileName = strFile;

            string strUrl = urlUrl;
            strUrl = System.Text.RegularExpressions.Regex.Replace(strUrl, @"[^\u0020-\u007E]", "");
            url.url = strUrl;
            if (dat.cacheDirectories.Count > 0)
            {
                connIE.Insert("ie", new string[] {"NULL",strUrl,url.lastAccessed,url.lastModified,strHeader,
                                dat.cacheDirectories[url.cacheFolderNumber], strFile,filename});
            }
            else
            {
                connIE.Insert("ie", new string[] { "NULL", strUrl, url.lastAccessed, url.lastModified, strHeader, "NULL", strFile, filename });
            }
        }
        static void ParseREDRTables(ref byte[] block, string filename)
        {
            REDRBlock redr = new REDRBlock();
            int err = 0;
            int counter = 0x10;
            byte isNull = 0x00;
            string urlUrl = "";
            do
            {

                urlUrl += (char)block[counter];
                isNull = block[counter];
                counter++;
            }
            while (isNull != 0);

            string writeAble = urlUrl;
            writeAble = System.Text.RegularExpressions.Regex.Replace(writeAble, @"[^\u0020-\u007E]", "");
            redr.URL = writeAble;
            //"url TEXT","accessed DATETIME","modified TEXT","header TEXT","cachedDir TEXT","file TEXT","filename TEXT"});

            connIE.Insert("ie", new string[] { "NULL", writeAble, "NULL", "NULL", "REDIRECT", "NULL", "NULL", filename });
        }
        static int ParseHashTables(ref byte[] block, ref byte[] buffer)
        {
            HashTable hash = new HashTable();
            hash.recordPositions = new List<int>();
            int ptrNextHash = (block[11] << 24)
                | (block[10] << 16)
                | (block[9] << 8)
                | (block[8]);
            if (ptrNextHash == 0 || ptrNextHash == 0x0BADF00D)
            {
                hash.nextTable = 0;
                GetRecordPositions(ref block, ref hash, ref buffer);
            }
            else
            {
                hash.nextTable = (int)ptrNextHash;
                GetRecordPositions(ref block, ref hash, ref buffer);
            }


            dat.hashTables.Add(hash);
            return hash.nextTable;
        }
        static void GetRecordPositions(ref byte[] block, ref HashTable hash, ref byte[] buffer)
        {
            for (int i = 0x10; i <= block.Length - 8; i += 8)
            {
                int validRecord = (block[i + 3] << 24)
                    | (block[i + 2] << 16)
                    | (block[i + 1] << 8)
                    | (block[i]);
                if (validRecord == 1 || validRecord != 3)
                {
                    int ptrActivityRecord = (block[i + 7] << 24)
                    | (block[i + 6] << 16)
                    | (block[i + 5] << 8)
                    | (block[i + 4]);
                    if (ptrActivityRecord != 0x3 && ptrActivityRecord > 0 && ptrActivityRecord < buffer.Length)
                    {
                        if (buffer[ptrActivityRecord] == 0x55
                            | buffer[ptrActivityRecord] == 0x52
                            | buffer[ptrActivityRecord] == 0x4C)
                        {
                            hash.recordPositions.Add(ptrActivityRecord);
                        }
                    }
                }
            }
        }
        static long GetOffset(byte[] buffer)
        {
            long offset = hextolong(ByteArrayToString(buffer));

            return offset;
        }
        static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }
        static long hextolong(string hexValue)
        {

            return long.Parse(hexValue, NumberStyles.AllowHexSpecifier);
        }
        /// <summary>
        /// Gets a timestamp from a byte[] value
        /// </summary>
        /// <param name="buffer">binary time input</param>
        /// <returns>Timestamp string</returns>
        static string TimeStamp(byte[] buffer)
        {
            string retVal = "";

            string longtime = ByteArrayToString(buffer);
            long longlongtime = hextolong(longtime);
            retVal = GetTime(longlongtime);

            return retVal;
        }
        #endregion
        #region firefox
        public enum VisitType
        {
            [StringValue("FollowedLink")]
            TRANSITION_LINK = 1,
            [StringValue("TypedInURL")]
            TRANSITION_TYPED = 2,
            [StringValue("FollowedBookmark")]
            TRANSITION_BOOKMARK = 3,
            [StringValue("IFRAME")]
            TRANSITION_EMBED = 4,
            [StringValue("PermanentRedirect")]
            TRANSITION_REDIRECT_PERMANENT = 5,
            [StringValue("TemporaryRedirect")]
            TRANSITION_REDIRECT_TEMPORARY = 6,
            [StringValue("Download")]
            TRANSITION_DOWNLOAD = 7
        }
        public struct FFHistoryEntry
        {
            public string[] colNames;
            public string[] results;
            public int GetColCount()
            {
                if (colNames != null)
                {
                    return colNames.Length;
                }
                else
                    return 0;
            }
            public int GetResultCount()
            {
                if (results != null)
                {
                    return results.Length;
                }
                else
                    return 0;
            }
        }
        static void ParseFirefox(string filename, int which)
        {

            FFHistoryEntry results;
            //"url ","access ","mod","visitCount ","type ","hidden ","filename "});
            if (which == 11)
            {
                //"url ","accessed ","modified ","visitCount ","type ","title ","hidden ","downloadComplete ","filename "});
                string[] colNames = { "moz_historyvisits.visit_date", "moz_places.url", "moz_historyvisits.visit_type", "moz_places.last_visit_date", "moz_places.visit_count", "moz_places.title" };
                connFF.AttachFFHistoryFile(filename, colNames, out results);
                for (int i = colNames.Length; i < results.GetResultCount(); i += colNames.Length)
                {
                    string output = "";
                    VisitType visit = (VisitType)Int32.Parse(results.results[i + 2]);
                    Type type = visit.GetType();
                    FieldInfo fi = type.GetField(visit.ToString());
                    StringValue[] attrs =
                       fi.GetCustomAttributes(typeof(StringValue),
                                               false) as StringValue[];
                    if (attrs.Length > 0)
                    {
                        output = attrs[0].Value;
                    }
                    //"url ","accessed ","modified ","visitCount ","type ","title ","hidden ","downloadComplete ","filename "});
                    string[] resultRow = new string[] { "NULL", results.results[i + 1], results.results[i], results.results[i + 3], results.results[i + 4], output, results.results[i + 5], "NO", "NULL", filename };
                    connFF.Insert("ff", resultRow);
                }
                colNames = new string[] { "moz_places.url", "moz_places.visit_count", "moz_places.hidden" };
                connFF.AttachFFHistoryFileEmbedded(filename, colNames, out results);
                for (int i = colNames.Length; i < results.GetResultCount(); i += colNames.Length)
                {
                    //"url ","accessed ","modified ","visitCount ","type ","title ","hidden ","downloadComplete ","filename "});
                    string[] resultRow = new string[] { "NULL", results.results[i], "NULL", "NULL", "NULL", "Archive", results.results[i + 1], "YES", "NULL", filename };
                    int rc = connFF.Insert("ff", resultRow);
                }
            }
            if (which == 12)
            {
                //"url TEXT","accessed DATETIME","modified DATETIME","visitCount INTEGER","type TEXT","title TEXT","hidden TEXT","downloadComplete TEXT","filename TEXT"});
                string[] colNamesDownloads = { "moz_downloads.source", "moz_downloads.startTime", "moz_downloads.endTime", "moz_downloads.name", "moz_downloads.currBytes", "moz_downloads.maxBytes" };
                connFF.AttachFFHistoryFileDownloads(filename, colNamesDownloads, out results);
                for (int i = colNamesDownloads.Length; i < results.GetResultCount(); i += colNamesDownloads.Length)
                {
                    if (results.results[i] == null && results.results[i + 1] == null)
                    {
                        continue;
                    }
                    UInt64 maxBytes = 0;
                    UInt64.TryParse(results.results[i + 5], out maxBytes);
                    UInt64 currBytes = 0;
                    UInt64.TryParse(results.results[i + 4], out currBytes);
                    string fullyDownloaded = "YES";
                    if (maxBytes - currBytes == 0)
                    {

                    }
                    else
                    {
                        fullyDownloaded = "NO";
                    }
                    string[] resultRow = new string[] { "NULL", results.results[i], results.results[i + 1], results.results[i + 2], "NULL", "Download", results.results[i + 3], "NULL", fullyDownloaded, filename };
                    int rc = connFF.Insert("ff", resultRow);
                }
            }
        }




        #endregion
        #region chrome
        public enum ChromeVisitType
        {
            [StringValue("FollowedLink")]
            LINK = 0,
            [StringValue("TypedInURL")]
            TYPED = 1,
            [StringValue("FollowedBookmark")]
            AUTO_BOOKMARK = 2,
            [StringValue("AutomaticIFRAME")]
            AUTO_SUBFRAME = 3,
            [StringValue("ManualIFRAME")]
            MANUAL_SUBFRAME = 4,
            [StringValue("KeywordGeneratedURL")]
            GENERATED = 5,
            [StringValue("StartPage")]
            START_PAGE = 6,
            [StringValue("FormSubmit")]
            FORM_SUBMIT = 7,
            [StringValue("Reload")]
            RELOAD = 8,
            [StringValue("TabKeywordGeneratedURL")]
            KEYWORD = 9,
            [StringValue("KeywordGeneratedVisit")]
            KEYWORD_GENERATED = 10,
            [StringValue("Unknown")]
            UNKNOWN = 9001,
            [StringValue("Download")]
            DOWNLOAD = 9002
        }
        public struct ChromeHistoryEntry
        {
            public string[] colNames;
            public string[] results;
            public int GetColCount()
            {
                if (colNames != null)
                {
                    return colNames.Length;
                }
                else
                    return 0;
            }
            public int GetResultCount()
            {
                if (results != null)
                {
                    return results.Length;
                }
                else
                    return 0;
            }
        }
        static void ParseChrome(string filename, int which)
        {
            bool isArchive = true;
            ChromeHistoryEntry results;
            if (which == 1)
            {
                string[] colNames = { " visits.visit_time", "urls.url", "visits.transition", "urls.last_visit_time", "term", "title", "visit_count", "hidden" };
                connChrome.AttachChromeHistoryFile(filename, colNames, out results);
                for (int i = colNames.Length; i < results.GetResultCount(); i += colNames.Length)
                {
                    if (results.results[i] == null && results.results[i + 1] == null && results.results[i + 2] == null && results.results[i + 3] == null)
                    {
                        continue;
                    }
                    string output = "";
                    uint parseResult = 0;
                    ChromeVisitType visit;
                    if (UInt32.TryParse(results.results[i + 2], out parseResult))
                    {
                        visit = (ChromeVisitType)(UInt32.Parse(results.results[i + 2]) & 0xFF);
                    }
                    else
                    {
                        visit = ChromeVisitType.UNKNOWN;
                    }
                    Type type = visit.GetType();
                    FieldInfo fi = type.GetField(visit.ToString());
                    StringValue[] attrs =
                       fi.GetCustomAttributes(typeof(StringValue),
                                               false) as StringValue[];
                    if (attrs.Length > 0)
                    {
                        output = attrs[0].Value;
                    }
                    //title,visit_count,hidden
                    //"url",                "accessed ",        "modified ",            "type ","archived ","title ","visitCount","hidden",","downloadComplete ","keywords ","filename "});
                    if (results.results[i + 4] == null)
                    {
                        results.results[i + 4] = "NULL";
                    }
                    if (results.results[i + 5] == null)
                    {
                        results.results[i + 5] = "NULL";
                    }
                    if (results.results[i + 7] == "0")
                    {
                        results.results[i + 7] = "NO";
                    }
                    else
                    {
                        results.results[i + 7] = "YES";
                    }
                    string[] resultRow = new string[] { "NULL", results.results[i + 1], results.results[i], results.results[i + 3], output, results.results[i + 5], results.results[i + 6],
                    results.results[i + 7],"NULL",results.results[i + 4], filename };
                    connChrome.Insert("chrome", resultRow);
                    isArchive = false;
                }

                //SELECT datetime(( downloads.start_time - 11644473600000000 ) / 1000000 ,'unixepoch'),datetime(( downloads.end_time - 11644473600000000 ) / 1000000 ,'unixepoch'),
                //downloads.url,downloads.full_path,downloads.received_bytes,downloads.total_bytes FROM downloads;
                string[] colNamesDownloads = { "downloads.start_time", "downloads.end_time", "downloads.url", "downloads.full_path", "downloads.received_bytes", "downloads.total_bytes" };
                connChrome.AttachChromeHistoryFileDownloads(filename, colNamesDownloads, out results);
                for (int i = colNamesDownloads.Length; i < results.GetResultCount(); i += colNamesDownloads.Length)
                {
                    if (results.results[i] == null && results.results[i + 1] == null && results.results[i + 2] == null && results.results[i + 3] == null)
                    {
                        continue;
                    }
                    string output = "";
                    ChromeVisitType visit = ChromeVisitType.DOWNLOAD;
                    Type type = visit.GetType();
                    FieldInfo fi = type.GetField(visit.ToString());
                    StringValue[] attrs =
                       fi.GetCustomAttributes(typeof(StringValue),
                                               false) as StringValue[];
                    if (attrs.Length > 0)
                    {
                        output = attrs[0].Value;
                    }
                    //title,visit_count,hidden
                    //"url",                "accessed ",        "modified ",            "type ","archived ","title ","visitCount","hidden","downloadComplete ","keywords ","filename "});
                    //SELECT datetime(( downloads.start_time - 11644473600000000 ) / 1000000 ,'unixepoch'),datetime(( downloads.end_time - 11644473600000000 ) / 1000000 ,'unixepoch'),
                    //downloads.url,downloads.full_path,downloads.received_bytes,downloads.total_bytes FROM downloads;

                    UInt64 received = 0;
                    UInt64.TryParse(results.results[i + 4], out received);
                    UInt64 total = 0;
                    UInt64.TryParse(results.results[i + 5], out total);
                    string fullyDownloaded = "YES";
                    if (received - total == 0)
                    {

                    }
                    else
                    {
                        fullyDownloaded = "NO";
                    }
                    string[] resultRow = new string[] { "NULL", results.results[i + 2], results.results[i], results.results[i + 1], output, results.results[i + 3],"NULL",
                    "NULL",fullyDownloaded,"NULL", filename };
                    connChrome.Insert("chrome", resultRow);
                    isArchive = false;
                }
            }
            if (which == 2)
            {
                //"url",                "accessed ",        "modified ",            "type ","title ","visitCount","hidden","downloadComplete ","keywords ","filename "});
                string[] colNamesArchived = { " info.time", "pages_content.c0url", "pages_content.c1title" };
                connChrome.AttachChromeHistoryFileArchive(filename, colNamesArchived, out results);
                for (int i = colNamesArchived.Length; i < results.GetResultCount(); i += colNamesArchived.Length)
                {
                    if (results.results[i] == null && results.results[i + 1] == null)
                    {
                        continue;
                    }

                    if (results.results[i + 2] == null)
                    {
                        results.results[i + 2] = "NULL";
                    }
                    string[] resultRow = new string[] { "NULL", results.results[i + 1], results.results[i], "NULL", "Archive", results.results[i + 2], "Unknown", "Unknown", "NULL", "NULL", filename };
                    connChrome.Insert("chrome", resultRow);
                }
            }
            if (which == 3)
            {
                string[] colNamesCookies = { "cookies.host_key", "cookies.creation_utc", "cookies.last_access_utc", "cookies.name" };
                connChrome.AttachChromeHistoryFileCookies(filename, colNamesCookies, out results);
                for (int i = colNamesCookies.Length; i < results.GetResultCount(); i += colNamesCookies.Length)
                {
                    if (results.results[i] == null && results.results[i + 1] == null)
                    {
                        continue;
                    }

                    if (results.results[i + 2] == null)
                    {
                        results.results[i + 2] = "NULL";
                    }
                    string[] resultRow = new string[] { "NULL", results.results[i], results.results[i + 1], results.results[i + 2], "Cookie", results.results[i + 3], "NULL", "NULL", "NULL", "NULL", filename };
                    connChrome.Insert("chrome", resultRow);
                }
            }
        }
        #endregion
        #region safari
        public enum NodeType : byte
        {
            NULL = 0,
            INT = 1,
            FLOAT = 2,
            DATE = 3,
            BINARY = 4,
            ASCII = 5,
            UNICODE = 6,
            ARRAY = 0xa,
            DICT = 0xd
        }
        public struct ObjectData
        {
            public int offset;
            public int length;
            public NodeType dataType;
            public byte[] data;
            public string strData;
            public List<int> keys;
            public List<int> refs;
        }
        [DllImport("kernel32.dll")]
        static extern uint GetFileSize(IntPtr hFile, IntPtr lpFileSizeHigh);
        private static void ParseSafari(string filename)
        {
            byte[] buffer;
            byte offsetSize;
            byte offsetRef;
            int topObject = 0;
            int numObjects = 0;
            int offsetTableOffset = 0;
            List<int> offsets;
            List<ObjectData> objs = new List<ObjectData>();
            byte[] offsetTable;
            uint size = 0;
            IntPtr sizeHigh = IntPtr.Zero;
            Functions funcs = new Functions();
            IntPtr hFile = funcs.pFuncCreateFile(filename, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (hFile != IntPtr.Zero && hFile.ToInt32() > 0)
            {
                uint bytesRead = 0;
                size = GetFileSize(hFile, sizeHigh);
                buffer = new byte[size];
                funcs.pFuncReadFile(hFile, buffer, size, out bytesRead, IntPtr.Zero);
                if (bytesRead < 1)
                {
                    funcs.pFuncCloseHandle(hFile);
                    return;
                }

                //Check if it's a history file
                byte[] checkHistoryStringBytes = new byte[0xFF];
                if (size <= 0xFF)
                {
                    return;
                }
                Array.Copy(buffer, 0, checkHistoryStringBytes, 0, 0xFF);
                System.Text.ASCIIEncoding asciiCheck = new ASCIIEncoding();
                string historyString = asciiCheck.GetString(checkHistoryStringBytes);
                if (!historyString.Contains("WebHistoryFileVersion"))
                {
                    funcs.pFuncCloseHandle(hFile);
                    return;
                }

                //Read Trailer
                byte[] trailer = new byte[32];
                int offsetTrailer = (int)(size - 32);
                Array.Copy(buffer, offsetTrailer, trailer, 0, trailer.Length);
                offsetSize = trailer[6];
                offsetRef = trailer[7];
                numObjects = trailer[12] << 24 | trailer[13] << 16 | trailer[14] << 8 | trailer[15];
                topObject = trailer[20] << 24 | trailer[21] << 16 | trailer[22] << 8 | trailer[23];
                offsetTableOffset = trailer[28] << 24 | trailer[29] << 16 | trailer[30] << 8 | trailer[31];
                offsets = new List<int>();
                //Read ObjectTable
                offsetTable = new byte[numObjects * offsetSize];
                Array.Copy(buffer, offsetTableOffset, offsetTable, 0, offsetTable.Length);

                //i controls the raw size of objects read, j controls the actual number of objects read
                for (int i = 0, j = 0; j < numObjects; i += offsetSize, j++)
                {
                    int arrObjOffset = 0;
                    for (int k = 0; k < offsetSize; k++)
                    {
                        arrObjOffset = (arrObjOffset << 8) | offsetTable[i + k];
                    }
                    offsets.Add(arrObjOffset);
                }

                //Parse objects
                if (buffer != null)
                {
                    for (int i = 0; i < numObjects; i++)
                    {
                        //byte[] fullObjectBlob = new byte[offsets[i+1] - offsets[i]];
                        ObjectData obj = new ObjectData();
                        obj.refs = new List<int>();
                        obj.keys = new List<int>();
                        obj.offset = offsets[i];

                        obj.data = null;
                        obj.length = 0;
                        //read first offset entry
                        //byte[] offData = offsets[i];
                        //int intOffset = offData[0] << 8 | offData[1];
                        obj.dataType = (NodeType)((buffer[obj.offset] & 0xF0) >> 4);
                        if (obj.dataType != NodeType.NULL)
                        {
                            if ((buffer[obj.offset] & 0xF) == 0xF)
                            {
                                if (obj.dataType == NodeType.DICT)
                                {
                                    obj.length = buffer[obj.offset + 2] * offsetRef * 2;
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 3, obj.data, 0, obj.length);
                                    for (int j = 0; j < obj.length / 2; j += offsetRef)
                                    {
                                        int refs = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            refs = refs << 8 | obj.data[j + k];
                                        }

                                        obj.refs.Add(refs);
                                    }
                                    for (int j = obj.length / 2; j < obj.length; j += offsetRef)
                                    {
                                        int keys = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            keys = keys << 8 | obj.data[j + k];
                                        }

                                        obj.keys.Add(keys);
                                    }
                                }
                                else if (obj.dataType == NodeType.ARRAY)
                                {
                                    obj.length = buffer[obj.offset + 2] * offsetRef;
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 3, obj.data, 0, obj.length);
                                    obj.refs = new List<int>();
                                    for (int j = 0; j < obj.length; j += offsetRef)
                                    {
                                        int refs = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            refs = refs << 8 | obj.data[j + k];
                                        }

                                        obj.refs.Add(refs);
                                    }
                                }
                                else if (obj.dataType == NodeType.UNICODE)
                                {
                                    obj.length = buffer[obj.offset + 2] * 2;


                                    System.Text.UnicodeEncoding uni = new UnicodeEncoding();
                                    if (buffer[obj.offset + 3] == 0x0 && buffer[obj.offset + 4] != 0x0)
                                    {
                                        obj.data = new byte[obj.length - 2];
                                        Array.Copy(buffer, obj.offset + 4, obj.data, 0, obj.length - 2);
                                    }
                                    else
                                    {
                                        obj.data = new byte[obj.length];
                                        Array.Copy(buffer, obj.offset + 3, obj.data, 0, obj.length);
                                    }
                                    obj.strData = uni.GetString(obj.data, 0, obj.data.Length);
                                }
                                else
                                {
                                    obj.length = buffer[obj.offset + 2];
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 3, obj.data, 0, obj.length);
                                }
                            }
                            else
                            {
                                if (obj.dataType == NodeType.DICT)
                                {
                                    obj.length = (buffer[obj.offset] & 0xf) * offsetRef * 2;
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 1, obj.data, 0, obj.length);
                                    for (int j = 0; j < obj.length / 2; j += offsetRef)
                                    {
                                        int refs = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            refs = refs << 8 | obj.data[j + k];
                                        }

                                        obj.refs.Add(refs);
                                    }
                                    for (int j = obj.length / 2; j < obj.length; j += offsetRef)
                                    {
                                        int keys = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            keys = keys << 8 | obj.data[j + k];
                                        }

                                        obj.keys.Add(keys);
                                    }
                                }
                                else if (obj.dataType == NodeType.ARRAY)
                                {
                                    obj.length = (buffer[obj.offset] & 0xf) * offsetRef;
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 1, obj.data, 0, obj.length);
                                    obj.refs = new List<int>();
                                    for (int j = 0; j < obj.length; j += offsetRef)
                                    {
                                        int refs = 0;
                                        for (int k = 0; k < offsetRef; k++)
                                        {
                                            refs = refs << 8 | obj.data[j + k];
                                        }

                                        obj.refs.Add(refs);
                                    }
                                }
                                else if (obj.dataType == NodeType.UNICODE)
                                {
                                    obj.length = buffer[obj.offset + 2] * 2;


                                    System.Text.UnicodeEncoding uni = new UnicodeEncoding();
                                    if (buffer[obj.offset + 1] == 0x0 && buffer[obj.offset + 2] != 0x0)
                                    {
                                        obj.data = new byte[obj.length - 2];
                                        Array.Copy(buffer, obj.offset + 2, obj.data, 0, obj.length - 2);
                                    }
                                    else
                                    {
                                        obj.data = new byte[obj.length];
                                        Array.Copy(buffer, obj.offset + 1, obj.data, 0, obj.length);
                                    }
                                    obj.strData = uni.GetString(obj.data, 0, obj.data.Length);
                                }
                                else
                                {
                                    obj.length = (buffer[obj.offset] & 0xf);
                                    obj.data = new byte[obj.length];
                                    Array.Copy(buffer, obj.offset + 1, obj.data, 0, obj.length);
                                }
                            }
                        }
                        if (obj.dataType == NodeType.ASCII)
                        {
                            System.Text.ASCIIEncoding ascii = new ASCIIEncoding();
                            obj.strData = ascii.GetString(obj.data, 0, obj.length);
                        }
                        else if (obj.dataType == NodeType.INT)
                        {
                            obj.strData = buffer[obj.offset + 1].ToString();
                        }

                        objs.Add(obj);
                    }
                    InsertSafariHistory(objs, offsets, filename);
                }
                funcs.pFuncCloseHandle(hFile);
            }
            return;
        }
        private static void InsertSafariHistory(List<ObjectData> objs, List<int> offsets, string filename)
        {

            List<Dictionary<string, string>> lstDict = new List<Dictionary<string, string>>();
            foreach (ObjectData obj in objs)
            {
                if (obj.dataType == NodeType.DICT)
                {
                    string url = "NULL", visited = "NULL", redirected = "NULL", visitCount = "NULL", title = "NULL";
                    //List<Dictionary<string, string>> lstDict = new List<Dictionary<string, string>>();
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    for (int i = 0, j = 0; i < obj.refs.Count && j < obj.keys.Count; i++)
                    {
                        string key = "";
                        string val = "";
                        foreach (ObjectData data in objs)
                        {
                            if (data.offset == offsets[obj.refs[i]])
                            {
                                if (data.strData == "")
                                {
                                    key = "URL";
                                }
                                else
                                {
                                    key = data.strData;
                                }
                                //if (i < obj.refs.Count-1)
                                //{
                                //    i++;
                                //}
                                continue;
                            }
                            else if (data.offset == offsets[obj.keys[i]])
                            {
                                val = data.strData;
                                if (data.dataType == NodeType.ARRAY)
                                {
                                    val = "";
                                    foreach (ObjectData item in objs)
                                    {
                                        if (offsets[data.refs[0]] == item.offset)
                                        {
                                            val = item.strData;
                                        }
                                    }

                                }
                                //if (j < obj.keys.Count - 1)
                                //{
                                //    j++;
                                //}
                                continue;
                            }
                            if (key != "" && val != "")
                            {
                                if (key == "lastVisitedDate")
                                {
                                    DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0);
                                    double test = double.Parse(val);
                                    reference = reference.AddSeconds(test);
                                    val = reference.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                                dict.Add(key, val);
                                key = "";
                                val = "";
                                break;
                            }
                        }
                    }
                    foreach (KeyValuePair<string, string> pair in dict)
                    {
                        switch (pair.Key)
                        {
                            case "URL":
                                url = pair.Value;
                                break;
                            case "lastVisitedDate":
                                visited = pair.Value;
                                break;
                            case "title":
                                title = pair.Value;
                                break;
                            case "visitCount":
                                visitCount = pair.Value;
                                break;
                            case "displayTitle":
                                break;
                            case "redirectURLs":
                                redirected = pair.Value;
                                break;
                            default:
                                break;
                        }
                    }
                    string[] resultRow = new string[] { "NULL", url, visited, redirected, visitCount, title, filename };
                    if (url != "NULL" || visited != "NULL" || redirected != "NULL" || visitCount != "NULL" || title != "NULL")
                    {
                        connSafari.Insert("safari", resultRow);
                    }
                }
            }
        }
        #endregion
        #region controller
        static byte[] magicSql = new byte[] { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33 };
        static byte[] magicIE = new byte[] { 0x43, 0x6C, 0x69, 0x65, 0x6E, 0x74, 0x20, 0x55, 0x72, 0x6C, 0x43, 0x61, 0x63, 0x68, 0x65 };
        static byte[] magicSafari = new byte[] { 0x62, 0x70, 0x6C, 0x69, 0x73, 0x74 };
        static byte[] magicEseDb = new byte[] { 0xEF, 0xCD, 0xAB, 0x89 };
        static byte[] magicComp = new byte[15];
        static void CheckAndParse(string file)
        {

            Functions funcs = new Functions();
            IntPtr hFile = funcs.pFuncCreateFile(file, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            if (hFile != IntPtr.Zero && hFile.ToInt32() > 0)
            {
                uint bytesRead = 0;
                funcs.pFuncReadFile(hFile, magicComp, 15, out bytesRead, IntPtr.Zero);
                if (bytesRead != 15)
                {
                    funcs.pFuncCloseHandle(hFile);
                    return;
                }
                funcs.pFuncCloseHandle(hFile);
            }
            System.Text.ASCIIEncoding enc = new ASCIIEncoding();
            if (enc.GetString(magicComp, 0, 15) == enc.GetString(magicSql, 0, 15))
            {
                int which = connSafari.WhichHistoryFileType(file);
                if (which >= 11 && which <= 20)
                {
                    Console.WriteLine("Found Firefox database at " + file);
                    try
                    {
                        ParseFirefox(file, which);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("****My word. This is tragic, possible corrupt database at " + file+"****");
                    }
                    Console.WriteLine("---->Finished parsing Firefox database " + file);
                    //continue;
                }
                else if (which >= 1 && which <= 10)
                {
                    Console.WriteLine("Found Chrome database at " + file);
                    try
                    {
                        ParseChrome(file, which);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("****My word. This is tragic, possible corrupt database at " + file + "****");
                    }
                    Console.WriteLine("---->Finished parsing Chrome database " + file);
                    //continue;
                }
            }
            if (enc.GetString(magicComp, 0, 15) == enc.GetString(magicIE, 0, 15))
            {
                Console.WriteLine("Found IE database, version <10 at " + file);
                try
                {
                    ParseInternetExplorer(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("****My word. This is tragic, possible corrupt database at " + file + "****");
                }
                Console.WriteLine("---->Finished parsing IE database " + file);
                //continue;
            }
            if (enc.GetString(magicComp, 0, 6) == enc.GetString(magicSafari, 0, 6))
            {
                Console.WriteLine("Found Safari database at " + file);
                try
                {
                    ParseSafari(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("****My word. This is tragic, possible corrupt database at " + file + "****");
                }
                Console.WriteLine("---->Finished parsing Safari database " + file);
                //continue;
            }
            if (magicComp[4] == magicEseDb[0] && magicComp[5] == magicEseDb[1] && magicComp[6] == magicEseDb[2] && magicComp[7] == magicEseDb[3])
            {
                Console.WriteLine("Found ESE database at " + file + " ...checking for IE10+");
                try
                {
                    Esedb db = new Esedb(file);
                    if (db.historyEntries.Count > 0)
                    {
                        Console.WriteLine("-->IE10+ ESE database confirmed at " + file + " ...parsing now");
                    }
                    foreach (Esedb.HistoryEntry entry in db.historyEntries)
                    {
                        string[] resultRow = new string[] { "NULL", entry.Url, entry.AccesedTime, entry.CreationTime, entry.ModifiedTime, entry.RedirectUrl, entry.Filename };
                        if (entry.Url != "NULL" || entry.CreationTime != "NULL")
                        {
                            connIe10.Insert("ie10", resultRow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("****My word. This is tragic, possible corrupt database at " + file + "****");
                }
                Console.WriteLine("---->Finished parsing IE10+ database " + file);
            }
        }


        #endregion
        #region scan_for_history
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindFirstFile(string lpFileName, out
                                WIN32_FIND_DATA lpFindFileData);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA
           lpFindFileData);

        IntPtr hHandle = IntPtr.Zero;

        internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        internal const int MAX_PATH = 260;

        // Assume dirName passed in is already prefixed with \\?\
        /// <summary>
        /// Recursively search a given directory for all files and folders
        /// </summary>
        /// <param name="dirName">Root directory to start in</param>
        /// <param name="os">Type of operating system</param>
        static void FindInternetHistory(string dirName)
        {
            WIN32_FIND_DATA findData;

            //Get all contents of the first directory
            IntPtr findHandle = FindFirstFile(dirName + @"\*", out findData);

            if (findHandle != INVALID_HANDLE_VALUE)
            {
                bool found;
                do
                {
                    string currentFileName = findData.cFileName;
                    // if this is a directory, find its contents
                    if (((int)findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                    {
                        if (currentFileName != "." && currentFileName != "..")
                        {
                            string path = dirName.Replace(@"\\?\", "");
                            FindInternetHistory(Path.Combine(dirName, currentFileName));

                        }

                    }

                    // it's a file; add it to the results
                    else
                    {
                        string path = Path.Combine(dirName, currentFileName);
                        CheckAndParse(path);
                    }
                    // find next file or directory
                    found = FindNextFile(findHandle, out findData);
                }
                while (found);
            }
            return;
        }
        #endregion
    }
}