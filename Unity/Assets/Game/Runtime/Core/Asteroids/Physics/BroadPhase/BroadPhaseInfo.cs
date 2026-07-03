using System.Runtime.InteropServices;
using FFS.Libraries.StaticEcs;

namespace Game.Core {
	[StructLayout(LayoutKind.Sequential)]
	public struct BroadPhaseInfo : IComponent, ITrackableChanged {
		public CellIndex LowerBound;
		public CellIndex UpperBound;
		public uint QueryId;
	}
}
