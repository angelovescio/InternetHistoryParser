// Type: VSS._VSS_PROVIDER_PROP
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System;
using System.Runtime.InteropServices;

namespace VSS
{
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct _VSS_PROVIDER_PROP
  {
    public Guid m_ProviderId;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszProviderName;
    public _VSS_PROVIDER_TYPE m_eProviderType;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszProviderVersion;
    public Guid m_ProviderVersionId;
    public Guid m_ClassId;
  }
}
