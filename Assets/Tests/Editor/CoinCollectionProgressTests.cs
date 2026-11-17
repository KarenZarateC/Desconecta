using Desconecta;
using NUnit.Framework;

public sealed class CoinCollectionProgressTests
{
    [Test] public void EachCoinAwardsTenPointsOnce()
    {
        var progress = new CoinCollectionProgress(new[] { "one", "two" });
        Assert.That(progress.TryCollect("one"), Is.True);
        Assert.That(progress.TryCollect("one"), Is.False);
        Assert.That(progress.Collected, Is.EqualTo(1));
        Assert.That(progress.BonusScore, Is.EqualTo(10));
    }

    [Test] public void UnregisteredAndInvalidIdsCannotAwardPoints()
    {
        var progress = new CoinCollectionProgress(new[] { "one", "one", "", null });
        Assert.That(progress.Total, Is.EqualTo(1));
        Assert.That(progress.TryCollect("unknown"), Is.False);
        Assert.That(progress.TryCollect(null), Is.False);
        Assert.That(progress.TryCollect(""), Is.False);
        Assert.That(progress.Collected, Is.Zero);
    }

    [Test] public void CompletingAllCoinsDoesNotRequireMissionProgress()
    {
        var progress = new CoinCollectionProgress(new[] { "one", "two" });
        Assert.That(progress.Complete, Is.False);
        progress.TryCollect("one"); progress.TryCollect("two");
        Assert.That(progress.Complete, Is.True);
        Assert.That(progress.BonusScore, Is.EqualTo(20));
    }

    [Test] public void RestartRestoresAllRewards()
    {
        var progress = new CoinCollectionProgress(new[] { "one" });
        progress.TryCollect("one"); progress.Reset();
        Assert.That(progress.Collected, Is.Zero);
        Assert.That(progress.BonusScore, Is.Zero);
        Assert.That(progress.Complete, Is.False);
        Assert.That(progress.TryCollect("one"), Is.True);
    }

    [Test] public void EmptyLevelDoesNotClaimCollectionComplete()
    {
        var progress = new CoinCollectionProgress(null);
        Assert.That(progress.Total, Is.Zero);
        Assert.That(progress.Complete, Is.False);
    }
}
