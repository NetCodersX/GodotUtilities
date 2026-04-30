// AUTO-GENERATED — do not edit manually
using Godot;

namespace Utilities.AudioManagement;

public partial class AudioManager
{
	public enum SfxId { None = -1, Digging, Planted, RockHit, TreeFalling, TreeHit, WaterCan }

	public enum MusicId { None = -1, GentleBreeze, GoldenGleam }

	public enum AmbienceId { None = -1,  }

	public static class SfxName
	{
		public static readonly StringName Digging = "digging";
		public static readonly StringName Planted = "planted";
		public static readonly StringName RockHit = "rock_hit";
		public static readonly StringName TreeFalling = "tree_falling";
		public static readonly StringName TreeHit = "tree_hit";
		public static readonly StringName WaterCan = "water_can";
	}

	public static class MusicName
	{
		public static readonly StringName GentleBreeze = "gentle_breeze";
		public static readonly StringName GoldenGleam = "golden_gleam";
	}

	public static class AmbienceName
	{
	}

	private static readonly StringName[] _sfxNames = { SfxName.Digging, SfxName.Planted, SfxName.RockHit, SfxName.TreeFalling, SfxName.TreeHit, SfxName.WaterCan };

	private static readonly StringName[] _musicNames = { MusicName.GentleBreeze, MusicName.GoldenGleam };

	private static readonly StringName[] _ambienceNames = {  };

	public AudioStreamPlayer   PlaySfxWithId(SfxId id, float pitchVariance = 0f)
		{ if (id == SfxId.None) return null; return PlaySfx(_sfxNames[(int)id], pitchVariance); }
	public AudioStreamPlayer2D PlaySfx2DWithId(SfxId id, Vector2 pos, float pitchVariance = 0f)
		{ if (id == SfxId.None) return null; return PlaySfx2D(_sfxNames[(int)id], pos, pitchVariance); }
	public void PlayMusicWithId(MusicId id, float fadeDuration = 1.5f)
		{ if (id == MusicId.None) return; PlayMusic(_musicNames[(int)id], fadeDuration); }
	public void ThenPlayMusicWithId(MusicId id, float fadeDuration = 1.5f)
		{ if (id == MusicId.None) return; ThenPlayMusic(_musicNames[(int)id], fadeDuration); }
	public void PlayAmbienceWithId(AmbienceId id, float fadeDuration = 3f)
		{ if (id == AmbienceId.None) return; PlayAmbience(_ambienceNames[(int)id], fadeDuration); }
	public void ThenPlayAmbienceWithId(AmbienceId id, float fadeDuration = 3f)
		{ if (id == AmbienceId.None) return; ThenPlayAmbience(_ambienceNames[(int)id], fadeDuration); }

}