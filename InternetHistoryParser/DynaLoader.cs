using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DatIndexParser
{
    class DynaLoader
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_EXPORT_DIRECTORY
        {
            public UInt32 Characteristics;
            public UInt32 TimeDateStamp;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt32 Name;
            public UInt32 Base;
            public UInt32 NumberOfFunctions;
            public UInt32 NumberOfNames;
            public UInt32 AddressOfFunctions;     // RVA from base of image
            public UInt32 AddressOfNames;     // RVA from base of image
            public UInt32 AddressOfNameOrdinals;  // RVA from base of image
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_IMPORT_BY_NAME
        {
            public short Hint;
            public byte Name;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYMODULE
        {
            public IMAGE_NT_HEADERS32 headers;
            public IntPtr codeBase;
            public IntPtr modules;
            public int numModules;
            public int initialized;

        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYMODULE64
        {
            public IMAGE_NT_HEADERS64 headers;
            public IntPtr codeBase;
            public IntPtr modules;
            public int numModules;
            public int initialized;

        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_BASE_RELOCATION
        {
            public uint VirtualAddress;
            public uint SizeOfBlock;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_IMPORT_DESCRIPTOR
        {
            public uint CharacteristicsOrOriginalFirstThunk;    // 0 for terminating null import descriptor; RVA to original unbound IAT (PIMAGE_THUNK_DATA)
            public uint TimeDateStamp;                          // 0 if not bound, -1 if bound, and real date\time stamp in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND); O.W. date/time stamp of DLL bound to (Old BIND)
            public uint ForwarderChain;                         // -1 if no forwarders
            public uint Name;
            public uint FirstThunk;                             // RVA to IAT (if bound this IAT has actual addresses)
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_IMPORT_DESCRIPTOR64
        {
            public UInt64 CharacteristicsOrOriginalFirstThunk;    // 0 for terminating null import descriptor; RVA to original unbound IAT (PIMAGE_THUNK_DATA)
            public UInt64 TimeDateStamp;                          // 0 if not bound, -1 if bound, and real date\time stamp in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND); O.W. date/time stamp of DLL bound to (Old BIND)
            public UInt64 ForwarderChain;                         // -1 if no forwarders
            public UInt64 Name;
            public UInt64 FirstThunk;                             // RVA to IAT (if bound this IAT has actual addresses)
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Name;
            //union 
            //{    
            //    DWORD PhysicalAddress;    
            //    DWORD VirtualSize;  
            //} Misc;  
            public uint PhysicalAddress;
            //public uint VirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public short NumberOfRelocations;
            public short NumberOfLinenumbers;
            public uint Characteristics;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_SECTION_HEADER64
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Name;
            //union 
            //{    
            //    DWORD PhysicalAddress;    
            //    DWORD VirtualSize;  
            //} Misc;  
            public UInt64 PhysicalAddress;
            //public uint VirtualSize;
            public UInt64 VirtualAddress;
            public uint SizeOfRawData;
            public UInt64 PointerToRawData;
            public UInt64 PointerToRelocations;
            public UInt64 PointerToLinenumbers;
            public short NumberOfRelocations;
            public short NumberOfLinenumbers;
            public uint Characteristics;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct IMAGE_DOS_HEADER
        {
            public UInt16 e_magic;       // Magic number
            public UInt16 e_cblp;        // Bytes on last page of file
            public UInt16 e_cp;          // Pages in file
            public UInt16 e_crlc;        // Relocations
            public UInt16 e_cparhdr;     // Size of header in paragraphs
            public UInt16 e_minalloc;    // Minimum extra paragraphs needed
            public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
            public UInt16 e_ss;          // Initial (relative) SS value
            public UInt16 e_sp;          // Initial SP value
            public UInt16 e_csum;        // Checksum
            public UInt16 e_ip;          // Initial IP value
            public UInt16 e_cs;          // Initial (relative) CS value
            public UInt16 e_lfarlc;      // File address of relocation table
            public UInt16 e_ovno;        // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt16[] e_res1;        // Reserved words
            public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
            public UInt16 e_oeminfo;     // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public UInt16[] e_res2;        // Reserved words
            public Int32 e_lfanew;      // File address of new exe header
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_DATA_DIRECTORY
        {
            public UInt32 VirtualAddress;
            public UInt32 Size;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_NT_HEADERS64
        {
            public uint Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_OPTIONAL_HEADER64
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public UInt64 ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public UInt64 SizeOfStackReserve;
            public UInt64 SizeOfStackCommit;
            public UInt64 SizeOfHeapReserve;
            public UInt64 SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_OPTIONAL_HEADER32
        {
            //
            // Standard fields.
            //
            public UInt16 Magic;
            public Byte MajorLinkerVersion;
            public Byte MinorLinkerVersion;
            public UInt32 SizeOfCode;
            public UInt32 SizeOfInitializedData;
            public UInt32 SizeOfUninitializedData;
            public UInt32 AddressOfEntryPoint;
            public UInt32 BaseOfCode;
            public UInt32 BaseOfData;
            //
            // NT additional fields.
            //
            public UInt32 ImageBase;
            public UInt32 SectionAlignment;
            public UInt32 FileAlignment;
            public UInt16 MajorOperatingSystemVersion;
            public UInt16 MinorOperatingSystemVersion;
            public UInt16 MajorImageVersion;
            public UInt16 MinorImageVersion;
            public UInt16 MajorSubsystemVersion;
            public UInt16 MinorSubsystemVersion;
            public UInt32 Win32VersionValue;
            public UInt32 SizeOfImage;
            public UInt32 SizeOfHeaders;
            public UInt32 CheckSum;
            public UInt16 Subsystem;
            public UInt16 DllCharacteristics;
            public UInt32 SizeOfStackReserve;
            public UInt32 SizeOfStackCommit;
            public UInt32 SizeOfHeapReserve;
            public UInt32 SizeOfHeapCommit;
            public UInt32 LoaderFlags;
            public UInt32 NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct IMAGE_FILE_HEADER
        {
            public UInt16 Machine;
            public UInt16 NumberOfSections;
            public UInt32 TimeDateStamp;
            public UInt32 PointerToSymbolTable;
            public UInt32 NumberOfSymbols;
            public UInt16 SizeOfOptionalHeader;
            public UInt16 Characteristics;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_NT_HEADERS32
        {
            public UInt32 Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
        }
        internal class Win32Constants
        {
            public static UInt32 MEM_COMMIT = 0x1000;

            public static UInt32 PAGE_EXECUTE_READWRITE = 0x40;
            public static UInt32 PAGE_READWRITE = 0x04;

            public static UInt32 MEM_RELEASE = 0x8000;
            public static UInt32 MEM_RESERVE = 0x2000;

        }

        internal static class Win32Imports
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern UInt32 GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32",EntryPoint="GetProcAddress", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern Int64 GetProcAddress64(IntPtr hModule, string procName);

            [DllImport("kernel32")]
            public static extern int LoadLibrary(string lpFileName);

            [DllImport("kernel32", EntryPoint = "LoadLibrary")]
            public static extern long LoadLibrary64(string lpFileName);

            [DllImport("kernel32")]
            public static extern UInt32 GetLastError();

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetProcAddress(IntPtr module, IntPtr ordinal);

            [DllImport("kernel32")]
            public static extern UInt32 VirtualAlloc(UInt32 lpStartAddr,
                 UInt32 size, UInt32 flAllocationType, UInt32 flProtect);
            [DllImport("kernel32")]
            public static extern UInt64 VirtualAlloc(UInt64 lpStartAddr,
                 UInt64 size, UInt64 flAllocationType, UInt64 flProtect);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize,
               uint dwFreeType);

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
               uint flNewProtect, out uint lpflOldProtect);

        }

        internal static class PointerHelpers
        {
            public unsafe static void PrintMembers<T>(T item)
            {
                Type myType = typeof(T);
                // Get the type and fields of FieldInfoClass.
                Debug.WriteLine("Type: " + myType.Name);
                Type t = item.GetType();
                System.Reflection.FieldInfo[] fields = t.GetFields();
                foreach (System.Reflection.FieldInfo field in fields)
                {
                    Debug.WriteLine(field.Name + " = " + field.GetValue(item).ToString());
                }
            }
            public static T ToStruct<T>(byte[] data) where T : struct
            {
                unsafe
                {
                    fixed (byte* p = &data[0])
                    {
                        return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
                    }
                }
            }

            public static T ToStruct<T>(byte[] data, uint from) where T : struct
            {
                unsafe
                {
                    fixed (byte* p = &data[from])
                    {
                        return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
                    }
                }
            }

            public static T ToStruct<T>(IntPtr ptr, uint from) where T : struct
            {
                return (T)Marshal.PtrToStructure((IntPtr)(ptr.ToInt32() + (int)from), typeof(T));
            }
            public static T ToStruct64<T>(IntPtr ptr, uint from) where T : struct
            {
                return (T)Marshal.PtrToStructure((IntPtr)(ptr.ToInt64() + (int)from), typeof(T));
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe delegate bool fnDllEntry(int instance, uint reason, void* reserved);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        unsafe delegate bool fnDllEntry64(Int64 instance, uint reason, void* reserved);

        internal unsafe bool LoadLibrary(byte[] data)
        {
            //fnDllEntry dllEntry;
            bool success = false;
            var dosHeader = PointerHelpers.ToStruct<IMAGE_DOS_HEADER>(data);
            PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
            if (Marshal.SizeOf(typeof(IntPtr)) == 0x4)
            {
                var oldHeader = PointerHelpers.ToStruct<IMAGE_NT_HEADERS32>(data, (uint)dosHeader.e_lfanew);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                var code = (IntPtr)(Win32Imports.VirtualAlloc(oldHeader.OptionalHeader.ImageBase, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_RESERVE, Win32Constants.PAGE_READWRITE));
                PointerHelpers.PrintMembers<IntPtr>(code);
                if (code.ToInt32() == 0)
                    code = (IntPtr)(Win32Imports.VirtualAlloc((uint)code, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_RESERVE, Win32Constants.PAGE_READWRITE));

                module = new MEMORYMODULE { codeBase = code, numModules = 0, modules = new IntPtr(0), initialized = 0 };
                PointerHelpers.PrintMembers<MEMORYMODULE>(module);
                Win32Imports.VirtualAlloc((uint)code, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_COMMIT, Win32Constants.PAGE_READWRITE);

                var headers = (IntPtr)(Win32Imports.VirtualAlloc((uint)code, oldHeader.OptionalHeader.SizeOfHeaders, Win32Constants.MEM_COMMIT, Win32Constants.PAGE_READWRITE));
                PointerHelpers.PrintMembers<IntPtr>(headers);
                Marshal.Copy(data, 0, headers, (int)(dosHeader.e_lfanew + oldHeader.OptionalHeader.SizeOfHeaders));
                //PointerHelpers.PrintMembers<IntPtr>(headers);

                module.headers = PointerHelpers.ToStruct<IMAGE_NT_HEADERS32>(headers, (uint)dosHeader.e_lfanew);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(module.headers);
                module.headers.OptionalHeader.ImageBase = (uint)code;
                PointerHelpers.PrintMembers<uint>(module.headers.OptionalHeader.ImageBase);

                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                CopySections(data, oldHeader, headers, dosHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                PointerHelpers.PrintMembers<MEMORYMODULE>(module);
                var locationDelta = (uint)(code.ToInt32() - (int)oldHeader.OptionalHeader.ImageBase);
                PointerHelpers.PrintMembers<uint>(locationDelta);
                if (locationDelta != 0)
                    PerformBaseRelocation(locationDelta);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                BuildImportTable();
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                FinalizeSections(headers, dosHeader, oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS32>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);

                try
                {
                    //Angelo Mods
                    long mod = module.codeBase.ToInt32() + (int)module.headers.OptionalHeader.AddressOfEntryPoint;
                    //end mods
                    fnDllEntry dllEntry =
                        (fnDllEntry)
                        Marshal.GetDelegateForFunctionPointer(
                            new IntPtr(module.codeBase.ToInt32() + (int)module.headers.OptionalHeader.AddressOfEntryPoint),
                            typeof(fnDllEntry));
                    success = dllEntry(code.ToInt32(), 1, (void*)0);
                }
                catch (Exception ex)
                {
                    int message = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                var oldHeader = PointerHelpers.ToStruct<IMAGE_NT_HEADERS64>(data, (uint)dosHeader.e_lfanew);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                var code = (IntPtr)(Win32Imports.VirtualAlloc(oldHeader.OptionalHeader.ImageBase, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_RESERVE, Win32Constants.PAGE_READWRITE));
                PointerHelpers.PrintMembers<IntPtr>(code);
                if (code.ToInt64() == 0)
                    code = (IntPtr)(Win32Imports.VirtualAlloc((uint)code, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_RESERVE, Win32Constants.PAGE_READWRITE));

                module64 = new MEMORYMODULE64 { codeBase = code, numModules = 0, modules = new IntPtr(0), initialized = 0 };
                PointerHelpers.PrintMembers<MEMORYMODULE64>(module64);
                Win32Imports.VirtualAlloc((UInt64)code, oldHeader.OptionalHeader.SizeOfImage, Win32Constants.MEM_COMMIT, Win32Constants.PAGE_READWRITE);

                var headers = (IntPtr)(Win32Imports.VirtualAlloc((UInt64)code, oldHeader.OptionalHeader.SizeOfHeaders, Win32Constants.MEM_COMMIT, Win32Constants.PAGE_READWRITE));
                PointerHelpers.PrintMembers<IntPtr>(headers);
                Marshal.Copy(data, 0, headers, (int)(dosHeader.e_lfanew + oldHeader.OptionalHeader.SizeOfHeaders));
                PointerHelpers.PrintMembers<IntPtr>(headers);
                
                module64.headers = PointerHelpers.ToStruct64<IMAGE_NT_HEADERS64>(headers, (uint)dosHeader.e_lfanew);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(module64.headers);
                module64.headers.OptionalHeader.ImageBase = (UInt64)code;
                PointerHelpers.PrintMembers<UInt64>(module64.headers.OptionalHeader.ImageBase);
                
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                CopySections64(data, oldHeader, headers, dosHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                PointerHelpers.PrintMembers<MEMORYMODULE64>(module64);
                var locationDelta = (uint)(code.ToInt64() - (int)oldHeader.OptionalHeader.ImageBase);
                
                if (locationDelta != 0)
                    PerformBaseRelocation64(locationDelta);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                BuildImportTable64();
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);
                FinalizeSections64(headers, dosHeader, oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IMAGE_NT_HEADERS64>(oldHeader);
                PointerHelpers.PrintMembers<IMAGE_DOS_HEADER>(dosHeader);
                PointerHelpers.PrintMembers<IntPtr>(headers);

                try
                {
                    //Angelo Mods
                    long mod = module64.codeBase.ToInt64() + (int)module64.headers.OptionalHeader.AddressOfEntryPoint;
                    //end mods
                    fnDllEntry64 dllEntry =
                        (fnDllEntry64)
                        Marshal.GetDelegateForFunctionPointer(
                            new IntPtr(module64.codeBase.ToInt64() + (int)module64.headers.OptionalHeader.AddressOfEntryPoint),
                            typeof(fnDllEntry64));
                    success = dllEntry(code.ToInt64(), 1, (void*)0);
                }
                catch (Exception ex)
                {
                    int message = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            return success;
        }

        public int GetModuleCount()
        {
            int count = 0;
            IntPtr codeBase = module.codeBase;
            IMAGE_DATA_DIRECTORY directory = module.headers.OptionalHeader.DataDirectory[1];
            if (directory.Size > 0)
            {
                var importDesc = PointerHelpers.ToStruct<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress);
                while (importDesc.Name > 0)
                {
                    var str = codeBase.ToInt32() + (int)importDesc.Name;
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    int handle = Win32Imports.LoadLibrary(tmp);

                    if (handle == -1)
                    {
                        break;
                    }
                    count++;
                    importDesc = PointerHelpers.ToStruct<IMAGE_IMPORT_DESCRIPTOR>(codeBase, (uint)(directory.VirtualAddress + (Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR)) * (count))));
                }
            }
            return count;
        }
        public int GetModuleCount64()
        {
            int count = 0;
            IntPtr codeBase = module64.codeBase;
            IMAGE_DATA_DIRECTORY directory = module64.headers.OptionalHeader.DataDirectory[1];
            if (directory.Size > 0)
            {
                var importDesc = PointerHelpers.ToStruct64<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress);
                while (importDesc.Name > 0)
                {
                    var str = codeBase.ToInt64() + (int)importDesc.Name;
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    int handle = Win32Imports.LoadLibrary(tmp);

                    if (handle == -1)
                    {
                        break;
                    }
                    count++;
                    importDesc = PointerHelpers.ToStruct64<IMAGE_IMPORT_DESCRIPTOR>(codeBase, (uint)(directory.VirtualAddress + (Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR)) * (count))));
                }
            }
            return count;
        }

        public int BuildImportTable()
        {
            int ucount = GetModuleCount();
            module.modules = Marshal.AllocHGlobal((ucount) * sizeof(int));
            int pcount = 0;
            int result = 1;
            IntPtr codeBase = module.codeBase;
            IMAGE_DATA_DIRECTORY directory = module.headers.OptionalHeader.DataDirectory[1];
            if (directory.Size > 0)
            {
                var importDesc = PointerHelpers.ToStruct<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress);
                while (importDesc.Name > 0)
                {
                    var str = codeBase.ToInt32() + (int)importDesc.Name;
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    unsafe
                    {
                        uint* thunkRef;
                        uint* funcRef;

                        int handle = Win32Imports.LoadLibrary(tmp);

                        if (handle == -1)
                        {
                            result = 0;
                            break;
                        }

                        if (importDesc.CharacteristicsOrOriginalFirstThunk > 0)
                        {
                            IntPtr thunkRefAddr = (IntPtr)(codeBase.ToInt32() + (int)importDesc.CharacteristicsOrOriginalFirstThunk);
                            thunkRef = (uint*)thunkRefAddr;
                            funcRef = (uint*)(codeBase.ToInt32() + (int)importDesc.FirstThunk);
                        }
                        else
                        {
                            thunkRef = (uint*)(codeBase.ToInt32() + (int)importDesc.FirstThunk);
                            funcRef = (uint*)(codeBase.ToInt32() + (int)importDesc.FirstThunk);
                        }
                        for (; *thunkRef > 0; thunkRef++, funcRef++)
                        {
                            if ((*thunkRef & 0x80000000) != 0)
                            {
                                *funcRef = (uint)Win32Imports.GetProcAddress(new IntPtr(handle), new IntPtr(*thunkRef & 0xffff));
                            }
                            else
                            {
                                var str2 = codeBase.ToInt32() + (int)(*thunkRef) + 2;
                                var tmpaa = Marshal.PtrToStringAnsi((IntPtr)str2);
                                *funcRef = Win32Imports.GetProcAddress(new IntPtr(handle), tmpaa);
                            }
                            if (*funcRef == 0)
                            {
                                result = 0;
                                break;
                            }
                        }


                        pcount++;
                        importDesc = PointerHelpers.ToStruct<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress + (uint)(Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR)) * pcount));
                    }
                }
            }
            return result;
        }
        public int BuildImportTable64()
        {
            int ucount = GetModuleCount64();
            module64.modules = Marshal.AllocHGlobal((ucount) * sizeof(int));
            int pcount = 0;
            int result = 1;
            IntPtr codeBase = module64.codeBase;
            IMAGE_DATA_DIRECTORY directory = module64.headers.OptionalHeader.DataDirectory[1];
            if (directory.Size > 0)
            {
                var importDesc = PointerHelpers.ToStruct64<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress);
                while (importDesc.Name > 0)
                {
                    var str = codeBase.ToInt64() + (int)importDesc.Name;
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    unsafe
                    {
                        Int64* thunkRef;
                        Int64* funcRef;

                        long handle = Win32Imports.LoadLibrary64(tmp);

                        if (handle == -1)
                        {
                            result = 0;
                            break;
                        }

                        if (importDesc.CharacteristicsOrOriginalFirstThunk > 0)
                        {
                            IntPtr thunkRefAddr = (IntPtr)(codeBase.ToInt64() + (int)importDesc.CharacteristicsOrOriginalFirstThunk);
                            thunkRef = (Int64*)thunkRefAddr;
                            funcRef = (Int64*)(codeBase.ToInt64() + (int)importDesc.FirstThunk);
                        }
                        else
                        {
                            thunkRef = (Int64*)(codeBase.ToInt64() + (int)importDesc.FirstThunk);
                            funcRef = (Int64*)(codeBase.ToInt64() + (int)importDesc.FirstThunk);
                        }
                        for (; *thunkRef > 0; thunkRef++, funcRef++)
                        {
                            if ((*thunkRef & 0x80000000) != 0)
                            {
                                *funcRef = (long)Win32Imports.GetProcAddress(new IntPtr(handle), new IntPtr(*thunkRef & 0xffff));
                            }
                            else
                            {
                                var str2 = codeBase.ToInt64() + (int)(*thunkRef) + 2;
                                var tmpaa = Marshal.PtrToStringAnsi((IntPtr)str2);
                                *funcRef = Win32Imports.GetProcAddress64(new IntPtr(handle), tmpaa);
                            }
                            if (*funcRef == 0)
                            {
                                result = 0;
                                break;
                            }
                        }


                        pcount++;
                        importDesc = PointerHelpers.ToStruct64<IMAGE_IMPORT_DESCRIPTOR>(codeBase, directory.VirtualAddress + (uint)(Marshal.SizeOf(typeof(IMAGE_IMPORT_DESCRIPTOR)) * pcount));
                    }
                }
            }
            return result;
        }

        static readonly int[][][] ProtectionFlags = new int[2][][];
        public void FinalizeSections(IntPtr headers, IMAGE_DOS_HEADER dosHeader, IMAGE_NT_HEADERS32 oldHeaders)
        {
            ProtectionFlags[0] = new int[2][];
            ProtectionFlags[1] = new int[2][];
            ProtectionFlags[0][0] = new int[2];
            ProtectionFlags[0][1] = new int[2];
            ProtectionFlags[1][0] = new int[2];
            ProtectionFlags[1][1] = new int[2];
            ProtectionFlags[0][0][0] = 0x01;
            ProtectionFlags[0][0][1] = 0x08;
            ProtectionFlags[0][1][0] = 0x02;
            ProtectionFlags[0][1][1] = 0x04;
            ProtectionFlags[1][0][0] = 0x10;
            ProtectionFlags[1][0][1] = 0x80;
            ProtectionFlags[1][1][0] = 0x20;
            ProtectionFlags[1][1][1] = 0x40;

            var section = PointerHelpers.ToStruct<IMAGE_SECTION_HEADER>(headers, (uint)(24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader));
            for (int i = 0; i < module.headers.FileHeader.NumberOfSections; i++)
            {
                //Console.WriteLine("Finalizing " + Encoding.UTF8.GetString(section.Name));
                int executable = (section.Characteristics & 0x20000000) != 0 ? 1 : 0;
                int readable = (section.Characteristics & 0x40000000) != 0 ? 1 : 0;
                int writeable = (section.Characteristics & 0x80000000) != 0 ? 1 : 0;

                if ((section.Characteristics & 0x02000000) > 0)
                {
                    bool aa = Win32Imports.VirtualFree(new IntPtr(section.PhysicalAddress), (UIntPtr)section.SizeOfRawData, 0x4000);
                    continue;
                }

                var protect = (uint)ProtectionFlags[executable][readable][writeable];

                if ((section.Characteristics & 0x04000000) > 0)
                    protect |= 0x200;
                var size = (int)section.SizeOfRawData;
                if (size == 0)
                {
                    if ((section.Characteristics & 0x00000040) > 0)
                        size = (int)module.headers.OptionalHeader.SizeOfInitializedData;
                    else if ((section.Characteristics & 0x00000080) > 0)
                        size = (int)module.headers.OptionalHeader.SizeOfUninitializedData;

                }

                if (size > 0)
                {
                    uint oldProtect;
                    if (!Win32Imports.VirtualProtect(new IntPtr(section.PhysicalAddress), section.SizeOfRawData, protect, out oldProtect))
                    {
                    }
                }

                section = PointerHelpers.ToStruct<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
            }

        }

        public void FinalizeSections64(IntPtr headers, IMAGE_DOS_HEADER dosHeader, IMAGE_NT_HEADERS64 oldHeaders)
        {
            ProtectionFlags[0] = new int[2][];
            ProtectionFlags[1] = new int[2][];
            ProtectionFlags[0][0] = new int[2];
            ProtectionFlags[0][1] = new int[2];
            ProtectionFlags[1][0] = new int[2];
            ProtectionFlags[1][1] = new int[2];
            ProtectionFlags[0][0][0] = 0x01;
            ProtectionFlags[0][0][1] = 0x08;
            ProtectionFlags[0][1][0] = 0x02;
            ProtectionFlags[0][1][1] = 0x04;
            ProtectionFlags[1][0][0] = 0x10;
            ProtectionFlags[1][0][1] = 0x80;
            ProtectionFlags[1][1][0] = 0x20;
            ProtectionFlags[1][1][1] = 0x40;

            var section = PointerHelpers.ToStruct64<IMAGE_SECTION_HEADER>(headers, (uint)(24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader));
            var imageOffset = module64.headers.OptionalHeader.ImageBase & 0xffffffff00000000;
            for (int i = 0; i < module64.headers.FileHeader.NumberOfSections; i++)
            {
                //Console.WriteLine("Finalizing " + Encoding.UTF8.GetString(section.Name));
                int executable = (section.Characteristics & 0x20000000) != 0 ? 1 : 0;
                int readable = (section.Characteristics & 0x40000000) != 0 ? 1 : 0;
                int writeable = (section.Characteristics & 0x80000000) != 0 ? 1 : 0;

                if ((section.Characteristics & 0x02000000) > 0)
                {
                    bool aa = Win32Imports.VirtualFree(new IntPtr(section.PhysicalAddress | (long)imageOffset), (UIntPtr)section.SizeOfRawData, 0x4000);
                    continue;
                }

                var protect = (uint)ProtectionFlags[executable][readable][writeable];

                if ((section.Characteristics & 0x04000000) > 0)
                    protect |= 0x200;
                var size = (int)section.SizeOfRawData;
                if (size == 0)
                {
                    if ((section.Characteristics & 0x00000040) > 0)
                        size = (int)module64.headers.OptionalHeader.SizeOfInitializedData;
                    else if ((section.Characteristics & 0x00000080) > 0)
                        size = (int)module64.headers.OptionalHeader.SizeOfUninitializedData;

                }

                if (size > 0)
                {
                    uint oldProtect;
                    
                    if (!Win32Imports.VirtualProtect(new IntPtr(section.PhysicalAddress | (long)imageOffset), section.SizeOfRawData, protect, out oldProtect))
                    {
                        throw new AccessViolationException("Angelo - You need to fixup the section.PhysicalAddress stuff");
                    }
                }

                section = PointerHelpers.ToStruct64<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
            }

        }

        public void PerformBaseRelocation(uint delta)
        {
            IntPtr codeBase = module.codeBase;
            int sizeOfBase = Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION));
            IMAGE_DATA_DIRECTORY directory = module.headers.OptionalHeader.DataDirectory[5];
            int cnt = 0;
            if (directory.Size > 0)
            {
                var relocation = PointerHelpers.ToStruct<IMAGE_BASE_RELOCATION>(codeBase, directory.VirtualAddress);
                while (relocation.VirtualAddress > 0)
                {
                    unsafe
                    {
                        var dest = (IntPtr)(codeBase.ToInt32() + (int)relocation.VirtualAddress);
                        var relInfo = (ushort*)(codeBase.ToInt32() + (int)directory.VirtualAddress + sizeOfBase);
                        uint i;
                        for (i = 0; i < ((relocation.SizeOfBlock - Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION))) / 2); i++, relInfo++)
                        {
                            int type = *relInfo >> 12;
                            int offset = (*relInfo & 0xfff);
                            switch (type)
                            {
                                case 0x00:
                                    break;
                                case 0x03:
                                    var patchAddrHl = (uint*)((dest).ToInt32() + (offset));
                                    *patchAddrHl += delta;
                                    break;
                            }
                        }
                    }
                    cnt += (int)relocation.SizeOfBlock;
                    relocation = PointerHelpers.ToStruct<IMAGE_BASE_RELOCATION>(codeBase, (uint)(directory.VirtualAddress + cnt));

                }
            }
        }
        public void PerformBaseRelocation64(UInt64 delta)
        {
            IntPtr codeBase = module64.codeBase;
            int sizeOfBase = Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION));
            IMAGE_DATA_DIRECTORY directory = module64.headers.OptionalHeader.DataDirectory[5];
            int cnt = 0;
            if (directory.Size > 0)
            {
                var relocation = PointerHelpers.ToStruct64<IMAGE_BASE_RELOCATION>(codeBase, directory.VirtualAddress);
                while (relocation.VirtualAddress > 0)
                {
                    unsafe
                    {
                        var dest = (IntPtr)(codeBase.ToInt64() + (int)relocation.VirtualAddress);
                        var relInfo = (ushort*)(codeBase.ToInt64() + (int)directory.VirtualAddress + sizeOfBase);
                        uint i;
                        for (i = 0; i < ((relocation.SizeOfBlock - Marshal.SizeOf(typeof(IMAGE_BASE_RELOCATION))) / 2); i++, relInfo++)
                        {
                            int type = *relInfo >> 12;
                            int offset = (*relInfo & 0xfff);
                            switch (type)
                            {
                                case 0x00:
                                    break;
                                case 0x03:
                                    var patchAddrHl = (UInt64*)((dest).ToInt64() + (offset));
                                    *patchAddrHl += delta;
                                    break;
                            }
                        }
                    }
                    cnt += (int)relocation.SizeOfBlock;
                    relocation = PointerHelpers.ToStruct64<IMAGE_BASE_RELOCATION>(codeBase, (uint)(directory.VirtualAddress + cnt));

                }
            }
        }

        private MEMORYMODULE module;
        private MEMORYMODULE64 module64;
        public long GetProcAddress(string name)
        {
            unsafe
            {
                if (Marshal.SizeOf(typeof(IntPtr)) == 0x4)
                {
                    var addr = _GetProcAddress(name);
                    return addr;
                }
                else
                {
                    var addr = _GetProcAddress64(name);
                    return addr;
                }
            }
        }
        public uint _GetProcAddress(string name)
        {
            unsafe
            {
                IntPtr codeBase = module.codeBase;
                int idx = -1;
                uint i;
                IMAGE_DATA_DIRECTORY directory = module.headers.OptionalHeader.DataDirectory[0];
                if (directory.Size == 0)
                    return 0;
                var exports = PointerHelpers.ToStruct<IMAGE_EXPORT_DIRECTORY>(codeBase, directory.VirtualAddress);
                var nameRef = (uint*)new IntPtr(codeBase.ToInt32() + exports.AddressOfNames);
                var ordinal = (ushort*)new IntPtr(codeBase.ToInt32() + exports.AddressOfNameOrdinals);
                for (i = 0; i < exports.NumberOfNames; i++, nameRef++, ordinal++)
                {
                    var str = codeBase.ToInt32() + (int)(*nameRef);
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    if (tmp == name)
                    {
                        idx = *ordinal;
                        break;
                    }
                }

                var tmpaa = (uint*)(codeBase.ToInt32() + (exports.AddressOfFunctions + (idx * 4)));
                var addr = (uint)((codeBase.ToInt32()) + (*tmpaa));
                return addr;
            }
        }
        public long _GetProcAddress64(string name)
        {
            unsafe
            {
                IntPtr codeBase = module64.codeBase;
                int idx = -1;
                uint i;
                IMAGE_DATA_DIRECTORY directory = module64.headers.OptionalHeader.DataDirectory[0];
                if (directory.Size == 0)
                    return 0;
                var exports = PointerHelpers.ToStruct64<IMAGE_EXPORT_DIRECTORY>(codeBase, directory.VirtualAddress);
                var nameRef = (uint*)new IntPtr(codeBase.ToInt64() + exports.AddressOfNames);
                var ordinal = (ushort*)new IntPtr(codeBase.ToInt64() + exports.AddressOfNameOrdinals);
                for (i = 0; i < exports.NumberOfNames; i++, nameRef++, ordinal++)
                {
                    var str = codeBase.ToInt64() + (int)(*nameRef);
                    string tmp = Marshal.PtrToStringAnsi((IntPtr)str);
                    if (tmp == name)
                    {
                        idx = *ordinal;
                        break;
                    }
                }

                var tmpaa = (uint*)(codeBase.ToInt64() + (exports.AddressOfFunctions + (idx * 4)));
                var addr = (long)((codeBase.ToInt64()) + (*tmpaa));
                return addr;
            }
        }

        public void CopySections(byte[] data, IMAGE_NT_HEADERS32 oldHeaders, IntPtr headers, IMAGE_DOS_HEADER dosHeader)
        {
            int i;
            IntPtr codebase = module.codeBase;
            var section = PointerHelpers.ToStruct<IMAGE_SECTION_HEADER>(headers, (uint)(24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader));
            for (i = 0; i < module.headers.FileHeader.NumberOfSections; i++)
            {
                IntPtr dest;
                if (section.SizeOfRawData == 0)
                {
                    uint size = oldHeaders.OptionalHeader.SectionAlignment;
                    if (size > 0)
                    {
                        dest = new IntPtr((Win32Imports.VirtualAlloc((uint)(codebase.ToInt32() + (int)section.VirtualAddress), size, Win32Constants.MEM_COMMIT,
                                                     Win32Constants.PAGE_READWRITE)));

                        section.PhysicalAddress = (uint)dest;
                        var write = new IntPtr(headers.ToInt32() + (32 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i)));
                        Marshal.WriteInt32(write, (int)dest);
                        var datazz = new byte[size + 1];
                        Marshal.Copy(datazz, 0, dest, (int)size);
                    }
                    section = PointerHelpers.ToStruct<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
                    continue;
                }

                dest = new IntPtr((Win32Imports.VirtualAlloc((uint)(codebase.ToInt32() + (int)section.VirtualAddress), section.SizeOfRawData, Win32Constants.MEM_COMMIT,
                                             Win32Constants.PAGE_READWRITE)));
                Marshal.Copy(data, (int)section.PointerToRawData, dest, (int)section.SizeOfRawData);
                section.PhysicalAddress = (uint)dest;
                var write2 = new IntPtr(headers.ToInt32() + (32 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i)));
                Marshal.WriteInt32(write2, (int)dest);
                section = PointerHelpers.ToStruct<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
            }
        }
        public void CopySections64(byte[] data, IMAGE_NT_HEADERS64 oldHeaders, IntPtr headers, IMAGE_DOS_HEADER dosHeader)
        {
            int i;
            IntPtr codebase = module64.codeBase;
            var section = PointerHelpers.ToStruct64<IMAGE_SECTION_HEADER>(headers, (uint)(24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader));
            for (i = 0; i < module64.headers.FileHeader.NumberOfSections; i++)
            {
                IntPtr dest;
                if (section.SizeOfRawData == 0)
                {
                    uint size = oldHeaders.OptionalHeader.SectionAlignment;
                    if (size > 0)
                    {
                        dest = new IntPtr((Win32Imports.VirtualAlloc((uint)(codebase.ToInt64() + (int)section.VirtualAddress), size, Win32Constants.MEM_COMMIT,
                                                     Win32Constants.PAGE_READWRITE)));

                        section.PhysicalAddress = (uint)dest;
                        var write = new IntPtr(headers.ToInt64() + (32 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i)));
                        Marshal.WriteInt64(write, (int)dest);
                        var datazz = new byte[size + 1];
                        Marshal.Copy(datazz, 0, dest, (int)size);
                    }
                    section = PointerHelpers.ToStruct64<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
                    continue;
                }

                dest = (IntPtr)Win32Imports.VirtualAlloc((ulong)(codebase.ToInt64() + (long)section.VirtualAddress), (ulong)section.SizeOfRawData, (ulong)(Win32Constants.MEM_COMMIT),
                                             (ulong)Win32Constants.PAGE_READWRITE);
                Marshal.Copy(data, (int)section.PointerToRawData, dest, (int)section.SizeOfRawData);
                section.PhysicalAddress = (uint)(dest.ToInt64() - codebase.ToInt64());
                var write2 = new IntPtr(headers.ToInt64() + (32 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i)));
                Marshal.WriteInt64(write2, dest.ToInt64());
                section = PointerHelpers.ToStruct64<IMAGE_SECTION_HEADER>(headers, (uint)((24 + dosHeader.e_lfanew + oldHeaders.FileHeader.SizeOfOptionalHeader) + (Marshal.SizeOf(typeof(IMAGE_SECTION_HEADER)) * (i + 1))));
            }
        }
    }
}
