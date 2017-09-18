// Type: VSS.IVssAdmin
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSS
{
  [InterfaceType((short) 1)]
  [Guid("77ED5996-2F63-11D3-8A39-00C04F72D8E3")]
  [ComImport]
  public interface IVssAdmin
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RegisterProvider([In] Guid pProviderId, [In] Guid ClassId, [MarshalAs(UnmanagedType.LPWStr), In] string pwszProviderName, [In] _VSS_PROVIDER_TYPE eProviderType, [MarshalAs(UnmanagedType.LPWStr), In] string pwszProviderVersion, [In] Guid ProviderVersionId);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void UnregisterProvider([In] Guid ProviderId);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void QueryProviders([MarshalAs(UnmanagedType.Interface)] out IVssEnumObject ppenum);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AbortAllSnapshotsInProgress();
  }
}
