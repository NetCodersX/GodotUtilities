using System.Collections.Generic;
using Godot;

namespace Utilities.AudioManagement;

public sealed class CrossfadeChannel
{
    private readonly record struct PendingTrack(AudioStream Stream, float FadeDuration);

    private readonly AudioStreamPlayer playerA;
    private readonly AudioStreamPlayer playerB;
    private readonly Callable callable;

    private readonly Queue<PendingTrack> sequence = new();

    private bool usingA;
    private Tween tween;

    public CrossfadeChannel(Node parent, string bus)
    {
        playerA = MakePlayer(bus); parent.AddChild(playerA);
        playerB = MakePlayer(bus); parent.AddChild(playerB);

        callable = Callable.From(OnTrackFinished);
    }

    private static AudioStreamPlayer MakePlayer(string bus) => new() { Bus = bus };

    private void OnTweenFinished()
    {
        var player = usingA ? playerA : playerB;
        player.Stop();
    }

    public void Play(AudioStream stream, float fadeDuration)
    {
        var incoming = usingA ? playerA : playerB;
        var outgoing = usingA ? playerB : playerA;
        usingA = !usingA;

        incoming.Stream = stream;
        incoming.VolumeLinear = 0f;

        incoming.Play();
        incoming.ConnectOnce(AudioStreamPlayer.SignalName.Finished, callable);

        tween.KillIfValid();
        tween = playerA.CreateTween().SetParallel();

        tween.TweenVolumeLinear(incoming, 1f, fadeDuration);
        tween.TweenVolumeLinear(outgoing, 0f, fadeDuration);
        tween.OnFinished(OnTweenFinished);
    }

    public void ThenPlay(AudioStream stream, float fadeDuration)
    {
        if (!playerA.Playing && !playerB.Playing)
            Play(stream, fadeDuration);
        else
            sequence.Enqueue(new(stream, fadeDuration));
    }

    public void Stop(float fadeDuration)
    {
        tween.KillIfValid();
        sequence.Clear();

        FadeOut(playerA, fadeDuration);
        FadeOut(playerB, fadeDuration);
    }

    public void Pause()
    {
        playerA.StreamPaused = true;
        playerB.StreamPaused = true;
    }

    public void Resume()
    {
        playerA.StreamPaused = false;
        playerB.StreamPaused = false;
    }

    public void ClearSequence()
    {
        sequence.Clear();
    }

    private static void FadeOut(AudioStreamPlayer player, float fadeDuration)
    {
        if (!player.Playing) return;

        player.CreateTween()
            .TweenVolumeLinear(player, 0f, fadeDuration)
            .OnFinished(player.Stop);
    }

    private void OnTrackFinished()
    {
        if (sequence.Count == 0) return;
        var next = sequence.Dequeue();
        Play(next.Stream, next.FadeDuration);
    }
}

