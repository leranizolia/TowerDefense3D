using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    private Vector2Int _boardSize;

    [SerializeField]
    private GameBoard _board;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private GameTileContentFactory _contentFactory;

    [SerializeField]
    WarFactory warFactory = default;

    [SerializeField]
    GameScenario scenario = default;

    [SerializeField, Range(0, 100)]
    int startingPlayerHealth = 10;

    [SerializeField, Range(1f, 10f)]
    float playSpeed = 1f;

    const float pausedTimeScale = 0f;

    int playerHealth;

    GameScenario.State activeScenario;

    private GameBehaviorCollection enemies = new GameBehaviorCollection();
    private GameBehaviorCollection nonEnemies = new GameBehaviorCollection();

    private TowerType selectedTowerType;

    private static Game instance;

    private Ray TouchRay => _camera.ScreenPointToRay(Input.mousePosition);

    private void Start()
    {
        playerHealth = startingPlayerHealth;
        _board.Intialize(_boardSize, _contentFactory);
        activeScenario = scenario.Begin();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedTowerType = TowerType.Laser;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedTowerType = TowerType.Mortar;
        }
        if (Input.GetMouseButtonDown(0))
        {
            HandleTouch();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            HandleAlternativeTouch();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale =
                Time.timeScale > pausedTimeScale ? pausedTimeScale : playSpeed;
        }
        else if (Time.timeScale > pausedTimeScale)
        {
            Time.timeScale = playSpeed;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            BeginNewGame();
        }

        if (playerHealth <= 0 && startingPlayerHealth > 0)
        {
            BeginNewGame();
        }

        if (!activeScenario.Progress() && enemies.IsEmpty)
        {
            BeginNewGame();
            activeScenario.Progress();
        }

        enemies.GameUpdate();
        Physics.SyncTransforms();
        _board.GameUpdate();
        nonEnemies.GameUpdate();
    }

    public static void SpawnEnemy(EnemyFactory factory, EnemyType type)
    {
        GameTile spawnPoint = instance._board.GetSpawnPoint(
            Random.Range(0, instance._board.SpawnPointCount)
        );
        Enemy enemy = factory.Get(type);
        enemy.SpawnOn(spawnPoint);
        instance.enemies.Add(enemy);
    }

    private void HandleAlternativeTouch()
    {
        GameTile tile = _board.GetTile(TouchRay);
        if (tile != null)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                _board.ToggleDestination(tile);
            }
            else
            {
                _board.ToggleSpawnPoint(tile);
            }
        }
    }
    private void HandleTouch()
    {
        GameTile tile = _board.GetTile(TouchRay);
        if (tile != null)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                _board.ToggleTower(tile, selectedTowerType);
            }
            else
            {
                _board.ToggleWall(tile);
            }
        }
    }

    public static Shell SpawnShell()
    {
        Shell shell = instance.warFactory.Shell;
        instance.nonEnemies.Add(shell);
        return shell;
    }

    void OnEnable()
    {
        instance = this;
    }

    public static Explosion SpawnExplosion()
    {
        Explosion explosion = instance.warFactory.Explosion;
        instance.nonEnemies.Add(explosion);
        return explosion;
    }

    void BeginNewGame()
    {
        playerHealth = startingPlayerHealth;
        enemies.Clear();
        nonEnemies.Clear();
        _board.Clear();
        activeScenario = scenario.Begin();
    }

    public static void EnemyReachedDestination()
    {
        instance.playerHealth -= 1;
    }
}
