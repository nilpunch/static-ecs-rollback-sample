using FFS.Libraries.StaticEcs.Unity.Editor;
using UnityEditor;

namespace Game.Client.Editor {
	public class WStaticEcsView : StaticEcsView<ClientWorldType, WEntity, WEvent> {
		[MenuItem("Tools/Static ECS/World View")]
		public static void Show() {
			CreateWindow<WStaticEcsView>().Init();
		}
	}
}
