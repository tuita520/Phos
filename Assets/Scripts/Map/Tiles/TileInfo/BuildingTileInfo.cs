﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[CreateAssetMenu(menuName = "Map Asset/Tile/Building")]
public class BuildingTileInfo : TileInfo
{
	public MeshEntityRotatable buildingMesh;
	public int size = 0;
	public int powerTransferRadius = 0;
	public int influenceRange = 0;
	[SerializeField]
	public Resource[] cost;
	public Sprite icon;

	public Resource[] production;
	public Resource[] consumption;

	[System.Serializable]
	public struct Resource
	{
		public string name;
		public int ammount;
	}

	public override ComponentType[] GetComponents()
	{
		return base.GetComponents().Concat(new ComponentType[]{
			typeof(FirstTickTag)
		}).ToArray();
	}

	public override Entity Instantiate(HexCoords pos, Vector3 scale)
	{
		var e = base.Instantiate(pos, scale);
		if (production.Length > 0)
		{
			var pData = new ProductionData
			{
				resourceIds = new int[production.Length],
				rates = new int[production.Length]
			};
			for (int i = 0; i < production.Length; i++)
			{
				var rId = ResourceDatabase.GetResourceId(production[i].name);
				var tileName = ResourceDatabase.GetResourceTile(rId)?.name ?? production[i].name;
				pData.resourceIds[i] = rId;
				pData.rates[i] = production[i].ammount;
			}

			Map.EM.AddSharedComponentData(e, pData);
		}
		if (consumption.Length > 0)
		{

			var cData = new ConsumptionData
			{
				resourceIds = new int[consumption.Length],
				rates = new int[consumption.Length]
			};
			for (int i = 0; i < consumption.Length; i++)
			{
				var rId = ResourceDatabase.GetResourceId(consumption[i].name);
				var tileName = ResourceDatabase.GetResourceTile(rId)?.name ?? consumption[i].name;
				cData.resourceIds[i] = rId;
				cData.rates[i] = consumption[i].ammount;
			}

			Map.EM.AddSharedComponentData(e, cData);
		}
		return e;
	}

	public override Tile CreateTile(HexCoords pos, float height)
	{
		if(consumption.Length != 0 || production.Any(p => p.name == "Power"))
			return new PoweredBuildingTile(pos, height, this);
		else
			return new BuildingTile(pos, height, this);
	}

	public string GetProductionString()
	{
		var costString = "";
		for (int i = 0; i < production.Length; i++)
		{
			costString += $"<size=.75em><voffset=.25em>+</voffset></size><sprite={ResourceDatabase.GetResourceId(production[i].name)}> <size=.75em><voffset=.25em>{production[i].ammount}/t</voffset></size>";
			if (i != production.Length - 1)
				costString += "\n";
		}
		return costString;
	}

	public string GetCostString()
	{
		var costString = "";
		for (int i = 0; i < cost.Length; i++)
		{
			var id = ResourceDatabase.GetResourceId(cost[i].name);
			var curCost = $"<size=.75em><voffset=.25em>-</voffset></size><sprite={id}> <size=.75em><voffset=.25em>{cost[i].ammount}</voffset></size>";
			if (ResourceSystem.resCount[id] < cost[i].ammount)
				curCost = $"<color=#ff0000>{curCost}</color>";
			costString += curCost;
			if (i != cost.Length - 1)
				costString += "\n";
		}
		if (consumption.Length > 0)
			costString += "\n";
		for (int i = 0; i < consumption.Length; i++)
		{
			costString += $"<size=.75em><voffset=.25em>-</voffset></size><sprite={ResourceDatabase.GetResourceId(consumption[i].name)}> <size=.75em><voffset=.25em>{consumption[i].ammount}/t</voffset></size>";
			if (i != production.Length - 1)
				costString += "\n";
		}
		return costString;
	}
}