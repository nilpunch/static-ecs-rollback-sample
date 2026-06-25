using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Client.UI
{
	public class MainMenuUI : AppBehaviour<MainMenuUI> {
		[field: SerializeField] public UIDocument Document { get; private set; }
	}
}
