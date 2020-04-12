﻿using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

using UnityEngine;

public class MobileUnit : IMoveable, IAttackState, IAttack, IGroundFire, IGuard, IRepairable
{
	public int id;
	public MobileUnitEntity info;

	public HexCoords Coords { get; protected set; }

	public Vector3 Position
	{
		get => Map.EM.GetComponentData<Translation>(Entity).Value;
		set
		{
			UpdatePos(value);
		}
	}

	public Entity Entity;
	public Entity HeadEntity;
	public Vector3 _position;
	public bool IsRendered { get; protected set; }

	private bool _isShown;
	private Faction _faction;
	private NativeArray<Entity> _healhBar;

	public MobileUnit(int id, MobileUnitEntity info, Tile tile, Faction faction)
	{
		this.id = id;
		this.info = info;
		_position = tile.SurfacePoint;
		Coords = tile.Coords;
		_faction = faction;
	}

	public Entity Render()
	{
		if (IsRendered)
			return Entity;
		IsRendered = _isShown = true;
		Entity = info.Instantiate(_position, Quaternion.identity, id, _faction);
		if (info.head != null)
			HeadEntity = info.head.Instantiate(_position, new float3(1, 1, 1), Quaternion.identity);
		Map.EM.SetComponentData(Entity, new FactionId { Value = _faction });
		if(info.healthBar != null)
			_healhBar = info.healthBar.Instantiate(Entity, info.centerOfMassOffset + info.healthBarOffset);
		return Entity;
	}

	public void UpdatePos(Vector3 pos)
	{
		_position = pos;
		Coords = HexCoords.FromPosition(pos, Map.ActiveMap.tileEdgeLength);
	}

	public void Show(bool isShown)
	{
		if (isShown == _isShown)
			return;
		if (_isShown = isShown)
		{
			Map.EM.RemoveComponent(Entity, typeof(FrozenRenderSceneTag));
			Map.EM.RemoveComponent(HeadEntity, typeof(FrozenRenderSceneTag));
			if(_healhBar.IsCreated)
				Map.EM.RemoveComponent(_healhBar, typeof(FrozenRenderSceneTag));
		}
		else
		{
			Map.EM.AddComponent(Entity, typeof(FrozenRenderSceneTag));
			Map.EM.AddComponent(HeadEntity, typeof(FrozenRenderSceneTag));
			if(_healhBar.IsCreated)
				Map.EM.AddComponent(_healhBar, typeof(FrozenRenderSceneTag));
		}
	}

	public void MoveTo(float3 pos)
	{
		if (!Map.EM.HasComponent<Destination>(Entity))
		{
			Map.EM.AddComponent(Entity, typeof(Destination));
		}
		if(Map.EM.HasComponent<Path>(Entity))
		{ 
			Map.EM.RemoveComponent<PathProgress>(Entity);	
			Map.EM.RemoveComponent<Path>(Entity);
		}
		Map.EM.SetComponentData(Entity, new Destination { Value = pos });
	}

	public virtual void Die()
	{
		Map.ActiveMap.units.Remove(id);
		Destroy();
		//TODO: Death Effect
	}

	public override int GetHashCode()
	{
		return id;
	}

	public void Destroy()
	{
		try
		{
			Map.EM.DestroyEntity(Entity);
			if (info.head != null)
				Map.EM.DestroyEntity(HeadEntity);
			if (_healhBar.IsCreated)
				Map.EM.DestroyEntity(_healhBar);
		}catch(Exception e)
		{
			Debug.LogWarning(e);
		}
	}

	public void SetState(UnitState.State state)
	{
		if (Map.EM.HasComponent<UnitState>(Entity))
			Map.EM.SetComponentData(Entity, new UnitState { Value = state });
		else
			Map.EM.AddComponentData(Entity, new UnitState { Value = state });
	}

	public void Attack(Entity target)
	{
		if (Map.EM.HasComponent<AttackTarget>(Entity))
			Map.EM.SetComponentData(Entity, new AttackTarget { Value = target });
		else
			Map.EM.AddComponentData(Entity, new AttackTarget { Value = target });
	}

	public void GoundFire(HexCoords pos)
	{
		throw new NotImplementedException();
	}

	public void Guard(Entity target)
	{
		throw new NotImplementedException();
	}

	public void Repair()
	{
		var cost = GetRepairCost();
		ResourceSystem.ConsumeResourses(cost);
		Map.EM.SetComponentData(Entity, new Health
		{
			Value = info.maxHealth
		});

	}

	public ResourceIndentifier[] GetRepairCost()
	{
		throw new NotImplementedException();
	}
}