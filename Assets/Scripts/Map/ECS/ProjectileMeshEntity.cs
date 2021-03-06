﻿using System.Collections.Generic;
using System.Linq;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

using UnityEngine;

public class ProjectileMeshEntity : PhysicsMeshEntity
{
	[Header("Projectile Settings")]
	public Faction faction;
	public bool friendlyFire;
	public float damage;

	public override IEnumerable<ComponentType> GetComponents()
	{
		return base.GetComponents().Concat(new ComponentType[] {
				typeof(Damage),
				typeof(FactionId),
			});
	}

	public override void PrepareDefaultComponentData(Entity entity)
	{
		base.PrepareDefaultComponentData(entity);
		Map.EM.SetComponentData(entity, new Damage
		{
			Value = damage,
			friendlyFire = friendlyFire
		});
		Map.EM.SetComponentData(entity, new FactionId
		{
			Value = faction
		});
	}

	public Entity Instantiate(float3 position, float scale, float3 velocity = default, float3 angularVelocity = default)
	{
		var rot = (velocity.Equals(default) ? quaternion.identity : quaternion.LookRotation(velocity, new float3(0, 1, 0)));
		var e = Instantiate(position, rot, scale, velocity, angularVelocity);
		return e;
	}

	public Entity BufferedInstantiate(EntityCommandBuffer cmb, float3 position, float scale, float3 velocity = default, float3 angularVelocity = default)
	{
		var rot = (velocity.Equals(default) ? quaternion.identity : quaternion.LookRotation(velocity, new float3(0, 1, 0)));
		var e = BufferedInstantiate(cmb, position, rot, scale, velocity, angularVelocity);
		return e;
	}

	protected override CollisionFilter GetFilter() => new CollisionFilter
	{
		BelongsTo = (1u << (int)faction),
		CollidesWith = ~((1u << (int)Faction.PhosProjectile) | (1u << (int)Faction.PlayerProjectile)),
		GroupIndex = 0
	};
}