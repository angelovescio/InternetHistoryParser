// Type: VSS._VSS_SNAPSHOT_STATE
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

namespace VSS
{
  public enum _VSS_SNAPSHOT_STATE
  {
    VSS_SS_UNKNOWN,
    VSS_SS_PREPARING,
    VSS_SS_PROCESSING_PREPARE,
    VSS_SS_PREPARED,
    VSS_SS_PROCESSING_PRECOMMIT,
    VSS_SS_PRECOMMITTED,
    VSS_SS_PROCESSING_COMMIT,
    VSS_SS_COMMITTED,
    VSS_SS_PROCESSING_POSTCOMMIT,
    VSS_SS_CREATED,
    VSS_SS_ABORTED,
    VSS_SS_DELETED,
    VSS_SS_COUNT,
  }
}
