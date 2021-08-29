using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[System.Serializable]
public struct EnemyAnimator
{
	PlayableGraph graph;

	AnimationMixerPlayable mixer;

	Clip previousClip;

	float transitionProgress;

	bool hasAppearClip, hasDisappearClip;

	const float transitionSpeed = 5f;

	public bool IsDone => GetPlayable(CurrentClip).IsDone();

	public Clip CurrentClip { get; private set; }

	public void Configure(Animator animator, EnemyAnimationConfig config)
	{
		hasAppearClip = config.Appear;
		hasDisappearClip = config.Disappear;

		graph = PlayableGraph.Create();
		graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
		mixer = AnimationMixerPlayable.Create(
			graph, hasAppearClip || hasDisappearClip ? 6 : 4);

		var clip = AnimationClipPlayable.Create(graph, config.Move);
		clip.Pause();
		mixer.ConnectInput((int)Clip.Move, clip, 0);

		clip = AnimationClipPlayable.Create(graph, config.Intro);
		clip.SetDuration(config.Intro.length);
		mixer.ConnectInput((int)Clip.Intro, clip, 0);

		clip = AnimationClipPlayable.Create(graph, config.Outro);
		clip.SetDuration(config.Outro.length);
		clip.Pause();
		mixer.ConnectInput((int)Clip.Outro, clip, 0);

		clip = AnimationClipPlayable.Create(graph, config.Dying);
		clip.SetDuration(config.Dying.length);
		clip.Pause();
		mixer.ConnectInput((int)Clip.Dying, clip, 0);

		if (hasAppearClip)
		{
			clip = AnimationClipPlayable.Create(graph, config.Appear);
			clip.SetDuration(config.Appear.length);
			clip.Pause();
			mixer.ConnectInput((int)Clip.Appear, clip, 0);
		}

		if (hasDisappearClip)
		{
			clip = AnimationClipPlayable.Create(graph, config.Disappear);
			clip.SetDuration(config.Disappear.length);
			clip.Pause();
			mixer.ConnectInput((int)Clip.Disappear, clip, 0);
		}

		var output = AnimationPlayableOutput.Create(graph, "Enemy", animator);
		output.SetSourcePlayable(mixer);
	}

	public void PlayIntro()
	{
		SetWeight(Clip.Intro, 1f);
		CurrentClip = Clip.Intro;
		graph.Play();
		transitionProgress = -1f;
		if (hasAppearClip)
		{
			GetPlayable(Clip.Appear).Play();
			SetWeight(Clip.Appear, 1f);
		}
	}

	private void SetWeight(Clip clip, float weight)
	{
		mixer.SetInputWeight((int)clip, weight);
	}

	public void PlayMove(float speed)
	{
		GetPlayable(Clip.Move).SetSpeed(speed);
		BeginTransition(Clip.Move);
		if (hasAppearClip)
		{
			SetWeight(Clip.Appear, 0f);
		}
	}

	Playable GetPlayable(Clip clip)
	{
		return mixer.GetInput((int)clip);
	}

	public void PlayOutro()
	{
		BeginTransition(Clip.Outro);
		if (hasDisappearClip)
		{
			PlayDisappearFor(Clip.Outro);
		}
	}

	public void Stop()
	{
		graph.Stop();
	}

	public void Destroy()
	{
		graph.Destroy();
	}

	public enum Clip
	{ 
		Move,
		Intro,
		Outro,
		Dying,
		Appear,
		Disappear
	}

	void BeginTransition(Clip nextClip)
	{
		previousClip = CurrentClip;
		CurrentClip = nextClip;
		transitionProgress = 0f;
		GetPlayable(nextClip).Play();
	}

	public void GameUpdate()
	{
		if (transitionProgress >= 0f)
		{
			transitionProgress += Time.deltaTime * transitionSpeed;
			if (transitionProgress >= 1f)
			{
				transitionProgress = -1f;
				SetWeight(CurrentClip, 1f);
				SetWeight(previousClip, 0f);
			}
			else
			{
				SetWeight(CurrentClip, transitionProgress);
				SetWeight(previousClip, 1f - transitionProgress);
			}
		}
	}

	public void PlayDying()
	{
		BeginTransition(Clip.Dying);
		if (hasDisappearClip)
		{
			PlayDisappearFor(Clip.Dying);
		}
	}

	private void PlayDisappearFor(Clip otherClip)
	{
		var clip = GetPlayable(Clip.Disappear);
		clip.Play();
		clip.SetDelay(GetPlayable(otherClip).GetDuration() - clip.GetDuration());
		SetWeight(Clip.Disappear, 1f);
	}
}
