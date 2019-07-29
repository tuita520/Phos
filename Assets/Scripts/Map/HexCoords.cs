using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct HexCoords
{
	//Position
	[SerializeField]
	public readonly int x;
	[SerializeField]
	public readonly int y;
	[SerializeField]
	public readonly int z;
	//Hex info
	[HideInInspector]
	public readonly float edgeLength;
	//World Pos
	public readonly float worldX;
	public readonly float worldZ;
	public readonly Vector3 worldXZ;
	public readonly Vector2 worldXY;
	//Offsets
	public readonly int offsetX;
	public readonly int offsetZ;
	public readonly bool isCreated;

	public HexCoords(int x, int y, float edgeLength, float? innerRadius = null)
	{
		this.x = x;
		this.y = y;
		this.z = -x - y;
		this.edgeLength = edgeLength;
		var innerR = (innerRadius ?? Mathf.Sqrt(3f) / 2f * this.edgeLength);
		offsetX = x + y / 2;
		offsetZ = y;
		//worldX = (offsetX + offsetZ * .5f - offsetZ / 2) * (innerRadius * 2f);
		//worldZ = offsetZ * (this.edgeLength * 1.5f);
		(worldX, worldZ) = OffsetToWorldPos(offsetX, offsetZ, innerR, edgeLength);

		worldXZ = new Vector3(worldX, 0, worldZ);
		worldXY = new Vector2(worldX, worldZ);
		isCreated = true;
	}

	public static Vector3 SnapToGrid(Vector3 worldPos, float innerRadius, float edgeLength)
	{
		float x = worldPos.x / (innerRadius * 2f);
		float z = -x;
		float offset = worldPos.z / (edgeLength * 3f);
		z -= offset;
		x -= offset;
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(-x - z);
		var offsetX = iX + iY / 2;
		var offsetZ = iY;
		return OffsetToWorldPosXZ(offsetX, offsetZ, innerRadius, edgeLength);
	}

	public static float CalculateInnerRadius(float edgeLength) => Mathf.Sqrt(3f) / 2f * edgeLength;

	public static float CalculateShortDiagonal(float edgeLength) => Mathf.Sqrt(3f) * edgeLength;

	public static HexCoords FromOffsetCoords(int x, int Z, float edgeLength = 1) => new HexCoords(x - (Z / 2), Z, edgeLength);

	public static HexCoords FromPosition(Vector3 position, float edgeLength = 1)
	{
		float innerRadius = CalculateInnerRadius(edgeLength);
		float x = position.x / (innerRadius * 2f);
		float z = -x;
		float offset = position.z / (edgeLength * 3f);
		z -= offset;
		x -= offset;
		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(-x -z);
		return new HexCoords(iX, iY, edgeLength, innerRadius);
	}

	public HexCoords ToChunkLocalCoord()
	{
		var (x, z) = GetChunkPos();
		return ToChunkLocalCoord(x, z);
	}

	public HexCoords TranslateOffset(int x, int z) => FromOffsetCoords(offsetX + x, offsetZ + z, edgeLength);

	public HexCoords ToChunkLocalCoord(int chunkX, int chunkZ) => FromOffsetCoords(offsetX - (chunkX * Map.Chunk.SIZE), offsetZ - (chunkZ * Map.Chunk.SIZE), edgeLength);

	public (int chunkX, int chunkZ) GetChunkPos() => (Mathf.FloorToInt((float)offsetX / Map.Chunk.SIZE), Mathf.FloorToInt((float)offsetZ / Map.Chunk.SIZE));

	public int GetChunkIndex(int width)
	{
		var (cx, cz) = GetChunkPos();
		return cx + cz * width;
	}

	public static int GetChunkIndex(int chunkX, int chunkZ, int width) => chunkX + chunkZ * width;

	public int ToIndex(int mapWidth) => x + y * mapWidth + y / 2;

	public float DistanceToSq(HexCoords b) => (worldXZ - b.worldXZ).sqrMagnitude;

	public static float DistanceSq(HexCoords a, HexCoords b) => (a.worldXZ - b.worldXZ).sqrMagnitude;
	
	public static (float X, float Z) OffsetToWorldPos(int x, int z, float innerRadius, float edgeLength)
	{
		var worldX = (x + z * .5f - z / 2) * (innerRadius * 2f);
		var worldZ = z * (edgeLength * 1.5f);
		return (worldX, worldZ);
	}

	public static Vector3 OffsetToWorldPosXZ(int x, int z, float innerRadius, float edgeLength)
	{
		var (wX, wZ) = OffsetToWorldPos(x, z, innerRadius, edgeLength);
		return new Vector3(wX, 0, wZ);
	}

	public static float TileToWorldDist(int tileCount, float innerRadius) => tileCount * (2 * innerRadius);

	public static int WorldToTileDist(float dist, float innerRadius) => Mathf.RoundToInt(dist / (2 * innerRadius));

	public override string ToString() => $"({x}, {y}, {z})";

	public static int GetTileCount(int r) => (1 + 3 * (r + 1) * (r));

	public static int CalculateRadius(int tileCount)
	{
		var sqrt = math.sqrt(3 * ((4 * tileCount) - 1));
		var a = -(3 + sqrt) / 6f;
		var b = -(3 - sqrt) / 6f;
		if (a < 0)
			return Mathf.CeilToInt(b);
		else
			return Mathf.CeilToInt(a);
	}

	public static bool operator ==(HexCoords a, HexCoords b) => a.Equals(b);

	public static bool operator !=(HexCoords a, HexCoords b) => !a.Equals(b);

	public static HexCoords[] HexSelect(HexCoords center, int radius, bool excludeCenter = false)
	{
		radius = Mathf.Abs(radius);
		var i = 0;
		if (radius == 0)
		{
			return new HexCoords[] { center };
		}
		var selection = new HexCoords[GetTileCount(radius)];
		for (int y = -radius; y <= radius; y++)
		{
			int xMin = -radius, xMax = radius;
			if (y < 0)
				xMin = -radius - y;
			if (y > 0)
				xMax = radius - y;
			for (int x = xMin; x <= xMax; x++)
			{
				int z = -x - y;
				var coord = new HexCoords(center.x + x, center.y + y, center.edgeLength);
				if (excludeCenter && coord == center)
					continue;
				selection[i++] = coord;
			}
		}
		return selection;
	}

	public static HexCoords[] GetNeighbors(HexCoords center, float? innerRadius = null)
	{
		HexCoords[] neighbors = new HexCoords[6];
		neighbors[0] = new HexCoords(center.x - 1, center.y    , center.edgeLength, innerRadius);
		neighbors[1] = new HexCoords(center.x - 1, center.y + 1, center.edgeLength, innerRadius);
		neighbors[2] = new HexCoords(center.x    , center.y + 1, center.edgeLength, innerRadius);
		neighbors[3] = new HexCoords(center.x + 1, center.y    , center.edgeLength, innerRadius);
		neighbors[4] = new HexCoords(center.x + 1, center.y - 1, center.edgeLength, innerRadius);
		neighbors[5] = new HexCoords(center.x    , center.y - 1, center.edgeLength, innerRadius);
		return neighbors;
	}

	public bool IsInBounds(int height, int widht)
	{
		if (0 > offsetZ || height <= offsetZ)
			return false;
		if (0 > offsetX || widht <= offsetX)
			return false;
		return true;
	}

	// override object.Equals
	public override bool Equals(object obj)
	{
		if (!isCreated)
			return false;
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}

		var h = (HexCoords)obj;
		if (!h.isCreated)
			return false;
		return (h.x == x && h.y == y);
	}

	// override object.GetHashCode
	const int prime = 31;
	public override int GetHashCode()
	{
		int hash = 23;
		hash = hash * prime + offsetX;
		hash = hash * prime + offsetZ;
		return hash;
	}
}
