﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public class UISelectionPanel : UIPanel
{
	public UISelectedEntity selectionIconPrefab;
	public RectTransform iconGrid;
	public Transform selectionBox;
	[HideInInspector]
	public UIActionsPanel actionsPanel;

	private Dictionary<int, int> _selectionGroups;
	private List<List<ICommandable>> _selectionItems;
	private List<ScriptableObject> _selectionGroupInfo;

	private BuildPhysicsWorld _buildPhysicsWorld;
	private NativeList<int> _castHits;
	private Tile _start;
	private Tile _end;
	private List<ICommandable> _selection;
	private List<UISelectedEntity> _activeIcons;



	protected override void Awake()
	{
		base.Awake();
		_castHits = new NativeList<int>(Allocator.Persistent);
		_selection = new List<ICommandable>();
		selectionBox.gameObject.SetActive(false);
		_selectionGroups = new Dictionary<int, int>();
		_selectionItems = new List<List<ICommandable>>();
		_selectionGroupInfo = new List<ScriptableObject>();
		_buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>();
		_activeIcons = new List<UISelectedEntity>();
	}

	protected override void Start()
	{
		base.Start();
	}

	public void Show(List<ICommandable> units)
	{
		_selectionGroups.Clear();
		_selectionItems.Clear();
		_selectionGroupInfo.Clear();
		for (int i = 0; i < units.Count; i++)
		{
			var info = units[i].GetInfo();
			if (_selectionGroups.ContainsKey(info.GetInstanceID()))
				_selectionItems[_selectionGroups[info.GetInstanceID()]].Add(units[i]);
			else
			{
				_selectionGroups.Add(info.GetInstanceID(), _selectionItems.Count);
				_selectionItems.Add(new List<ICommandable> { units[i] });
				_selectionGroupInfo.Add(info);
			}
		}
		if (units[0] is MobileUnit)
			RenderUnits();
		else
			RenderTiles();
		Show();
		actionsPanel.Show();
		Debug.Log("selection open");
	}

	public override void Hide()
	{
		base.Hide();
		_start = _end = null;
		_selection.Clear();
		_selectionGroups.Clear();
		_selectionItems.Clear();
		_selectionGroupInfo.Clear();
	}

	private void RenderUnits()
	{
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			var unitInfo = (MobileUnitEntity)_selectionGroupInfo[i];
			if(_activeIcons.Count <= i)
				_activeIcons.Add(Instantiate(selectionIconPrefab, iconGrid, false));
			_activeIcons[i].nameText.SetText(unitInfo.name);
			_activeIcons[i].icon.sprite = default; //TODO: Add Icon
			_activeIcons[i].countText.SetText(_selectionItems[i].Count.ToString());
		}
	}

	private void RenderTiles()
	{
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			var tileInfo = (BuildingTileEntity)_selectionGroupInfo[i];
			if (_activeIcons.Count <= i)
				_activeIcons.Add(Instantiate(selectionIconPrefab, iconGrid, false));
			_activeIcons[i].nameText.SetText(tileInfo.name);
			_activeIcons[i].icon.sprite = tileInfo.icon; 
			_activeIcons[i].countText.SetText(_selectionItems[i].Count.ToString());
		}
	}

	public void UpdateState()
	{
		var mousePos = Input.mousePosition;
		var cam = GameRegistry.Camera;
		var ray = cam.ScreenPointToRay(mousePos);
		var hasTile = _buildPhysicsWorld.GetTileFromRay(ray, cam.transform.position.y * 2, out var pos);
		if (!hasTile)
			return;
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			Hide();
			return;
		}
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			_start = Map.ActiveMap[pos];
		}
		if (Input.GetKey(KeyCode.Mouse0))
		{
			_end = Map.ActiveMap[pos];
			if (_start != _end && _start != null && _end != null)
				DisplaySelectionRect();
		}
		if (Input.GetKeyUp(KeyCode.Mouse0))
		{
			selectionBox.gameObject.SetActive(false);
			_end = Map.ActiveMap[pos];
			if (_start != null && _end != null)
			{
				_castHits.Clear();
				var bounds = MathUtils.PhysicsBounds(_start.SurfacePoint, _end.SurfacePoint);
				bounds.Max.y = 50;
				bounds.Min.y = 0;
				DebugUtilz.DrawBounds(bounds, Color.cyan);
				_buildPhysicsWorld.AABBCast(bounds, new CollisionFilter
				{
					BelongsTo = 1u << (int)Faction.Player,
					CollidesWith = 1u << (int)Faction.Unit
				}, ref _castHits);
				_selection.Clear();
				actionsPanel.ResetSelection();
				if (_castHits.Length > 0)
				{
					for (int i = 0; i < _castHits.Length; i++)
					{
						var entity = _buildPhysicsWorld.PhysicsWorld.Bodies[_castHits[i]].Entity;
						if (Map.EM.HasComponent<UnitId>(entity))
						{
							var e = Map.ActiveMap.units[Map.EM.GetComponentData<UnitId>(entity).Value];
							_selection.Add(e);
							actionsPanel.AddSelectedEntity(e);
						}
					}
				}
				if (_selection.Count > 0)
					Show(_selection);
				else
					Hide();
			}
		}
	}

	private void DisplaySelectionRect()
	{
		//Drag Select
		var p0 = _start.Coords.world;
		var p1 = HexCoords.OffsetToWorldPosXZ(_end.Coords.offsetCoords.x, _start.Coords.offsetCoords.y, Map.ActiveMap.innerRadius, Map.ActiveMap.tileEdgeLength);
		var p2 = HexCoords.OffsetToWorldPosXZ(_start.Coords.offsetCoords.x, _end.Coords.offsetCoords.y, Map.ActiveMap.innerRadius, Map.ActiveMap.tileEdgeLength);
		var p3 = _end.Coords.world;
#if DEBUG
		p0.y = p1.y = p2.y = p3.y = (_start.Height + _end.Height) / 2f;
		Debug.DrawLine(p0, p1, Color.white);
		Debug.DrawLine(p1, p3, Color.white);
		Debug.DrawLine(p0, p2, Color.white);
		Debug.DrawLine(p2, p3, Color.white);
#endif
		var w = p0.x - p1.x;
		var h = p0.z - p2.z;
		selectionBox.position = new Vector3(p0.x - (w / 2), 0, p0.z - (h / 2));
		selectionBox.localScale = new Vector3(w, 80, h);
		selectionBox.gameObject.SetActive(true);
	}
}
