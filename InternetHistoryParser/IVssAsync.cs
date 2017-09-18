// Type: VSS.IVssAsync
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSS
{
  [InterfaceType((short) 1)]
  [Guid("C7B98A22-222D-4E62-B875-1A44980634AF")]
  [ComImport]
  public interface IVssAsync
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Cancel();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Wait();

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void QueryStatus([MarshalAs(UnmanagedType.Error)] out int pHrResult, [In, Out] ref int pReserved);
  }
}
