# using namespace Mouledoux
The Mouledoux namespace is a collection of classes and functions specifically made for use within the Unity3D game engine, however they can be used in any C# project.

<br>

## Current classes and their uses
- Mediator class
  - Subscribe to, and broadcast string messages
  - Invoke delegates with an array of object arguments
  - Invoke delegates with NO arguments (quick broadcast)
- Finite State Machine (**under reconstruction**)
  - Add/remove arbitrary states
  - Add/remove transistions
  - "Any" state
  - Transistion to/from states 
  - Invoke delegates on transistions (soon to be replaced with Mouledoux.Callback)
- Callback [depricated] Callback has been replaced with Action<object[]>
  - Void delegate that takes object[] args
  - Packet [depricated]

## In progress
- Decision trees (AI)
