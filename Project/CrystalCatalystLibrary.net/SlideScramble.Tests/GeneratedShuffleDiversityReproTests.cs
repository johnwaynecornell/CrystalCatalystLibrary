using Xunit;

namespace SlideScramble.Tests;

public class GeneratedShuffleDiversityReproTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    [InlineData(51)]
    public void GeneratedShuffleSequenceUsesEveryDirectedGestureWithinEachCycle(int seed)
    {
        var registry = OperationRegistry.CreateDefault();
        var rules = PuzzleRuleSet.CreateShuffleLogic(registry);
        var challenge = ShuffleChallengeFactory.GenerateRuleAware(3, 3, 16, rules,
            new OperationRuleEvaluator(registry), new Random(seed));
        var gestures = challenge.ScramblePlans.Select(plan => (ShuffleOperationIntent)plan.Intent)
            .Select(intent => (intent.StartCell, intent.TargetCell)).ToArray();

        Assert.True(gestures.Length >= 16);
        foreach (var cycle in gestures.Take(16).Chunk(8))
            Assert.Equal(8, cycle.Distinct().Count());
    }
}