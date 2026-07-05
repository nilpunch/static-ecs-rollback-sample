using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class ClientStabilityPlotter : MonoBehaviour {
		private const int Samples = 240; // ~4s at 60fps
		private readonly float[] _lead = new float[Samples];
		private int _head;

		private static Texture2D _px;

		private static Texture2D Px {
			get {
				if (_px == null) {
					_px = new Texture2D(1, 1);
					_px.SetPixel(0, 0, Color.white);
					_px.Apply();
				}
				return _px;
			}
		}

		private void Update() {
			var sync = CLNT.TickSync;
			_lead[_head] = (float)(sync.SmoothedLeadTime * sync.TickRate);
			_head = (_head + 1) % Samples;
		}

		private void OnDestroy() {
			if (_px != null) {
				DestroyImmediate(_px);
			}
		}

		private void OnGUI() {
			var sync = CLNT.TickSync;

			// graph bounds in tick units
			const float lo = -5f, hi = 5f;
			var rect = new Rect(10, 10, Samples, 120);

			Box(rect, new Color(0, 0, 0, 0.6f));
			// reference lines: target buffer (green) and zero (red = drop threshold)
			HLine(rect, lo, hi, 0f, new Color(1f, 0.3f, 0.3f, 0.5f));

			for (var i = 0; i < Samples; i++) {
				var idx = (_head + i) % Samples;
				var x = rect.x + i;
				Dot(x, Map(rect, lo, hi, _lead[idx]), new Color(0.4f, 0.8f, 1f, 1f)); // smoothed lead
			}

			GUI.Label(new Rect(14, 134, 400, 20),
				$"lead={sync.SmoothedLeadTime * 1000:F1}ms (set {sync.PredictionLeadTime * 1000:F1}ms)");
		}

		private static float Map(Rect r, float lo, float hi, float v) =>
			r.yMax - (Mathf.Clamp(v, lo, hi) - lo) / (hi - lo) * r.height;

		private static void HLine(Rect r, float lo, float hi, float v, Color c) {
			var y = Map(r, lo, hi, v);
			Box(new Rect(r.x, y, r.width, 1), c);
		}

		private static void Dot(float x, float y, Color c) => Box(new Rect(x, y - 1, 1, 2), c);

		private static void Box(Rect r, Color c) {
			var old = GUI.color;
			GUI.color = c;
			GUI.DrawTexture(r, Px);
			GUI.color = old;
		}
	}
}
