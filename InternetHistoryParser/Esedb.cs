using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DatIndexParser
{
    class Esedb
    {
        
        public struct HistoryEntry
        {
            public string tablename;
            public string EntryId;
            public string ContainerId;
            public string CacheId;
            public string UrlHash;
            public string SecureDirectory;
            public string FileSize;
            public string Type;
            public string Flags;
            public string AccessCount;
            public string SyncTime;
            public string CreationTime;
            public string ExpiryTime;
            public string ModifiedTime;
            public string AccesedTime;
            public string PostCheckTime;
            public string SyncCount;
            public string ExemptionDelta;
            public string Url;
            public string Filename;
            public string FileExtension;
            public string RequestHeaders;
            public string ResponseHeaders;
            public string RedirectUrl;
            public string Group;
            public string extraData;
        }
        #region win32
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = false)]
        static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, uint dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);
        #endregion
        #region libesedb
        public enum LIBESEDB_COLUMN_TYPES : uint
        {
            LIBESEDB_COLUMN_TYPE_NULL = 0,
            LIBESEDB_COLUMN_TYPE_BOOLEAN = 1,
            LIBESEDB_COLUMN_TYPE_INTEGER_8BIT_UNSIGNED = 2,
            LIBESEDB_COLUMN_TYPE_INTEGER_16BIT_SIGNED = 3,
            LIBESEDB_COLUMN_TYPE_INTEGER_32BIT_SIGNED = 4,
            LIBESEDB_COLUMN_TYPE_CURRENCY = 5,
            LIBESEDB_COLUMN_TYPE_FLOAT_32BIT = 6,
            LIBESEDB_COLUMN_TYPE_DOUBLE_64BIT = 7,
            LIBESEDB_COLUMN_TYPE_DATE_TIME = 8,
            LIBESEDB_COLUMN_TYPE_BINARY_DATA = 9,
            LIBESEDB_COLUMN_TYPE_TEXT = 10,
            LIBESEDB_COLUMN_TYPE_LARGE_BINARY_DATA = 11,
            LIBESEDB_COLUMN_TYPE_LARGE_TEXT = 12,
            LIBESEDB_COLUMN_TYPE_SUPER_LARGE_VALUE = 13,
            LIBESEDB_COLUMN_TYPE_INTEGER_32BIT_UNSIGNED = 14,
            LIBESEDB_COLUMN_TYPE_INTEGER_64BIT_SIGNED = 15,
            LIBESEDB_COLUMN_TYPE_GUID = 16,
            LIBESEDB_COLUMN_TYPE_INTEGER_16BIT_UNSIGNED = 17
        };
        public enum LIBESEDB_VALUE_FLAGS
        {
            LIBESEDB_VALUE_FLAG_VARIABLE_SIZE = 0x01,
            LIBESEDB_VALUE_FLAG_COMPRESSED = 0x02,
            LIBESEDB_VALUE_FLAG_LONG_VALUE = 0x04,
            LIBESEDB_VALUE_FLAG_MULTI_VALUE = 0x08,
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_free(out IntPtr file, int nullable);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_get_version();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_initialize(out IntPtr file, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_open(IntPtr file, char[] filename, int access, out IntPtr err);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_open_wide(IntPtr file, [MarshalAs(UnmanagedType.LPWStr)] string filename, int access, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_get_number_of_tables(IntPtr file, out int count, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_file_get_table(IntPtr file, int index, out IntPtr table, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_free(out IntPtr table, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_number_of_records(IntPtr file, out int count, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_number_of_columns(IntPtr table, out int count, byte flags, out IntPtr err);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_utf16_name_size(
            IntPtr table,
            out int utf16_string_size,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_utf16_name(
            IntPtr table,
            IntPtr utf16_string,
            int utf16_string_size,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_column(
            IntPtr table,
            int column_entry,
            out IntPtr column,
            byte flags,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_column_get_utf8_name_size(
             IntPtr column,
             out int utf8_string_size,
             out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_column_get_utf8_name(
             IntPtr column,
             ref IntPtr utf8_string,
             int utf8_string_size,
             out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_column_get_utf16_name_size(
            IntPtr column,
            out int utf16_string_size,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_column_get_utf16_name(
            IntPtr column,
            IntPtr utf16_string,
            int utf16_string_size,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_column_free(
            out IntPtr column,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libcstring_system_string_allocate(
            int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_table_get_record(
            IntPtr table,
            int record_entry,
            out IntPtr record,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_free(
            out IntPtr record,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_column_type(
            IntPtr record,
            int value_entry,
            out LIBESEDB_COLUMN_TYPES column_type,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_number_of_values(
            IntPtr record,
            out int number_of_values,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value(
            IntPtr record,
            int value_entry,
            out IntPtr value_data,
            out int value_data_size,
            out LIBESEDB_VALUE_FLAGS value_flags,
            out IntPtr error);

        /* Retrieves the boolean value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_boolean(
            IntPtr record,
            int value_entry,
            out byte value_boolean,
            out IntPtr error);

        /* Retrieves the 8-bit value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_8bit(
            IntPtr record,
            int value_entry,
            out byte value_8bit,
            out IntPtr error);

        /* Retrieves the 16-bit value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_16bit(
            IntPtr record,
            int value_entry,
            out short value_16bit,
            out IntPtr error);

        /* Retrieves the 32-bit value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_32bit(
            IntPtr record,
            int value_entry,
            out uint value_32bit,
            out IntPtr error);

        /* Retrieves the 64-bit value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_64bit(
            IntPtr record,
            int value_entry,
            out ulong value_64bit,
            out IntPtr error);

        /* Retrieves the 64-bit filetime value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_filetime(
            IntPtr record,
            int value_entry,
            out ulong value_filetime,
            out IntPtr error);

        /* Retrieves the single precision floating point value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_floating_point_32bit(
            IntPtr record,
            int value_entry,
            out float value_floating_point_32bit,
            out IntPtr error);

        /* Retrieves the double precision floating point value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_floating_point_64bit(
            IntPtr record,
            int value_entry,
            out double value_floating_point_64bit,
            out IntPtr error);

        /* Retrieves the size of an UTF-8 encoded string a specific entry
         * The returned size includes the end of string character
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_utf8_string_size(
            IntPtr record,
            int value_entry,
            out int utf8_string_size,
            out IntPtr error);

        /* Retrieves the UTF-8 encoded string of a specific entry
         * The function uses the codepage in the column definition if necessary
         * The size should include the end of string character
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_utf8_string(
            IntPtr record,
            int value_entry,
            IntPtr utf8_string,
            int utf8_string_size,
            out IntPtr error);

        /* Retrieves the size of an UTF-16 encoded string a specific entry
         * The returned size includes the end of string character
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_utf16_string_size(
            IntPtr record,
            int value_entry,
            out int utf16_string_size,
            out IntPtr error);

        /* Retrieves the UTF-16 encoded string of a specific entry
         * The function uses the codepage in the column definition if necessary
         * The size should include the end of string character
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_utf16_string(
            IntPtr record,
            int value_entry,
            IntPtr utf16_string,
            int utf16_string_size,
            out IntPtr error);

        /* Retrieves the binary data size of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_binary_data_size(
            IntPtr record,
            int value_entry,
            out int binary_data_size,
            out IntPtr error);

        /* Retrieves the binary data value of a specific entry
         * Returns 1 if successful, 0 if value is NULL or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_value_binary_data(
            IntPtr record,
            int value_entry,
            IntPtr binary_data,
            int binary_data_size,
            out IntPtr error);

        /* Retrieves the long value of a specific entry
         * Creates a new long value
         * Returns 1 if successful, 0 if the item does not contain such value or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_long_value(
            IntPtr record,
            int value_entry,
            out long long_value,
            out IntPtr error);

        /* Retrieves the multi value of a specific entry
         * Creates a new multi value
         * Returns 1 if successful, 0 if the item does not contain such value or -1 on error
         */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_record_get_multi_value(
             IntPtr record,
             int value_entry,
             out IntPtr multi_value,
             out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libfdatetime_filetime_initialize(
            out IntPtr filetime,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libfdatetime_filetime_free(
            out IntPtr filetime,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libfdatetime_filetime_copy_from_64bit(
            IntPtr filetime,
            UInt64 value_64bit,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libfdatetime_filetime_copy_to_utf16_string(
            IntPtr filetime,
            IntPtr utf16_string,
            int utf16_string_size,
            byte string_format_flags,
            int date_time_format,
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void libesedb_error_free(
            out IntPtr error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr libesedb_error_sprint(
            IntPtr error,
            IntPtr str,
            int size);

        libesedb_file_free FileFree;
        libesedb_file_initialize libFileInitialize;
        libesedb_file_open libFileOpen;
        libesedb_file_open_wide libFileOpenWide;
        libesedb_file_get_number_of_tables libGetNumberTables;
        libesedb_file_get_table libFileGetTable;
        libesedb_table_get_utf16_name_size GetUtf16NameSize;
        libesedb_table_get_utf16_name TableGetUtf16Name;
        libesedb_table_get_number_of_records TableGetNumRecords;
        libesedb_table_free TableFree;
        libesedb_table_get_number_of_columns TableGetNumCols;
        libesedb_table_get_column TableGetCol;
        libesedb_column_free ColFree;
        libesedb_column_get_utf16_name_size ColGetUtf16NameSize;
        libesedb_column_get_utf16_name ColGetUtf16Name;
        libcstring_system_string_allocate SysStringAlloc;
        libesedb_table_get_record TableGetRecord;
        libesedb_record_get_number_of_values RecordGetNumVals;
        libesedb_record_get_column_type RecordGetColType;
        libesedb_record_get_value RecordGetVal;
        libesedb_record_free RecordFree;
        libesedb_record_get_value_boolean RecordGetValBool;
        libesedb_record_get_value_filetime RecordGetValFt;
        libfdatetime_filetime_initialize RecordGetValFtInit;
        libfdatetime_filetime_free RecordGetValFtFree;
        libesedb_record_get_value_floating_point_64bit RecordGetValFloat64;
        libesedb_record_get_value_floating_point_32bit RecordGetValFloat32;
        libesedb_record_get_value_64bit RecordGetVal64;
        libesedb_record_get_value_16bit RecordGetVal16;
        libesedb_record_get_value_8bit RecordGetVal8;
        libesedb_record_get_value_32bit RecordGetVal32;
        libesedb_record_get_value_binary_data_size RecordGetValBinSize;
        libesedb_record_get_value_utf16_string_size RecordGetValUtf16StrSize;
        libesedb_record_get_value_binary_data RecordGetValBin;
        libesedb_record_get_value_utf16_string RecordGetValUtf16Str;
        libfdatetime_filetime_copy_to_utf16_string RecordFtToUtf16;
        libesedb_error_free ErrorFree;
        libesedb_error_sprint ErrorSprint;
        bool InitFuncs()
        {

            long ptrFileFree = loader.GetProcAddress("libesedb_file_free");
            FileFree = (libesedb_file_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrFileFree, typeof(libesedb_file_free));
            
            long ptrErrorFree = loader.GetProcAddress("libesedb_error_free");
            ErrorFree = (libesedb_error_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrErrorFree, typeof(libesedb_error_free));

            long ptrErrorSprint = loader.GetProcAddress("libesedb_error_sprint");
            ErrorSprint = (libesedb_error_sprint)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrErrorSprint, typeof(libesedb_error_sprint));

            long ptrLibFileInitialize = loader.GetProcAddress("libesedb_file_initialize");
            libFileInitialize =
                (libesedb_file_initialize)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrLibFileInitialize, typeof(libesedb_file_initialize));

            long ptrLibFileOpen = loader.GetProcAddress("libesedb_file_open");
            libFileOpen = (libesedb_file_open)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrLibFileOpen, typeof(libesedb_file_open));

            long ptrLibFileOpenWide = loader.GetProcAddress("libesedb_file_open_wide");
            libFileOpenWide = (libesedb_file_open_wide)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrLibFileOpenWide, typeof(libesedb_file_open_wide));

            long ptrLibGetNumTables = loader.GetProcAddress("libesedb_file_get_number_of_tables");
            libGetNumberTables = (libesedb_file_get_number_of_tables)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrLibGetNumTables, typeof(libesedb_file_get_number_of_tables));

            long ptrlibesedb_file_get_table = loader.GetProcAddress("libesedb_file_get_table");
            libFileGetTable =
                        (libesedb_file_get_table)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrlibesedb_file_get_table, typeof(libesedb_file_get_table));

            long ptrGetUtf16NameSize = loader.GetProcAddress("libesedb_table_get_utf16_name_size");
            GetUtf16NameSize =
                        (libesedb_table_get_utf16_name_size)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrGetUtf16NameSize, typeof(libesedb_table_get_utf16_name_size));

            long ptrTableGetUtf16Name = loader.GetProcAddress("libesedb_table_get_utf16_name");
            TableGetUtf16Name =
                (libesedb_table_get_utf16_name)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableGetUtf16Name, typeof(libesedb_table_get_utf16_name));

            long ptrTableGetNumRecords = loader.GetProcAddress("libesedb_table_get_number_of_records");
            TableGetNumRecords =
                (libesedb_table_get_number_of_records)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableGetNumRecords, typeof(libesedb_table_get_number_of_records));

            long ptrTableFree = loader.GetProcAddress("libesedb_table_free");
            TableFree =
                (libesedb_table_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableFree, typeof(libesedb_table_free));

            long ptrTableGetNumCols = loader.GetProcAddress("libesedb_table_get_number_of_columns");
            TableGetNumCols =
                    (libesedb_table_get_number_of_columns)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableGetNumCols, typeof(libesedb_table_get_number_of_columns));

            long ptrTableGetCol = loader.GetProcAddress("libesedb_table_get_column");
            TableGetCol =
                (libesedb_table_get_column)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableGetCol, typeof(libesedb_table_get_column));

            long ptrColFree = loader.GetProcAddress("libesedb_column_free");
            ColFree =
                (libesedb_column_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrColFree, typeof(libesedb_column_free));

            long ptrColGetUtf16NameSize = loader.GetProcAddress("libesedb_column_get_utf16_name_size");
            ColGetUtf16NameSize =
                (libesedb_column_get_utf16_name_size)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrColGetUtf16NameSize, typeof(libesedb_column_get_utf16_name_size));

            long ptrColGetUtf16Name = loader.GetProcAddress("libesedb_column_get_utf16_name");
            ColGetUtf16Name =
                (libesedb_column_get_utf16_name)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrColGetUtf16Name, typeof(libesedb_column_get_utf16_name));

            long ptrSysStringAlloc = loader.GetProcAddress("libcstring_system_string_allocate");
            SysStringAlloc =
                (libcstring_system_string_allocate)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrSysStringAlloc, typeof(libcstring_system_string_allocate));

            long ptrTableGetRecord = loader.GetProcAddress("libesedb_table_get_record");
            TableGetRecord =
                (libesedb_table_get_record)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrTableGetRecord, typeof(libesedb_table_get_record));

            long ptrRecordGetNumVals = loader.GetProcAddress("libesedb_record_get_number_of_values");
            RecordGetNumVals =
                (libesedb_record_get_number_of_values)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetNumVals, typeof(libesedb_record_get_number_of_values));

            long ptrRecordGetColType = loader.GetProcAddress("libesedb_record_get_column_type");
            RecordGetColType =
                (libesedb_record_get_column_type)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetColType, typeof(libesedb_record_get_column_type));

            long ptrRecordGetVal = loader.GetProcAddress("libesedb_record_get_value");
            RecordGetVal =
                (libesedb_record_get_value)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetVal, typeof(libesedb_record_get_value));

            long ptrRecordFree = loader.GetProcAddress("libesedb_record_free");
            RecordFree =
                (libesedb_record_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordFree, typeof(libesedb_record_free));

            long ptrRecordGetValBool = loader.GetProcAddress("libesedb_record_get_value_boolean");
            RecordGetValBool =
                (libesedb_record_get_value_boolean)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValBool, typeof(libesedb_record_get_value_boolean));

            long ptrRecordGetValFt = loader.GetProcAddress("libesedb_record_get_value_filetime");
            libesedb_record_get_value_filetime RecordGetValFt =
                (libesedb_record_get_value_filetime)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValFt, typeof(libesedb_record_get_value_filetime));

            long ptrRecordGetValFtInit = loader.GetProcAddress("libfdatetime_filetime_initialize");
            RecordGetValFtInit =
                (libfdatetime_filetime_initialize)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValFtInit, typeof(libfdatetime_filetime_initialize));

            long ptrRecordGetValFtFree = loader.GetProcAddress("libfdatetime_filetime_free");
            RecordGetValFtFree =
                (libfdatetime_filetime_free)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValFtFree, typeof(libfdatetime_filetime_free));

            long ptrRecordGetValFloat64 = loader.GetProcAddress("libesedb_record_get_value_floating_point_64bit");
            RecordGetValFloat64 =
                (libesedb_record_get_value_floating_point_64bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValFloat64, typeof(libesedb_record_get_value_floating_point_64bit));

            long ptrRecordGetValFloat32 = loader.GetProcAddress("libesedb_record_get_value_floating_point_32bit");
            RecordGetValFloat32 =
                (libesedb_record_get_value_floating_point_32bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValFloat32, typeof(libesedb_record_get_value_floating_point_32bit));

            long ptrRecordGetVal64 = loader.GetProcAddress("libesedb_record_get_value_64bit");
            RecordGetVal64 =
                (libesedb_record_get_value_64bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetVal64, typeof(libesedb_record_get_value_64bit));

            long ptrRecordGetVal16 = loader.GetProcAddress("libesedb_record_get_value_16bit");
            RecordGetVal16 =
                (libesedb_record_get_value_16bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetVal16, typeof(libesedb_record_get_value_16bit));

            long ptrRecordGetVal8 = loader.GetProcAddress("libesedb_record_get_value_8bit");
            RecordGetVal8 =
                (libesedb_record_get_value_8bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetVal8, typeof(libesedb_record_get_value_8bit));

            long ptrRecordGetVal32 = loader.GetProcAddress("libesedb_record_get_value_32bit");
            RecordGetVal32 =
                (libesedb_record_get_value_32bit)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetVal32, typeof(libesedb_record_get_value_32bit));

            long ptrRecordGetValBinSize = loader.GetProcAddress("libesedb_record_get_value_binary_data_size");
            RecordGetValBinSize =
                (libesedb_record_get_value_binary_data_size)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValBinSize, typeof(libesedb_record_get_value_binary_data_size));

            long ptrRecordGetValUtf16StrSize = loader.GetProcAddress("libesedb_record_get_value_utf16_string_size");
            RecordGetValUtf16StrSize =
                (libesedb_record_get_value_utf16_string_size)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValUtf16StrSize, typeof(libesedb_record_get_value_utf16_string_size));

            long ptrRecordGetValBin = loader.GetProcAddress("libesedb_record_get_value_binary_data");
            RecordGetValBin =
                (libesedb_record_get_value_binary_data)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValBin, typeof(libesedb_record_get_value_binary_data));

            long ptrRecordGetValUtf16Str = loader.GetProcAddress("libesedb_record_get_value_utf16_string");
            RecordGetValUtf16Str =
                (libesedb_record_get_value_utf16_string)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordGetValUtf16Str, typeof(libesedb_record_get_value_utf16_string));

            long ptrRecordFtToUtf16 = loader.GetProcAddress("libfdatetime_filetime_copy_to_utf16_string");
            RecordFtToUtf16 =
                (libfdatetime_filetime_copy_to_utf16_string)Marshal.GetDelegateForFunctionPointer((IntPtr)ptrRecordFtToUtf16, typeof(libfdatetime_filetime_copy_to_utf16_string));
            return true;
        }
        #endregion
        static DynaLoader loader = new DynaLoader();
        static byte[] dat;
        bool loaded = false;
        bool isInit = false;
        IntPtr fHandle;
        IntPtr fError;
        public List<HistoryEntry> historyEntries = new List<HistoryEntry>();
        /// <summary>
        /// Instantiate and parse and ESE database
        /// </summary>
        /// <param name="filename">Filename to the ESE db, must be prefixed with \\?\ for the path to resolve</param>
        public Esedb(string filename)
        {
            if (filename == "")
            {
                return;
            }
            if (Marshal.SizeOf(typeof(IntPtr)) == 0x4)
            {
                dat = InternetHistoryParser.Properties.Resources.libesedb_x86;
            }
            else
            {
                dat = InternetHistoryParser.Properties.Resources.libesedb_x64;
            }
            if (!loaded)
            {
                loaded = loader.LoadLibrary(dat);
                if (loaded)
                {
                    isInit = InitFuncs();
                    if (isInit)
                    {
                        bool isSetup = SetupFile(filename);
                        if (isSetup)
                        {
                            GetTables();
                            if (fError != IntPtr.Zero)
                            {
                                ErrorFree(out fError);
                            }
                            if (fHandle != IntPtr.Zero)
                            {
                                FileFree(out fHandle, 0);
                            }
                            return;
                        }
                    }
                }
            }
            bool isSetup2 = SetupFile(filename);
            if (isSetup2)
            {
                GetTables();
                if (fError != IntPtr.Zero)
                {
                    ErrorFree(out fError);
                }
                if (fHandle != IntPtr.Zero)
                {
                    FileFree(out fHandle, 0);
                }
            }
        }
        public Esedb(byte[] file)
        {
            if (Marshal.SizeOf(typeof(IntPtr)) == 0x4)
            {
                dat = InternetHistoryParser.Properties.Resources.libesedb_x86;
            }
            else
            {
                dat = InternetHistoryParser.Properties.Resources.libesedb_x64;
            }
            loaded = loader.LoadLibrary(dat);
            InitFuncs();
            //bool isSetup = SetupFile(filename);
            //if (isSetup)
            //{
            //    GetTables();
            //}
        }
        bool SetupFile(string filename)
        {
            if (filename == "")
            {
                return false;
            }
            var val = libFileInitialize(out fHandle, out fError);
            char[] fName = filename.ToCharArray();
            val = libFileOpen(fHandle, fName, 1, out fError);
            int err = Marshal.GetLastWin32Error();
            
            if (val.ToInt32() != 0x1)
            {
                val = libFileOpenWide(fHandle, filename, 1, out fError);
                if (val.ToInt32() != 0x1)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return true;
            }
        }

        int GetTables()
        {
            int tableCount = 0;

            var val = libGetNumberTables(fHandle, out tableCount, out fError);
            FieldInfo[] fi = typeof(HistoryEntry).GetFields(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < tableCount; i++)
            {
                IntPtr table = IntPtr.Zero;

                val = libFileGetTable(fHandle, i, out table, out fError);

                int nameSize;

                val = GetUtf16NameSize(table, out nameSize, out fError);

                IntPtr strNamePtr = AllocData(true, nameSize);

                val = TableGetUtf16Name(table, strNamePtr, nameSize, out fError);
                string strName = Marshal.PtrToStringUni(strNamePtr);
                if (strName.ToLower().StartsWith("container_"))
                {
                    HistoryEntry history = new HistoryEntry();
                    int recordCount = 0;

                    val = TableGetNumRecords(table, out recordCount, out fError);
                    string[] columnNames = GetTableColumnNames(table);
                    string[] recs = new string[columnNames.Length];
                    if (columnNames.Length == 25)
                    {
                        for (int j = 0; j < recordCount; j++)
                        {
                            recs = RetrieveRecord(table, IntPtr.Zero, j);
                            history.tablename = strName;
                            history.EntryId = recs[0];
                            history.ContainerId = recs[1];
                            history.CacheId = recs[2];
                            history.UrlHash = recs[3];
                            history.SecureDirectory = recs[4];
                            history.FileSize = recs[5];
                            history.Type = recs[6];
                            history.Flags = recs[7];
                            history.AccessCount = recs[8];
                            history.SyncTime = recs[9];
                            history.CreationTime = Program.GetTime(Convert.ToInt64(recs[10]));
                            history.ExpiryTime = Program.GetTime(Convert.ToInt64(recs[11]));
                            history.ModifiedTime = Program.GetTime(Convert.ToInt64(recs[12]));
                            history.AccesedTime = Program.GetTime(Convert.ToInt64(recs[13]));
                            history.PostCheckTime = recs[14];
                            history.SyncCount = recs[15];
                            history.ExemptionDelta = recs[16];
                            history.Url = recs[17];
                            history.Filename = recs[18];
                            history.FileExtension = recs[19];
                            history.RequestHeaders = recs[20];
                            history.ResponseHeaders = recs[21];
                            history.RedirectUrl = recs[22];
                            history.Group = recs[23];
                            history.extraData = recs[24];
                            historyEntries.Add(history);
                        }
                    }
                }
                FreeTable(table);
            }
            return (int)val;
        }

        int FreeTable(IntPtr table)
        {

            var val = TableFree(out table, out fError);
            return (int)val;
        }
        int GetTableColumnCount(IntPtr table)
        {

            byte flags = 0;
            int columnCount = 0;

            var val = TableGetNumCols(table, out columnCount, flags, out fError);
            return columnCount;
        }
        int GetTableRecordCount(IntPtr table)
        {
            int recordCount = 0;

            var val = TableGetNumRecords(table, out recordCount, out fError);
            return (int)val;
        }


        string[] GetTableColumnNames(IntPtr table)
        {
            string[] val;
            int columnCount = 0;
            columnCount = GetTableColumnCount(table);
            val = new string[columnCount];


            for (int i = 0; i < columnCount; i++)
            {
                IntPtr column = IntPtr.Zero;

                var ret = TableGetCol(table, i, out column, 0, out fError);
                int size = 0;
                ret = ColGetUtf16NameSize(column, out size, out fError);

                IntPtr p = AllocData(true, size);

                if (p != null)
                {
                    ret = ColGetUtf16Name(column, p, size, out fError);
                    string str = Marshal.PtrToStringUni(p);
                    FreeData(p);
                    val[i] = str;
                }
                ret = ColFree(out column, out fError);
            }
            return val;
        }

        string[] RetrieveRecord(IntPtr table, IntPtr column, int index)
        {
            string[] records;
            IntPtr record;
            int valueCount = 0;
            LIBESEDB_COLUMN_TYPES colType = LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_NULL;
            IntPtr ret = TableGetRecord(table, index, out record, out fError);
            ret = RecordGetNumVals(record, out valueCount, out fError);

            records = new string[valueCount];
            for (int i = 0; i < valueCount; i++)
            {
                IntPtr valueData;
                LIBESEDB_VALUE_FLAGS valueFlags;
                int valueSize;


                ret = RecordGetColType(record, i, out colType, out fError);



                ret = RecordGetVal(record, i, out valueData, out valueSize, out valueFlags, out fError);
                records[i] = GetValueFromRecord(record, colType, i);
            }


            ret = RecordFree(out record, out fError);
            return records;
        }


        string GetValueFromRecord(IntPtr value, LIBESEDB_COLUMN_TYPES type, int index)
        {
            string retval = "";
            IntPtr ret = IntPtr.Zero;
            if (value != IntPtr.Zero)
            {
                switch (type)
                {
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_NULL:
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_BOOLEAN:

                        byte blVal;
                        ret = RecordGetValBool(value, index, out blVal, out fError);
                        retval = Convert.ToBoolean(blVal).ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_DATE_TIME:

                        ulong ft;
                        ret = RecordGetValFt(value, index, out ft, out fError);


                        IntPtr pFt;
                        ret = RecordGetValFtInit(out pFt, out fError);


                        IntPtr strFt = AllocData(true, 32);
                        ret = RecordFtToUtf16(pFt, strFt, 32, 1 | 2 | 3, 99, out fError);

                        retval = Marshal.PtrToStringUni(strFt);
                        FreeData(pFt);


                        ret = RecordGetValFtFree(out pFt, out fError);
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_DOUBLE_64BIT:
                        double dblVal;
                        ret = RecordGetValFloat64(value, index, out dblVal, out fError);
                        retval = dblVal.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_FLOAT_32BIT:
                        float flVal;
                        ret = RecordGetValFloat32(value, index, out flVal, out fError);
                        retval = flVal.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_GUID:

                        ulong ulVal;
                        ret = RecordGetVal64(value, index, out ulVal, out fError);
                        retval = ulVal.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_16BIT_SIGNED:
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_16BIT_UNSIGNED:

                        short sVal;
                        ret = RecordGetVal16(value, index, out sVal, out fError);
                        retval = sVal.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_8BIT_UNSIGNED:

                        byte bVal2;
                        ret = RecordGetVal8(value, index, out bVal2, out fError);
                        retval = bVal2.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_32BIT_SIGNED:
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_32BIT_UNSIGNED:

                        uint uiVal;
                        ret = RecordGetVal32(value, index, out uiVal, out fError);
                        retval = uiVal.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_CURRENCY:
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_INTEGER_64BIT_SIGNED:

                        ulong ul64Val;
                        ret = RecordGetVal64(value, index, out ul64Val, out fError);
                        retval = ul64Val.ToString();
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_BINARY_DATA:
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_LARGE_BINARY_DATA:
                        //libesedb_record_get_value_utf16_string_size

                        int binSize;
                        ret = RecordGetValBinSize(value, index, out binSize, out fError);
                        if (binSize == 0)
                            break;

                        IntPtr binData = AllocData(false, binSize);
                        ret = RecordGetValBin(value, index, binData, binSize, out fError);
                        byte[] bData = new byte[binSize];
                        Marshal.Copy(binData, bData, 0, binSize);
                        retval = BitConverter.ToString(bData);
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_TEXT:
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_LARGE_TEXT:
                        //libesedb_record_get_value_utf16_string_size

                        int strSize;
                        ret = RecordGetValUtf16StrSize(value, index, out strSize, out fError);
                        if (strSize == 0)
                            break;
                        IntPtr strData = AllocData(true, strSize);



                        ret = RecordGetValUtf16Str(value, index, strData, strSize, out fError);
                        retval = Marshal.PtrToStringUni(strData);
                        FreeData(strData);
                        break;
                    case LIBESEDB_COLUMN_TYPES.LIBESEDB_COLUMN_TYPE_SUPER_LARGE_VALUE:
                    //long addr18 = loader.GetProcAddress("libesedb_record_get_value_binary_data");
                    //libesedb_record_get_value_binary_data invoke18 =
                    //    (libesedb_record_get_value_binary_data)Marshal.GetDelegateForFunctionPointer((IntPtr)addr18, typeof(libesedb_record_get_value_binary_data));

                    //ret = invoke18(value,index, out fError);
                    //break;
                    default:
                        break;
                }
            }
            return retval;
        }
        IntPtr AllocData(bool isWide, int strLen)
        {
            IntPtr ptr = GetProcessHeap();
            IntPtr p;
            if (isWide)
            {
                p = HeapAlloc(ptr, 0, (uint)(2 * strLen));
                return p;
            }
            else
            {
                p = HeapAlloc(ptr, 0, (uint)(strLen));
                return p;
            }
        }
        bool FreeData(IntPtr ptr)
        {
            IntPtr p = GetProcessHeap();
            if (p != null && ptr != null)
            {
                HeapFree(p, 0, ptr);
                return true;
            }
            return false;
        }
        
    }
}
