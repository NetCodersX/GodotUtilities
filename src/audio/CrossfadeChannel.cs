using Godot;

namespace Utilities.AudioManagement;

public sealed class CrossfadeChannel
{
    private readonly record struct PendingTrack(AudioStream Stream, float FadeDuration);

    private readonly AudioStreamPlayer playerA;
    private readonly AudioStreamPlayer playerB;
    private bool _usingA;
    private Tween tween;

    private PendingTrack? next;

    public CrossfadeChannel(Node parent, string bus)
    {
        playerA = MakePlayer(bus);
        playerB = MakePlayer(bus);
        parent.AddChild(playerA);
        parent.AddChild(playerB);
    }

    private static AudioStreamPlayer MakePlayer(string bus) => new() { Bus = bus };

    private void OnTweenFinished()
    {
        var stoppedPlayer = _usingA ? playerA : playerB;
        stoppedPlayer.Stop();
    }

    public void Play(AudioStream stream, float fadeDuration)
    {
        var incoming = _usingA ? playerA : playerB;
        var outgoing = _usingA ? playerB : playerA;
        _usingA      = !_usingA;

        incoming.Stream       = stream;
        incoming.VolumeLinear = 0f;
        incoming.Play();

        incoming.Finished += OnTrackFinished;

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
            next = new(stream, fadeDuration);
    }

    public void Stop(float fadeDuration)
    {
        tween.KillIfValid();
        tween = null;

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

    private static void FadeOut(AudioStreamPlayer player, float fadeDuration)
    {
        if (!player.Playing) return;
        var t = player.CreateTween();
        t.TweenVolumeLinear(player, 0f, fadeDuration);
        t.OnFinished(player.Stop);
    }

    private void OnTrackFinished()
    {
        var finishedPlayer = _usingA ? playerB : playerA;
        finishedPlayer.Finished -= OnTrackFinished;

        if (next.HasValue)
        {
            var pending = next.Value;
            next = null;
            Play(pending.Stream, pending.FadeDuration);
        }
    }
}

