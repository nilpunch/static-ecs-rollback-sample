using UnityEngine;
using ILogger = Shenanicode.Rollback.ILogger;

namespace Game.Client {
	public class UnityLogger : ILogger {
		private readonly string _prefix;

		public UnityLogger(string prefix = "") {
			_prefix = string.IsNullOrEmpty(prefix) ? "" : $"[{prefix}] ";
		}

		public void Log(string message) => Debug.Log($"{_prefix}{message}");

		public void Warn(string message) => Debug.LogWarning($"{_prefix}{message}");

		public void Error(string message) => Debug.LogError($"{_prefix}{message}");
	}
}
