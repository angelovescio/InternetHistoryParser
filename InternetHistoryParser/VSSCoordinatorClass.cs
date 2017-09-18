// Type: VSS.VSSCoordinatorClass
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VSS
{
  [ComImport, Guid("E579AB5F-1CC4-44B4-BED9-DE0991FF0623")]
  public class VSSCoordinatorClass : IVssCoordinator, VSSCoordinator, IVssAdmin
  {
      extern void IVssCoordinator.SetContext(int lContext);
      
      extern void IVssCoordinator.StartSnapshotSet(out Guid pSnapshotSetId);

      extern void IVssCoordinator.AddToSnapshotSet(string pwszVolumeName, Guid ProviderId, out Guid pSnapshotId);
      
      extern void IVssCoordinator.DoSnapshotSet(object pWriterCallback, out IVssAsync ppAsync);
      
      extern void IVssCoordinator.GetSnapshotProperties(Guid SnapshotId, out _VSS_SNAPSHOT_PROP pProp);
      
      extern void IVssCoordinator.ExposeSnapshot(Guid SnapshotId, string wszPathFromRoot, int lAttributes, string wszExpose, out string pwszExposed);
      
      extern void IVssCoordinator.RemountReadWrite(Guid SnapshotId, out IVssAsync ppAsync);
      
      extern void IVssCoordinator.ImportSnapshots(string bstrXMLSnapshotSet, out IVssAsync ppAsync);
     
      extern void IVssCoordinator.Query(Guid QueriedObjectId, _VSS_OBJECT_TYPE eQueriedObjectType, _VSS_OBJECT_TYPE eReturnedObjectsType, out IVssEnumObject ppenum);
      
      extern void IVssCoordinator.DeleteSnapshots(Guid SourceObjectId, _VSS_OBJECT_TYPE eSourceObjectType, int bForceDelete, out int plDeletedSnapshots, out Guid pNondeletedSnapshotID);
      
      extern void IVssCoordinator.BreakSnapshotSet(Guid SnapshotSetId);
      
      extern void IVssCoordinator.IsVolumeSupported(Guid ProviderId, string pwszVolumeName, out int pbSupportedByThisProvider);
      
      extern void IVssCoordinator.IsVolumeSnapshotted(Guid ProviderId, string pwszVolumeName, out int pbSnapshotsPresent, out int plSnapshotCompatibility);
      
      extern void IVssCoordinator.SetWriterInstances(int lWriterInstanceIdCount, ref Guid rgWriterInstanceId);

      extern void IVssAdmin.RegisterProvider(Guid pProviderId, Guid ClassId, string pwszProviderName, _VSS_PROVIDER_TYPE eProviderType, string pwszProviderVersion, Guid ProviderVersionId);
      
      extern void IVssAdmin.UnregisterProvider(Guid ProviderId);
      
      extern void IVssAdmin.QueryProviders(out IVssEnumObject ppenum);
      
      extern void IVssAdmin.AbortAllSnapshotsInProgress();
      
  }
}
