﻿using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MapRenderer : MonoBehaviour
{
	public TileEntity tile;
	public MapGenerator generator;
	public GameObject oceanPlane;
	public MeshEntityRotatable line;
	public TileDatabase tileDatabase;
	public UnityEngine.UI.Image img;
	public int mapRes = 4;

	[HideInInspector]
	public Map map;

	[HideInInspector]
	public Vector3 min, max;
	public SerializedMap serializedMap;

	private Transform _ocean;
	private Camera _cam;
	private Vector3 _lastCamPos;
	private Quaternion _lastCamRot;
	private Plane[] _camPlanes;
	private EntityManager _entityManager;
	[HideInInspector]
	private Map map2;

	private void Start()
	{
		_cam = GameRegistry.Camera;
		Init();
		_lastCamPos = _cam.transform.position;
		_camPlanes = GeometryUtility.CalculateFrustumPlanes(_cam);
		min = Vector3.zero;
		max = new Vector3(map.totalWidth * map.shortDiagonal, 0, map.totalHeight * 1.5f);
		_cam.transform.position = new Vector3(max.x / 2, 50, max.z / 2);
		GameEvents.OnMapRegen += Regenerate;
		GameEvents.InvokeOnGameLoaded();
	}

	private void OnDestroy()
	{
		map?.Dispose();
	}

	public void Init()
	{
		_entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		map = generator.GenerateMap(transform);
		Map.ActiveMap = map;
		generator.GenerateFeatures(map);
		serializedMap = map.Serialize();
		//map2 = serializedMap.Deserialize(tileDatabase);
		//map.Render(_entityManager);
		//map = Map.ActiveMap = map2;
		map.Render(_entityManager);
		/*
		var col = Cartographer.RenderMap(map, mapRes);
		var tex = new Texture2D(map.totalWidth * mapRes, map.totalHeight * mapRes, TextureFormat.RGBA32, false);
		tex.SetPixels(col);
		tex.Apply();
		(var sprite = Sprite.Create(tex, new Rect(Vector2.zero, new Vector2(tex.width, tex.height)), Vector2.zero);
		img.sprite = sprite;
		*/
		var pos = oceanPlane.transform.localScale;
		pos *= 2;
		pos.y = map.seaLevel;
		_ocean = Instantiate(oceanPlane, pos, Quaternion.identity).GetComponent<Transform>();
		UnityEngine.Debug.Log("Map Load Invoke");
		GameEvents.InvokeOnMapLoaded();
		GameEvents.InvokeOnGameReady();
	}

	private void LateUpdate()
	{
		var camPos = _cam.transform.position;
		var camRot = _cam.transform.rotation;
		if (_lastCamPos != camPos || _lastCamRot != camRot)
		{
			GeometryUtility.CalculateFrustumPlanes(_cam, _camPlanes);
			map.UpdateView(_camPlanes);
			_lastCamPos = _cam.transform.position;
			_lastCamRot = _cam.transform.rotation;
			_ocean.position = new Vector3(_lastCamPos.x, _ocean.position.y, _lastCamPos.z);
		}

		if (generator.Regen)
		{
			GameEvents.InvokeOnMapRegen();
			generator.Regen = false;
		}
	}

	public void Regenerate()
	{
		map.Destroy();
		Destroy(_ocean.gameObject);
		Init();
		_lastCamPos = Vector3.zero;
	}
}