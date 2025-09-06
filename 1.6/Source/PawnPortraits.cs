using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits {
	public class PawnPortraits : IExposable {
		private List<PortraitCondition> conditions;
		public List<PortraitCondition> Conditions {
			get {
				if (conditions == null) conditions = new List<PortraitCondition>();
				return conditions;
			}
		}

		public bool IsSimple { get; private set; } = false;
		private PortraitCondition simpleDefault = null;
		private PortraitCondition simpleInspector = null;
		private PortraitCondition simpleColonistBar = null;
		private PortraitCondition simpleTopRight = null;
		private PortraitCondition simpleActions = null;
		private PortraitCondition simpleCustom = null;

		private string cacheDefault = null;
		private string cacheInspector = null;
		private string cacheColonistBar = null;
		private string cacheTopRight = null;
		private string cacheActions = null;
		private string cacheCustom = null;

		public bool HasFilename(PortraitPosition? position) {
			return GetFilenameExact(position) != null;
		}
		public string GetFilenameExact(PortraitPosition? position) {
			switch (position) {
				case PortraitPosition.Inspector: return cacheInspector;
				case PortraitPosition.ColonistBar: return cacheColonistBar;
				case PortraitPosition.TopRight: return cacheTopRight;
				case PortraitPosition.Actions: return cacheActions;
				case PortraitPosition.Custom: return cacheCustom;
				default: return cacheDefault;
			}
		}
		public string GetFilename(PortraitPosition? position) {
			return GetFilenameExact(position) ?? cacheDefault;
		}
		private void CacheFilename(PortraitPosition? position, string value) {
			switch (position) {
				case PortraitPosition.Inspector: cacheInspector = value; break;
				case PortraitPosition.ColonistBar: cacheColonistBar = value; break;
				case PortraitPosition.TopRight: cacheTopRight = value; break;
				case PortraitPosition.Actions: cacheActions = value; break;
				case PortraitPosition.Custom: cacheCustom = value; break;
				default: cacheDefault = value; break;
			}
		}

		public void Update(Pawn p) {
			if (p == null) return;
			cacheDefault = null;
			cacheInspector = null;
			cacheColonistBar = null;
			cacheTopRight = null;
			cacheActions = null;
			cacheCustom = null;

			foreach (PortraitCondition pc in Conditions) {
				if (HasFilename(pc.position)) continue;
				if (!pc.CheckFor(p)) continue;
				CacheFilename(pc.position, pc.filename);
			}
		}

		private PortraitCondition GetSimpleForPosition(PortraitPosition? position) {
			switch (position) {
				case PortraitPosition.Inspector: return simpleInspector;
				case PortraitPosition.ColonistBar: return simpleColonistBar;
				case PortraitPosition.TopRight: return simpleTopRight;
				case PortraitPosition.Actions: return simpleActions;
				case PortraitPosition.Custom: return simpleCustom;
				default: return simpleDefault;
			}
		}
		public void SetSimple(PortraitPosition? position, string value) {
			if (!IsSimple) {
				Conditions.Clear();
				Conditions.Add(simpleDefault = new ConstantPortraitCondition(null, null));
				Conditions.Add(simpleInspector = new ConstantPortraitCondition(PortraitPosition.Inspector, null));
				Conditions.Add(simpleColonistBar = new ConstantPortraitCondition(PortraitPosition.ColonistBar, null));
				Conditions.Add(simpleTopRight = new ConstantPortraitCondition(PortraitPosition.TopRight, null));
				Conditions.Add(simpleActions = new ConstantPortraitCondition(PortraitPosition.Actions, null));
				Conditions.Add(simpleCustom = new ConstantPortraitCondition(PortraitPosition.Custom, null));
				IsSimple = true;
			}
			GetSimpleForPosition(position).filename = value;
			CacheFilename(position, value);
		}

		public void ExposeData() {
			Scribe_Collections.Look(ref conditions, "pawn_portraits");
		}
	}
}
