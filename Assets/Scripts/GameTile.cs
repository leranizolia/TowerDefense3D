using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTile : MonoBehaviour
{
    [SerializeField]
    private Transform _arrow;

    private GameTile _north, _east, _south, _west, _nextOnPath;

    private int _distance;

    private static Quaternion
        northRotation = Quaternion.Euler(90f, 0f, 0f),
        eastRotation = Quaternion.Euler(90f, 90f, 0f),
        southRotation = Quaternion.Euler(90f, 180f, 0f),
        westRotation = Quaternion.Euler(90f, 270f, 0f);

    public bool IsAlternative { get; set; }

    private GameTileContent _content;
    public GameTileContent Content
    {
        get => _content;
        set
        {
            if (_content != null)
            {
                _content.Recycle();
            }
            _content = value;
            _content.transform.localPosition = transform.localPosition;
        }
    }

    public GameTile NextTileOnPath => _nextOnPath;

    public Vector3 ExitPoint { get; private set; }

    public Direction PathDirection { get; private set; }

    public static void MakeEastWestNeighbors(GameTile east, GameTile west)
    {
        west._east = east;
        east._west = west;
    }

    public static void MakeSouthNorthNeighbors(GameTile north, GameTile south)
    {
        south._north = north;
        north._south = south;
    }

    public void ClearPath()
    {
        _distance = int.MaxValue;
        _nextOnPath = null;
    }

    public void BecomeDestination()
    {
        _distance = 0;
        _nextOnPath = null;
        ExitPoint = transform.localPosition;
    }

    public bool HasPath => _distance != int.MaxValue;

    private GameTile GrowPathTo(GameTile neighbor, Direction direction)
    {
        if (!HasPath || neighbor == null || neighbor.HasPath)
        {
            return null;
        }
        neighbor._distance = _distance + 1;
        neighbor._nextOnPath = this;
        neighbor.ExitPoint = neighbor.transform.localPosition + direction.GetHalfVector();
        neighbor.PathDirection = direction;
        return
            neighbor.Content.BlocksPath ? null : neighbor;
    }

    public GameTile GrowPathNorth() => GrowPathTo(_north, Direction.South);
    public GameTile GrowPathEast() => GrowPathTo(_east, Direction.West);
    public GameTile GrowPathSouth() => GrowPathTo(_south, Direction.North);
    public GameTile GrowPathWest() => GrowPathTo(_west, Direction.East);

    public void ShowPath()
    {
        if (_distance == 0)
        {
            _arrow.gameObject.SetActive(false);
            return;
        }
        _arrow.gameObject.SetActive(true);
        _arrow.localRotation =
            _nextOnPath == _north ? northRotation :
            _nextOnPath == _east ? eastRotation :
            _nextOnPath == _south ? southRotation :
            westRotation;
    }
}
