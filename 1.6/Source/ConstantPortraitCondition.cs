using Verse;

namespace Foxy.CustomPortraits {
	public class ConstantPortraitCondition : PortraitCondition {
		public override string TranslationKey => "Foxy.CustomPortraits.Condition.Constant";

		public ConstantPortraitCondition() { }
		public ConstantPortraitCondition(PortraitPosition? position, string filename) {
			this.position = position;
			this.filename = filename;
		}

		public override bool CheckFor(Pawn p) {
			return filename != null;
		}
	}
}
