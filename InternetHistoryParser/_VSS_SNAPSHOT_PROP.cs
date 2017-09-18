// Type: VSS._VSS_SNAPSHOT_PROP
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System;
using System.Runtime.InteropServices;

namespace VSS
{
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct _VSS_SNAPSHOT_PROP
  {
    public Guid m_SnapshotId;
    public Guid m_SnapshotSetId;
    public int m_lSnapshotsCount;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszSnapshotDeviceObject;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszOriginalVolumeName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszOriginatingMachine;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszServiceMachine;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszExposedName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string m_pwszExposedPath;
    public Guid m_ProviderId;
    public int m_lSnapshotAttributes;
    public long m_tsCreationTimestamp;
    public _VSS_SNAPSHOT_STATE m_eStatus;
  }
}
