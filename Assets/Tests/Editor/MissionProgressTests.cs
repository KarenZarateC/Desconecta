using NUnit.Framework;
using Desconecta;

public sealed class MissionProgressTests
{
    [Test] public void ResolvingSameSituationDoesNotAwardTwice()
    {
        var progress=new MissionProgress(7);
        Assert.That(progress.Resolve("tap"),Is.True);
        Assert.That(progress.Resolve("tap"),Is.False);
        Assert.That(progress.Score,Is.EqualTo(100));
        Assert.That(progress.Completed,Is.EqualTo(1));
    }
    [Test] public void PausedAndUnlimitedModesPreserveRemainingTime()
    {
        var progress=new MissionProgress(7);
        progress.Tick(40,false);
        Assert.That(progress.RemainingSeconds,Is.EqualTo(600));
        progress.Unlimited=true; progress.Tick(40,true);
        Assert.That(progress.RemainingSeconds,Is.EqualTo(600));
    }
    [Test] public void TimerExpiresWithoutBecomingNegative()
    {
        var progress=new MissionProgress(7);
        progress.Tick(601,true);
        Assert.That(progress.Expired,Is.True);
        Assert.That(progress.RemainingSeconds,Is.Zero);
        Assert.That(progress.Resolve("late"),Is.False);
    }
    [Test] public void PenaltiesDoNotMakeScoreNegative()
    {
        var progress=new MissionProgress(7);
        progress.Penalize(); Assert.That(progress.Score,Is.Zero);
        progress.Resolve("tap"); progress.Penalize();
        Assert.That(progress.Score,Is.EqualTo(75));
    }
    [Test] public void SeventhUniqueResolutionWinsAndLocksScore()
    {
        var progress=new MissionProgress(7);
        for(int i=0;i<7;i++) Assert.That(progress.Resolve("situation_"+i),Is.True);
        Assert.That(progress.Won,Is.True);
        progress.Penalize(); progress.Tick(50,true);
        Assert.That(progress.Score,Is.EqualTo(700));
        Assert.That(progress.RemainingSeconds,Is.EqualTo(600));
        Assert.That(progress.Resolve("extra"),Is.False);
    }
    [Test] public void InvalidIdCannotAdvanceProgress()
    {
        var progress=new MissionProgress(7);
        Assert.That(progress.Resolve(" "),Is.False);
        Assert.That(progress.Completed,Is.Zero);
    }
}
