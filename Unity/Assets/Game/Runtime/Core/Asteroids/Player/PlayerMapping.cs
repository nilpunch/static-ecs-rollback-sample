using System;
using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using FFS.Libraries.StaticPack;

namespace Game.Core {
	public class PlayerMapping : IResource {
		public Dictionary<ushort, EntityGID> EntityByChannel = new();

		public Guid? Guid() => new Guid("1fb614f49408cfc755978cd5be71dc4e");

		public void Write(ref BinaryPackWriter writer) {
			writer.WriteDictionary(EntityByChannel);
		}

		public void Read(ref BinaryPackReader reader, byte version) {
			reader.ReadDictionary(ref EntityByChannel);
		}
	}
}
