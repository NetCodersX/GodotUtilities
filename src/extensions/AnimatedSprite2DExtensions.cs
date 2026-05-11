using Godot;

namespace Utilities;

public static class AnimatedSprite2DExtensions
{
    public static bool PlayIfExist(this AnimatedSprite2D animatedSprite, StringName animName)
    {
        if (!animatedSprite.SpriteFrames.HasAnimation(animName))
            return false;

        animatedSprite.Play(animName);
        return true;
    }

    public static void PlayFrames(this AnimatedSprite2D animatedSprite, StringName animName, SpriteFrames frames)
    {
        if (animatedSprite.SpriteFrames != frames)
            animatedSprite.SpriteFrames = frames;

        animatedSprite.Play(animName);
    }

    public static SignalAwaiter WaitToFinish(this AnimatedSprite2D animatedSprite)
    {
        return animatedSprite.ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
    }
}

