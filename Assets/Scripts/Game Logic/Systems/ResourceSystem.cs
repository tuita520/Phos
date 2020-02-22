﻿using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ResourceSystem : ComponentSystem
{
	public static int[] resCount;
	public static int maxStorage;

	public DateTime nextTic;
	public float ticRate = 1;

	public ResourceTransactionRecord[] resourceRecords;
	private bool _canRun;

	public struct ResourceTransactionRecord
	{
		public int resId;
		public Dictionary<int, Transaction> transactions;

		public int totalDemand;
		public int totalSatisfaction;
		public int totalProduction;
		public int totalExcess;

		public struct Transaction
		{
			public int buildingId;
			public int demand;
			public int demandCount;
			public int satisfaction;
			public int satisfactionCount;
			public int production;
			public int productionCount;
			public int excess;
			public int excessCount;

			public Transaction(int buildingId)
			{
				this.buildingId = buildingId;
				demand = satisfaction = production = excess = 0;
				demandCount = satisfactionCount = productionCount = excessCount = 0;
			}
		}

		public ResourceTransactionRecord(int id)
		{
			resId = id;
			transactions = new Dictionary<int, Transaction>();
			totalDemand = totalSatisfaction = totalProduction = totalExcess = 0;
		}

		public void Clear()
		{
			transactions = new Dictionary<int, Transaction>();
			totalDemand = totalSatisfaction = totalProduction = totalExcess = 0;
		}

		public void LogDemand(int count, int buildingId)
		{
			Transaction transaction;
			if (transactions.ContainsKey(buildingId))
				transaction = transactions[buildingId];
			else
				transactions.Add(buildingId, transaction = new Transaction(buildingId));
			transaction.demand += count;
			transaction.demandCount++;
			transactions[buildingId] = transaction;
			totalDemand += count;
		}

		public void LogSatisfaction(int count, int buildingId)
		{
			Transaction transaction;
			if (transactions.ContainsKey(buildingId))
				transaction = transactions[buildingId];
			else
				transactions.Add(buildingId, transaction = new Transaction(buildingId));
			transaction.satisfaction += count;
			transaction.satisfactionCount++;
			transactions[buildingId] = transaction;
			totalSatisfaction += count;
		}

		public void LogProduction(int count, int buildingId)
		{
			Transaction transaction;
			if (transactions.ContainsKey(buildingId))
				transaction = transactions[buildingId];
			else
				transactions.Add(buildingId, transaction = new Transaction(buildingId));
			transaction.production += count;
			transaction.productionCount++;
			transactions[buildingId] = transaction;
			totalProduction += count;
		}

		public void LogExcess(int count, int buildingId)
		{
			Transaction transaction;
			if (transactions.ContainsKey(buildingId))
				transaction = transactions[buildingId];
			else
				transactions.Add(buildingId, transaction = new Transaction(buildingId));
			transaction.excess += count;
			transaction.excessCount++;
			transactions[buildingId] = transaction;
			totalExcess += count;
		}
	}

	protected override void OnCreate()
	{
		base.OnCreate();
		if(GameRegistry.INST == null)
			_canRun = false;
		GameRegistry.INST.resourceSystem = this;
		resourceRecords = new ResourceTransactionRecord[ResourceDatabase.ResourceCount];
		for (int i = 0; i < resourceRecords.Length; i++)
			resourceRecords[i] = new ResourceTransactionRecord(i);
		resCount = new int[ResourceDatabase.ResourceCount];
		maxStorage = 1000;
	}

	protected override void OnStartRunning()
	{
		nextTic = DateTime.Now.AddSeconds(1 / ticRate);
	}

	protected override void OnUpdate()
	{
		if (!_canRun)
			return;
		if (DateTime.Now < nextTic)
			return;
		//Init Tick
		nextTic = nextTic.AddSeconds(1 / ticRate);
		for (int i = 0; i < resourceRecords.Length; i++)
			resourceRecords[i].Clear();
		EventManager.InvokeEvent("OnTick");


		//Consumption
		Entities.WithNone<BuildingOffTag, ConsumptionDebuff>().ForEach((Entity e, ConsumptionData c, ref BuildingId id) =>
		{
			if (HasAllResources(c.resourceIds, c.rates, demandSrc: id.Value))
			{
				ConsumeResourses(c.resourceIds, c.rates, demandSrc: id.Value);
				if (EntityManager.HasComponent<InactiveBuildingTag>(e))
					PostUpdateCommands.RemoveComponent<InactiveBuildingTag>(e);
			}else
			{
				if (!EntityManager.HasComponent<InactiveBuildingTag>(e))
					PostUpdateCommands.AddComponent(e, new InactiveBuildingTag());

			}

		});

		//Debuffed Consumption
		Entities.WithNone<BuildingOffTag>().ForEach((Entity e, ConsumptionData c, ref ConsumptionDebuff d, ref BuildingId id) =>
		{
			if (HasAllResources(c.resourceIds, c.rates, d.distance * ConsumptionDebuff.multi, id.Value))
			{
				ConsumeResourses(c.resourceIds, c.rates, d.distance * ConsumptionDebuff.multi, id.Value);
				if (EntityManager.HasComponent<InactiveBuildingTag>(e))
					PostUpdateCommands.RemoveComponent<InactiveBuildingTag>(e);
			}
			else
			{
				if (!EntityManager.HasComponent<InactiveBuildingTag>(e))
					PostUpdateCommands.AddComponent(e, new InactiveBuildingTag());
			}
		});

		//Production
		Entities.WithNone<InactiveBuildingTag, BuildingOffTag, FirstTickTag>().ForEach((Entity e, ProductionData p, ref BuildingId id) =>
		{
			for (int i = 0; i < p.resourceIds.Length; i++)
			{
				int res = p.resourceIds[i];
				if (resCount[res] == maxStorage)
				{
					LogExcess(res, p.rates[i], id.Value);
					continue;
				}
				LogProduction(res, p.rates[i], id.Value);
				resCount[res] += p.rates[i];
				if (resCount[res] > maxStorage)
					resCount[res] = maxStorage;
			}
		});

		//Prevent production of resources on first tick
		Entities.WithAll<FirstTickTag>().ForEach(e =>
		{
			if (EntityManager.Exists(e))
				PostUpdateCommands.RemoveComponent(e, typeof(FirstTickTag));
		});
	}

	public void LogDemand(int rId, int rate, int srcBuilding) => resourceRecords[rId].LogDemand(rate, srcBuilding);

	public void LogSatisfaction(int rId, int rate, int srcBuilding) => resourceRecords[rId].LogSatisfaction(rate, srcBuilding);

	public void LogProduction(int rId, int rate, int srcBuilding) => resourceRecords[rId].LogProduction(rate, srcBuilding);

	public void LogExcess(int rId, int rate, int srcBuilding) => resourceRecords[rId].LogExcess(rate, srcBuilding);


	public bool HasAllResources(int[] ids, int[] rates, float multi = 1, int demandSrc = -1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return true;
		bool hasRes = true;
		for (int i = 0; i < ids.Length; i++)
		{
			var totalRate = multi == 1 ? rates[i] : (int)(rates[i] * multi);
			if(demandSrc != -1)
				LogDemand(ids[i], totalRate, demandSrc);
			if (resCount[ids[i]] < totalRate)
				hasRes = false;
		}
		return hasRes;
	}

	public bool HasAllResources(ResourceIndentifier[] resources, float multi = 1, int demandSrc = -1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return true;
		bool hasRes = true;
		for (int i = 0; i < resources.Length; i++)
		{
			var totalRate = multi == 1 ? (int)resources[i].ammount : (int)(resources[i].ammount * multi);
			if (demandSrc != -1)
				LogDemand(resources[i].id, totalRate, demandSrc);
			if (resCount[resources[i].id] < totalRate)
				hasRes = false;
		}
		return hasRes;
	}

	void ConsumeResourses(int[] ids, int[] rates, float multi = 1, int demandSrc = -1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return;
		for (int i = 0; i < ids.Length; i++)
		{
			var totalRate = multi == 1 ? rates[i] : (int)(rates[i] * multi);
			resCount[ids[i]] -= totalRate;
			if (demandSrc != -1)
				LogSatisfaction(ids[i], totalRate, demandSrc);
		}
	}

	public static void ConsumeResourses(ResourceIndentifier[] resources, float multi = 1, int demandSrc = -1)
	{
		for (int i = 0; i < resources.Length; i++)
			ConsumeResource(resources[i], multi, demandSrc);
	}

	public static void ConsumeResource(ResourceIndentifier resource, float multi = 1, int demandSrc = -1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return;
		var ammount = Mathf.FloorToInt(resource.ammount * multi);
		resCount[resource.id] -= ammount;
		if (demandSrc != -1)
			GameRegistry.ResourceSystem.LogSatisfaction(resource.id, ammount, demandSrc);
	}

	public static void AddResources(ResourceIndentifier[] resources, float multi = 1)
	{
		for (int i = 0; i < resources.Length; i++)
			AddResource(resources[i]);
	}

	public static void AddResource(ResourceIndentifier resource, float multi = 1)
	{
		resCount[resource.id] += Mathf.FloorToInt(resource.ammount * multi);
	}

	/*public static bool HasResourses(ResourceIndentifier[] resources, float multi = 1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return true;
		for (int i = 0; i < resources.Length; i++)
		{
			var id = resources[i].id;
			if (resCount[id] < Mathf.FloorToInt(resources[i].ammount * multi))
				return false;
		}
		return true;
	}*/

	public static bool HasResource(ResourceIndentifier resource, float multi = 1)
	{
		if (GameRegistry.Cheats.NO_RESOURCE_COST)
			return true;
		return resCount[resource.id] >= Mathf.FloorToInt(resource.ammount * multi);
	}
}
