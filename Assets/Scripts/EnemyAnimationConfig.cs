using UnityEngine;

[CreateAssetMenu]
public class EnemyAnimationConfig : ScriptableObject
{

	[SerializeField]
	AnimationClip move = default, intro = default, outro = default;

	public AnimationClip Move => move;

	public AnimationClip Intro => intro;

	public AnimationClip Outro => outro;
}