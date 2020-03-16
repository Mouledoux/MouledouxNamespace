# using namespace Mouledoux
The Mouledoux namespace is a collection of classes and functions specifically made for use within the Unity3D game engine, however they can be used in any C# project.

<br>

## Current classes and their uses
- Mediator class
  - Subscribe to, and broadcast string messages
  - Invoke delegates with an array of object arguments
  - Invoke delegates with NO arguments (quick broadcast)
- ~~Finite State Machine (**under reconstruction**)~~
  - ~~Add/remove arbitrary states~~
  - ~~Add/remove transitions~~
  - ~~"Any" state~~
  - ~~Transition to/from states~~
  - ~~Invoke delegates on transitions (soon to be replaced with Mouledoux.Callback)~~
- **NEW** Superior State Machine
  - Only tracks current state
  - NO MANUAL STATE CHANGES
  - States are automatically transitioned to when predefined prerequisites are met
  - Unique On Transition events for each transistion, not each state
- ~~Callback **[depricated] Callback has been replaced with Action<object[]>**~~
  - ~~Void delegate that takes object[] args~~
  - ~~Packet **[depricated]**~~

## In progress
- Decision trees (AI)
- Data Containers (public, static, storage for arbitrary variables)
