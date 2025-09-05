using Foxy.CustomPortraits;
using Verse;

public class Comp_FoxyPawnCustomPortrait : ThingComp {
	private PawnPortraits storage;
	public PawnPortraits Storage {
		get {
			if (storage == null) storage = new PawnPortraits();
			return storage;
		}
	}

	public override void CompTickLong() {
		Storage.Update(parent as Pawn);
	}

	public string this[PortraitPosition? position] {
		get => Storage[position];
		set => Storage[position] = value;
	}

	public bool HasFilename(PortraitPosition? position) {
		return Storage.HasFilename(position);
	}

	public string GetFilename(PortraitPosition? position) {
		return Storage.GetFilename(position);
	}

	public void SetFilename(PortraitPosition? position, string value) {
		Storage.SetFilename(position, value);
	}

	private static bool MigrateLegacy(ref PawnPortraits storage) {
		if (Scribe.mode != LoadSaveMode.LoadingVars) return false;
		if (Scribe.loader.EnterNode("pawn_portrait")) {
			// Already migrated
			Scribe.loader.ExitNode();
			return false;
		}

		LegacyPawnPortraits legacy = new LegacyPawnPortraits();
		legacy.ExposeData();

		if (storage == null) storage = new PawnPortraits();
		storage.filename = legacy.filename;

		return true;
	}

	public override void PostExposeData() {
		base.PostExposeData();
		if (!MigrateLegacy(ref storage))
			Scribe_Deep.Look(ref storage, "pawn_portrait");
	}
}
