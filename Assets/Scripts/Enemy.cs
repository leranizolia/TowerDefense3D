using UnityEngine;

public class Enemy : GameBehavior
{
    private EnemyFactory originFactory;

    public EnemyFactory OriginFactory
    {
        get => originFactory;
        set{
            originFactory = value;
        }
    }

    private GameTile tileFrom, tileTo;

    private Vector3 positionFrom, positionTo;

    private float progress, progressFactor;

    private Direction direction;

    private DirectionChange directionChange;

    private float directionAngleFrom, directionAngleTo;

    [SerializeField]
    Transform model = default;

    [SerializeField]
    EnemyAnimationConfig animationConfig = default;

    EnemyAnimator animator;

    private float pathOffSet;

    private float speed;
    public float Scale { get; private set; }

    float Health { get; set; }

    Collider targetPointCollider;

    public Collider TargetPointCollider
    {
        set
        {
            targetPointCollider = value;
        }
    }

    public bool IsValidTarget => animator.CurrentClip == EnemyAnimator.Clip.Move;


    public void SpawnOn(GameTile tile)
    {
        tileFrom = tile;
        tileTo = tile.NextTileOnPath;
        progress = 0f;
        PrepareIntro();
    }

    private void PrepareIntro()
    {
        positionFrom = tileFrom.transform.localPosition;
        transform.localPosition = positionFrom;
        positionTo = tileFrom.ExitPoint;
        direction = tileFrom.PathDirection;
        directionChange = DirectionChange.None;
        directionAngleFrom = directionAngleTo = direction.GetAngle();
        model.localPosition = new Vector3(pathOffSet, 0f);
        transform.localRotation = direction.GetRotation();
        progressFactor = 2f * speed;
    }

    public override bool GameUpdate()
    {
#if UNITY_EDITOR
		if (!animator.IsValid) {
			animator.RestoreAfterHotReload(
				model.GetChild(0).GetComponent<Animator>(),
				animationConfig,
				animationConfig.MoveAnimationSpeed * speed / Scale
			);
		}
#endif
        animator.GameUpdate();

        if (animator.CurrentClip == EnemyAnimator.Clip.Intro)
        {
            if (!animator.IsDone)
            {
                return true;
            }
            animator.PlayMove(animationConfig.MoveAnimationSpeed * speed / Scale);
            targetPointCollider.enabled = true;
        }
        else if (animator.CurrentClip >= EnemyAnimator.Clip.Outro)
        {
            if (animator.IsDone)
            {
                Recycle();
                return false;
            }
            return true;
        }

        if (Health <= 0f)
        {
            animator.PlayDying();
            targetPointCollider.enabled = false;
            return true;
        }

        progress += Time.deltaTime * progressFactor;
        while (progress >= 1f)
        {
            if (tileTo == null)
            {
                Game.EnemyReachedDestination();
                animator.PlayOutro();
                targetPointCollider.enabled = false;
                return true;
            }
            progress = (progress - 1f) / progressFactor;
            PrepareNextState();
            progress *= progressFactor;
        }
        if (directionChange == DirectionChange.None)
        {
            transform.localPosition = Vector3.LerpUnclamped(positionFrom, positionTo, progress);
        }
        else
        {
            float angle = Mathf.LerpUnclamped(
                directionAngleFrom, directionAngleTo, progress);
            transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
        return true;
    }

    private void PrepareNextState()
    {
        tileFrom = tileTo;
        tileTo = tileTo.NextTileOnPath;
        positionFrom = positionTo;
        if (tileTo == null)
        {
            PrepareOutro();
            return;
        }
        positionTo = tileFrom.ExitPoint;
        directionChange = direction.GetDirectionChangeTo(tileFrom.PathDirection);
        direction = tileFrom.PathDirection;
        directionAngleFrom = directionAngleTo;
        switch (directionChange)
        {
            case DirectionChange.None: PrepareForward(); break;
            case DirectionChange.TurnRight: PrepareTurnRight(); break;
            case DirectionChange.TurnLeft: PrepareTurnLeft(); break;
            default: PrepareTurnAround(); break;
        }
    }

    private void PrepareForward()
    {
        transform.localRotation = direction.GetRotation();
        directionAngleTo = direction.GetAngle();
        model.localPosition = new Vector3(pathOffSet, 0f);
        progressFactor = speed;
    }

    private void PrepareTurnRight()
    {
        directionAngleTo = directionAngleFrom + 90f;
        model.localPosition = new Vector3(pathOffSet - 0.5f, 0f);
        transform.localPosition = positionFrom + direction.GetHalfVector();
        progressFactor = speed / (Mathf.PI * 0.5f * (0.5f - pathOffSet));
    }

    private void PrepareTurnLeft()
    {
        directionAngleTo = directionAngleFrom - 90f;
        model.localPosition = new Vector3(pathOffSet + 0.5f, 0f);
        transform.localPosition = positionFrom + direction.GetHalfVector();
        progressFactor = speed / (Mathf.PI * 0.5f * (0.5f + pathOffSet));
    }

    private void PrepareTurnAround()
    {
        directionAngleTo = directionAngleFrom + (pathOffSet < 0f ? 180f: -180f);
        model.localPosition = new Vector3(pathOffSet, 0f);
        transform.localPosition = positionFrom;
        progressFactor = speed / (Mathf.PI * Mathf.Max(Mathf.Abs(pathOffSet), 0.2f));
    }

    private void PrepareOutro()
    {
        positionTo = tileFrom.transform.localPosition;
        directionChange = DirectionChange.None;
        directionAngleTo = direction.GetAngle();
        model.localPosition = new Vector3(pathOffSet, 0f);
        transform.localRotation = direction.GetRotation();
        progressFactor = 2f * speed;
    }

    public void Initialize(float scale, float speed, float pathOffset, float health)
    {
        Health = health;
        Scale = scale;
        model.localScale = new Vector3(scale, scale, scale);
        this.speed = speed;
        this.pathOffSet = pathOffset;
        animator.PlayIntro();
        targetPointCollider.enabled = false;
    }

    public void ApplyDamage(float damage)
    {
        Health -= damage;
    }

    public override void Recycle()
    {
        animator.Stop();
        OriginFactory.Reclaim(this);
    }

    void Awake()
    {
        animator.Configure(
            model.GetChild(0).gameObject.AddComponent<Animator>(),
            animationConfig
        );
    }

    void OnDestroy()
    {
        animator.Destroy();
    }
}
