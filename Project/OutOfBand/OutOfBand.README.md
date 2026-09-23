# Out-of-Band Projects

Projects included here are examples of CrystalCatalyst API usage intended to stand outside the primary CrystalCatalyst solution.

They may be used as reference implementations or templates and may be freely copied out of this repository, built, run, and adapted. They rely only on a live NewAge environment and the installed or staged dependencies made available through that environment.

Their placement here is intentional.

Unlike projects in the main CrystalCatalyst solution, OutOfBand projects demonstrate the experience of consuming CrystalCatalyst as an external user would: through its published or staged assemblies rather than through direct project references.

## Purpose

OutOfBand projects serve several related purposes:

- demonstrate practical CrystalCatalyst API usage;
- provide small applications that can be copied and adapted;
- exercise CrystalCatalyst from outside its development solution;
- validate that the normal NewAge environment provides everything required by downstream consumers;
- provide realistic integration examples for developers and development agents;
- expose usability or dependency issues that may not appear when everything is built together inside one solution.

## Building

An OutOfBand project should be buildable after being copied to an unrelated source location, provided that a working NewAge environment is active and its normal reference paths and dependencies are available.

Projects here should not depend on their physical location inside the CrystalCatalyst repository.

In particular, they should avoid relying on direct project references merely because the CrystalCatalyst source tree happens to be nearby.

## Using These Projects

These projects are intended to be explored.

You are welcome to:

- run them as demonstrations;
- inspect them as API examples;
- copy them elsewhere as starting points;
- reduce them into smaller experiments;
- extend them into applications of your own.

When adapting one, the source code is generally more important than preserving the example exactly as written.

## Repository Access and Agents

An OutOfBand project should remain capable of building without access to the CrystalCatalyst source repository.

During development or diagnosis, however, a developer may choose to give an agent access to both the consuming project and the CrystalCatalyst repository. This can provide deeper implementation context while preserving the same external build boundary experienced by an ordinary consumer.

The distinction is useful:

**build dependency does not require source dependency.**

## Current Projects

### StreamerTrial

`StreamerTrial` exercises CrystalCatalyst windowing, CrystalSkia rendering, and CrystalOpenAL streaming from an external consumer project.

It began as a focused test of streamed audio and evolved into an oscilloscope-style visualization useful both as a demonstration and as an integration exercise.

See the project README for its current controls, capabilities, and usage.