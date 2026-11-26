# Operation animation

Portable Lens operations support three presentation styles: `Instant` commits immediately, `Primitive` animates the complete ordered `RecordedMove` expansion, and `Logical` animates the resulting local tile permutation. The command menu exposes all three plus `Automatic`, which chooses a logical animation for a directly counted operation when its permutation is supported, and otherwise animates the authorized primitive expansion.

Animation is presentation-only. `OperationAnimationPlan` captures the starting board, computes each intermediate board from the stored primitive rotations, and derives logical source/destination paths from tile identities in the final permutation. The live grid and recording controller are untouched while frames render. On completion the window rechecks the board, controller, mode, and position, then calls the existing operation commit once; move cost, recording, and victory handling remain on that boundary. Escape or focus loss cancels without committing.

Logical wrapped paths use the shortest toroidal displacement; primitive paths follow the recorded rotation direction. Both render clipped copies at the opposite board edge. Tile images and borders scale as one rounded visual frame; the lens symbols remain in the overlay above the moving tiles. Primitive playback and classic row/column drag animations continue using their existing presentation paths.

The verified `ColumnSwap` and `RowSwap` expansions produce two simultaneous tile exchanges (four moving tile identities). Logical motion partners are derived from the actual permutation, rather than assuming an unverified single pair or substituting a simplified transformation.
