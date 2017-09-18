// Type: VSS.IVssCoordinator
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSS
{
  [InterfaceType((short) 1)]
  [Guid("93BA4344-AA56-403E-87F2-819650FEDACD")]
  [ComImport]
  public interface IVssCoordinator
  {
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetContext([In] int lContext);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void StartSnapshotSet(out Guid pSnapshotSetId);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void AddToSnapshotSet([MarshalAs(UnmanagedType.LPWStr), In] string pwszVolumeName, [In] Guid ProviderId, out Guid pSnapshotId);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DoSnapshotSet([MarshalAs(UnmanagedType.IDispatch), In] object pWriterCallback, [MarshalAs(UnmanagedType.Interface)] out IVssAsync ppAsync);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void GetSnapshotProperties([In] Guid SnapshotId, out _VSS_SNAPSHOT_PROP pProp);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ExposeSnapshot([In] Guid SnapshotId, [MarshalAs(UnmanagedType.LPWStr), In] string wszPathFromRoot, [In] int lAttributes, [MarshalAs(UnmanagedType.LPWStr), In] string wszExpose, [MarshalAs(UnmanagedType.LPWStr)] out string pwszExposed);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void RemountReadWrite([In] Guid SnapshotId, [MarshalAs(UnmanagedType.Interface)] out IVssAsync ppAsync);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void ImportSnapshots([MarshalAs(UnmanagedType.BStr), In] string bstrXMLSnapshotSet, [MarshalAs(UnmanagedType.Interface)] out IVssAsync ppAsync);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void Query([In] Guid QueriedObjectId, [In] _VSS_OBJECT_TYPE eQueriedObjectType, [In] _VSS_OBJECT_TYPE eReturnedObjectsType, [MarshalAs(UnmanagedType.Interface)] out IVssEnumObject ppenum);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void DeleteSnapshots([In] Guid SourceObjectId, [In] _VSS_OBJECT_TYPE eSourceObjectType, [In] int bForceDelete, out int plDeletedSnapshots, out Guid pNondeletedSnapshotID);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void BreakSnapshotSet([In] Guid SnapshotSetId);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsVolumeSupported([In] Guid ProviderId, [MarshalAs(UnmanagedType.LPWStr), In] string pwszVolumeName, out int pbSupportedByThisProvider);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void IsVolumeSnapshotted([In] Guid ProviderId, [MarshalAs(UnmanagedType.LPWStr), In] string pwszVolumeName, out int pbSnapshotsPresent, out int plSnapshotCompatibility);

    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    void SetWriterInstances([In] int lWriterInstanceIdCount, [In] ref Guid rgWriterInstanceId);

  }
}
