using Xunit;

namespace SlideScramble.Tests;

public class PortableLensInteractionTests
{
    [Theory]
    [InlineData(3, 3)]
    [InlineData(5, 3)]
    [InlineData(7, 6)]
    public void EveryPlacementMapsWrappedBoardCellsToCanonicalLensSymbols(int width, int height)
    {
        var interaction = new PortableLensInteractionController();
        for (int anchorY = 0; anchorY < height; anchorY++)
        for (int anchorX = 0; anchorX < width; anchorX++)
        {
            Assert.True(interaction.BeginPlacement(width, height));
            Assert.True(interaction.SetPlacementAnchor(anchorX, anchorY));
            Assert.True(interaction.AcceptPosition());

            foreach (PortableLensCell cell in Enum.GetValues<PortableLensCell>())
            {
                var (offsetX, offsetY) = LensGestureOperationMapper.Coordinates(cell);
                int x = (anchorX + offsetX) % width;
                int y = (anchorY + offsetY) % height;
                Assert.Equal(cell, interaction.CellAt(x, y));
            }
        }
    }

    [Fact]
    public void PlacementRequiresEnterAndAcceptedLensCanBeReusedUntilEscape()
    {
        var interaction = new PortableLensInteractionController();
        Assert.True(interaction.BeginPlacement(4, 3));
        Assert.True(interaction.SetPlacementAnchor(3, 2));
        Assert.Equal(PortableLensInteractionPhase.Positioning, interaction.Phase);
        Assert.Null(interaction.CellAt(3, 2));
        Assert.True(interaction.AcceptPosition());
        Assert.Null(interaction.CellAt(-1, 0));
        Assert.Null(interaction.CellAt(4, 0));
        Assert.Null(interaction.CellAt(0, 3));

        for (int i = 0; i < 2; i++)
        {
            Assert.True(interaction.BeginGesture(PortableLensCell.Circle, false));
            var request = interaction.CompleteGesture(PortableLensCell.Star);
            Assert.NotNull(request);
            Assert.Equal(PuzzleOperationIds.Shuffle, request.OperationId);
            Assert.Equal(3, request.AnchorX);
            Assert.Equal(2, request.AnchorY);
            Assert.False(interaction.HasPendingGesture);
        }

        interaction.Cancel();
        Assert.Equal(PortableLensInteractionPhase.Inactive, interaction.Phase);
        Assert.Null(interaction.CellAt(3, 2));
    }

    [Fact]
    public void IncompleteInvalidAndCancelledGesturesProduceNoRequest()
    {
        var interaction = AcceptedLens();

        Assert.Null(interaction.CompleteGesture(PortableLensCell.Star));
        Assert.True(interaction.BeginGesture(PortableLensCell.Circle, false));
        Assert.Null(interaction.CompleteGesture(PortableLensCell.Square));
        Assert.False(interaction.HasPendingGesture);
        Assert.True(interaction.BeginGesture(PortableLensCell.Circle, false));
        interaction.CancelGesture();
        Assert.Null(interaction.CompleteGesture(PortableLensCell.Star));
        Assert.False(interaction.HasPendingGesture);
    }

    [Fact]
    public void ModifierIsCapturedAtGestureStartAndModifierOnlyDoesNothing()
    {
        var interaction = AcceptedLens();
        Assert.False(interaction.HasPendingGesture);

        Assert.True(interaction.BeginGesture(PortableLensCell.Circle, true));
        LensGestureRequest? request = interaction.CompleteGesture(PortableLensCell.Star);

        Assert.NotNull(request);
        Assert.Equal(LensOperationFamily.ColumnSwap, request.Family);
        Assert.Null(request.ShuffleIntent);
        Assert.Equal(PortableLensInteractionPhase.Accepted, interaction.Phase);
    }

    [Fact]
    public void AcceptedLensCanBeRepositionedAndInvalidOffsetsLeaveItUnchanged()
    {
        var interaction = AcceptedLens();
        Assert.True(interaction.BeginGesture(PortableLensCell.Circle, false, 120, 85));

        Assert.False(interaction.RepositionAndAccept(3, 0));
        Assert.Equal(0, interaction.AnchorX);
        Assert.Equal(0, interaction.AnchorY);
        Assert.True(interaction.HasPendingGesture);

        Assert.True(interaction.RepositionAndAccept(2, 1));
        Assert.Equal(2, interaction.AnchorX);
        Assert.Equal(1, interaction.AnchorY);
        Assert.Equal(PortableLensInteractionPhase.Accepted, interaction.Phase);
        Assert.False(interaction.HasPendingGesture);
        Assert.Null(interaction.GestureStartPointer);
        Assert.Null(interaction.GesturePointer);
    }

    [Fact]
    public void GestureTracksActualPointerOffsetsAndClearsThemWhenCompleted()
    {
        var interaction = AcceptedLens();
        Assert.True(interaction.BeginGesture(PortableLensCell.Circle, false, 120, 85));

        interaction.UpdateGesturePointer(164, 111);

        Assert.Equal((120, 85), interaction.GestureStartPointer);
        Assert.Equal((164, 111), interaction.GesturePointer);
        Assert.NotNull(interaction.CompleteGesture(PortableLensCell.Star));
        Assert.Null(interaction.GestureStartPointer);
        Assert.Null(interaction.GesturePointer);
    }

