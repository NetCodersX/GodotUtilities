// AUTO-GENERATED — do not edit manually
using Godot;

namespace Utilities.ParticlesManagement;

public partial class ParticlesManager
{
	public enum ParticleId { None = -1, BushHit, DirtDust, GroundHit, LeavesFall, RockHit, WoodHit }

	public static class ParticleName
	{
		public static readonly StringName BushHit = "bush_hit";
		public static readonly StringName DirtDust = "dirt_dust";
		public static readonly StringName GroundHit = "ground_hit";
		public static readonly StringName LeavesFall = "leaves_fall";
		public static readonly StringName RockHit = "rock_hit";
		public static readonly StringName WoodHit = "wood_hit";
	}

	private static readonly StringName[] _particleNames = { ParticleName.BushHit, ParticleName.DirtDust, ParticleName.GroundHit, ParticleName.LeavesFall, ParticleName.RockHit, ParticleName.WoodHit };

	public PooledParticle SpawnById(ParticleId id, Vector2 pos) { if (id == ParticleId.None) return null; return Spawn(_particleNames[(int)id], pos); }
}