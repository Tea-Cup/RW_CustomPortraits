using Verse;

namespace Foxy.CustomPortraits {
	public abstract class PortraitCondition : IExposable {
		public abstract string TranslationKey { get; }
		public PortraitPosition? position = null;
		public string filename = null;

		public abstract bool CheckFor(Pawn p);

		public virtual string GetTexture(Pawn p) {
			return filename;
		}

		public virtual void ExposeData() {
			Scribe_Values.Look(ref position, "position", null);
			Scribe_Values.Look(ref filename, "filename", null);
		}

		public override string ToString() {
			return $"{{{GetType().Name} {position?.ToString() ?? "<null>"} \"{filename.ToStringSafe()}\"}}";
		}
	}
}