    [Theory]
    [InlineData(PortableLensCell.Star, -1, 1, PortableLensCell.Circle, 5, 0)]
    [InlineData(PortableLensCell.Circle, 5, 1, PortableLensCell.Star, -5, 0)]
    public void HorizontalSplitLensMapsAdjacentOutsidePointersAcrossEitherBoardEdge(
        PortableLensCell start, int pointerX, int pointerY, PortableLensCell expectedTarget,
        int expectedWrapOffsetX, int expectedWrapOffsetY)
    {
        var interaction = AcceptedLens(5, 4, 4, 1);
        Assert.True(interaction.BeginGesture(start, false));

        Assert.True(interaction.TryGetGestureTarget(pointerX, pointerY, out PortableLensCell target,
            out int wrapOffsetX, out int wrapOffsetY));

        Assert.Equal(expectedTarget, target);
        Assert.Equal(expectedWrapOffsetX, wrapOffsetX);
        Assert.Equal(expectedWrapOffsetY, wrapOffsetY);
        Assert.NotNull(interaction.CompleteGesture(target));
    }

    [Theory]
    [InlineData(PortableLensCell.Triangle, 1, -1, PortableLensCell.Circle, 0, 4)]
    [InlineData(PortableLensCell.Circle, 1, 4, PortableLensCell.Triangle, 0, -4)]
    public void VerticalSplitLensMapsAdjacentOutsidePointersAcrossEitherBoardEdge(
        PortableLensCell start, int pointerX, int pointerY, PortableLensCell expectedTarget,
        int expectedWrapOffsetX, int expectedWrapOffsetY)
    {
        var interaction = AcceptedLens(5, 4, 1, 3);
        Assert.True(interaction.BeginGesture(start, false));

        Assert.True(interaction.TryGetGestureTarget(pointerX, pointerY, out PortableLensCell target,
            out int wrapOffsetX, out int wrapOffsetY));

        Assert.Equal(expectedTarget, target);
        Assert.Equal(expectedWrapOffsetX, wrapOffsetX);
        Assert.Equal(expectedWrapOffsetY, wrapOffsetY);
        Assert.NotNull(interaction.CompleteGesture(target));
    }

    [Fact]
    public void WrappedGestureRejectsCornersUnsplitEdgesAndInvalidCellPairs()
    {
        var horizontalSplit = AcceptedLens(5, 4, 4, 1);
        Assert.True(horizontalSplit.BeginGesture(PortableLensCell.Circle, false));
        Assert.False(horizontalSplit.TryGetGestureTarget(-1, -1, out _, out _, out _));
        Assert.False(horizontalSplit.TryGetGestureTarget(-2, 1, out _, out _, out _));
        Assert.False(horizontalSplit.TryGetGestureTarget(0, 2, out _, out _, out _));

        var unsplit = AcceptedLens(5, 4, 0, 1);
        Assert.True(unsplit.BeginGesture(PortableLensCell.Circle, false));
        Assert.False(unsplit.TryGetGestureTarget(5, 1, out _, out _, out _));

        var splitTop = AcceptedLens(5, 4, 1, 3);
        Assert.True(splitTop.BeginGesture(PortableLensCell.Circle, false));
        Assert.False(splitTop.TryGetGestureTarget(1, 3, out _, out _, out _));
        Assert.False(splitTop.TryGetGestureTarget(2, 0, out _, out _, out _));
    }

    [Fact]
    public void LensCannotBePositionedOnBoardSmallerThanAtlasMinimum()
    {
        var interaction = new PortableLensInteractionController();
        Assert.False(interaction.BeginPlacement(2, 4));
        Assert.Equal(PortableLensInteractionPhase.Inactive, interaction.Phase);
    }

    private static PortableLensInteractionController AcceptedLens()
    {
        return AcceptedLens(3, 3, 0, 0);
    }

    private static PortableLensInteractionController AcceptedLens(int width, int height, int anchorX, int anchorY)
    {
        var interaction = new PortableLensInteractionController();
        Assert.True(interaction.BeginPlacement(width, height, anchorX, anchorY));
        Assert.True(interaction.AcceptPosition());
        return interaction;
    }
}

public class PortableLensDoubleClickTrackerTests
{
    [Fact]
    public void TwoStationaryClicksWithinTheIntervalAreRecognizedOnce()
    {
        var tracker = new PortableLensDoubleClickTracker();
        tracker.BeginPress(100, 80, 0);
        Assert.False(tracker.CompletePress(102, 81, 40));

        tracker.BeginPress(105, 82, 100);

        Assert.True(tracker.CompletePress(106, 83, 140));
        tracker.BeginPress(105, 82, 180);
        Assert.False(tracker.CompletePress(105, 82, 200));
    }

    [Fact]
    public void SlowOrMovingClicksDoNotCompleteADoubleClick()
    {
        var tracker = new PortableLensDoubleClickTracker();
        tracker.BeginPress(100, 80, 0);
        Assert.False(tracker.CompletePress(101, 80, 40));
        tracker.BeginPress(100, 80, 700);
        Assert.False(tracker.CompletePress(100, 80, 740));

        tracker.BeginPress(100, 80, 800);
        Assert.False(tracker.CompletePress(130, 80, 840));
        tracker.BeginPress(101, 81, 880);
        Assert.False(tracker.CompletePress(101, 81, 920));
    }

    [Fact]
    public void ResetAndIneligiblePressesClearThePendingClick()
    {
        var tracker = new PortableLensDoubleClickTracker();
        tracker.BeginPress(100, 80, 0);
        Assert.False(tracker.CompletePress(100, 80, 40));
        tracker.Reset();
        tracker.BeginPress(100, 80, 80);
        Assert.False(tracker.CompletePress(100, 80, 120));
        tracker.BeginPress(100, 80, 160, eligible: false);
        Assert.False(tracker.CompletePress(100, 80, 200));
    }
}