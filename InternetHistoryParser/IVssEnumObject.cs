// Type: VSS.IVssEnumObject
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSS
{
  [InterfaceType((short) 1)]
  [Guid("AE1C7110-2F60-11D3-8A39-00C04F72D8E3")]
  [ComImport]
  public interface IVssEnumObject
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Next([In] uint celt, out _VSS_OBJECT_PROP rgelt, out uint pceltFetched);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Skip([In] uint celt);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Reset();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Clone([MarshalAs(UnmanagedType.Interface), In, Out] ref IVssEnumObject ppenum);
  }
}
