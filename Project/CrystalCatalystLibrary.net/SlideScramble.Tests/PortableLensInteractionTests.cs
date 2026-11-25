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
    public void LensCannotBePositionedOnBoardSmallerThanAtlasMinimum()
    {
        var interaction = new PortableLensInteractionController();
        Assert.False(interaction.BeginPlacement(2, 4));
        Assert.Equal(PortableLensInteractionPhase.Inactive, interaction.Phase);
    }

    private static PortableLensInteractionController AcceptedLens()
    {
        var interaction = new PortableLensInteractionController();
        Assert.True(interaction.BeginPlacement(3, 3));
        Assert.True(interaction.AcceptPosition());
        return interaction;
    }
}