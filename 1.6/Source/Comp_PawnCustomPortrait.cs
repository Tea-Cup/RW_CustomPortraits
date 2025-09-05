using Foxy.CustomPortraits;
using Verse;

public class Comp_FoxyPawnCustomPortrait : ThingComp {
	private int last_tick = 0;
	private PawnPortraits storage;
	public PawnPortraits Storage {
		get {
			if (storage == null) storage = new PawnPortraits();
			return storage;
		}
	}

	public override void Initialize(CompProperties props) {
		base.Initialize(props);
	}

	public override void CompTick() {
		if (Find.TickManager.TicksGame - last_tick < 60) return;
		last_tick = Find.TickManager.TicksGame;
		Storage.Update(parent as Pawn);
	}

	private static bool MigrateLegacy(ref PawnPortraits storage) {
		if (Scribe.mode != LoadSaveMode.LoadingVars) return false;
		if (Scribe.loader.EnterNode("pawn_portraits")) {
			// Already migrated
			Scribe.loader.ExitNode();
			return false;
		}

		LegacyPawnPortraits legacy = new LegacyPawnPortraits();
		legacy.ExposeData();

		if (storage == null) storage = new PawnPortraits();
		if (legacy.filename != null) storage.SetSimple(null, legacy.filename);
		if (legacy.inspector != null) storage.SetSimple(PortraitPosition.Inspector, legacy.inspector);
		if (legacy.colonistBar != null) storage.SetSimple(PortraitPosition.ColonistBar, legacy.colonistBar);
		if (legacy.topRight != null) storage.SetSimple(PortraitPosition.TopRight, legacy.topRight);
		if (legacy.actions != null) storage.SetSimple(PortraitPosition.Actions, legacy.actions);
		if (legacy.custom != null) storage.SetSimple(PortraitPosition.Custom, legacy.custom);

		return true;
	}

	public override void PostExposeData() {
		base.PostExposeData();
		if (!MigrateLegacy(ref storage))
			Storage.ExposeData();
	}
}
