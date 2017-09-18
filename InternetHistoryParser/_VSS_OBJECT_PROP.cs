// Type: VSS._VSS_OBJECT_PROP
// Assembly: Interop.VSS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 891D12FB-73EB-48CD-B6C5-8263EA58F00F
// Assembly location: C:\Users\vesh\Documents\Visual Studio 2012\Projects\InternetHistoryParser\InternetHistoryParser\bin\Release\Interop.VSS.dll

using System.Runtime.InteropServices;

namespace VSS
{
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
  public struct _VSS_OBJECT_PROP
  {
    public _VSS_OBJECT_TYPE Type;
    [ComAliasName("VSS.VSS_OBJECT_UNION")]
    public VSS_OBJECT_UNION Obj;
  }
}
