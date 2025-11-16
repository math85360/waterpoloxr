# Water Polo VR - Phase 1 MVP Setup Guide

## Overview

Phase 1 MVP implements the foundational systems for the Water Polo VR game as defined in CLAUDE.md.

### What's Implemented

✅ **Folder Structure**
- Organized according to CLAUDE.md architecture
- Core/, Players/, Ball/, UI/, Configuration/ folders

✅ **Core Systems**
- `EventBus` - Central event system for decoupled communication
- `MatchState` - Game state management (PREGAME, PLAYING, PAUSED, etc.)
- `GameClock` - Time management (quarters, shot clock, exclusion timers)
- `ScoreTable` - Score tracking and statistics
- `GameManager` - Coordinates all core systems

✅ **Player System**
- `WaterPoloPlayer` - Abstract base class for all players
- `AIPlayer` - Basic AI with simple movement and decision-making
- `VRPlayer` - VR player with Oculus/Meta Quest integration

✅ **Ball System**
- `BallController` - Ball state management (FREE, POSSESSED, SHOOTING, PASSING)
- Integrates with existing `BallBuoyancy` and `BallGrabAndThrow`

✅ **Goal System**
- `GoalDetector` - Detects ball crossing goal line
- Integrates with existing Goal prefab

---

## Scene Setup Instructions

### Step 1: Setup Core Systems

1. **Create an empty GameObject** named "GameManager" in your scene
2. **Add these components** to the GameManager object:
   - `GameManager`
   - `GameClock`
   - `MatchState`
   - `ScoreTable`

3. **Configure GameManager settings:**
   - Auto Start Match: ☑ (for testing)
   - Match Start Delay: 3 seconds
   - Assign Ball reference (drag ball GameObject from scene)

4. **Configure GameClock settings:**
   - Quarter Duration: 480 (8 minutes, or use 60 for quick testing)
   - Use Shot Clock: ☑
   - Shot Clock Duration: 30 seconds

5. **Configure ScoreTable settings:**
   - Home Team Name: "Home"
   - Away Team Name: "Away"

### Step 2: Setup Ball

1. **Find your Ball GameObject** in the scene
2. **Add `BallController` component**
3. **Verify ball has:**
   - Tag: "Ball"
   - `Rigidbody` component
   - `BallBuoyancy` component (existing)
   - Collider

### Step 3: Setup Goals

1. **Find your Goal prefabs** in the scene (should have 2, one for each team)
2. **On each Goal:**
   - Find the child object "GoalCollider" (the trigger collider)
   - Add `GoalDetector` component to it
   - Configure:
     - Goal Team: "Home" for one goal, "Away" for the other
     - Require Full Ball Crossing: ☑
     - Visualize Detection: ☑

3. **Add tag "Goal"** to both Goal GameObjects
   - Edit → Project Settings → Tags and Layers
   - Add tag "Goal"
   - Assign to both Goal GameObjects

### Step 4: Setup AI Players

1. **Create a new empty GameObject** for each AI player
2. **Name them:** "AI_Player_Home_1", "AI_Player_Away_1", etc.
3. **Add `AIPlayer` component**
4. **Configure AIPlayer:**
   - Player Name: e.g., "John"
   - Role: Select from dropdown (CenterForward, LeftWing, etc.)
   - Team Name: "Home" or "Away"
   - Swim Speed: 1.5

5. **Position players** in the pool (spread them out for testing)

6. **Create at least 4 players total** (2 per team minimum for testing)

### Step 5: Setup VR Player (Optional)

1. **Find your VR Camera Rig** (OVRCameraRig or similar)
2. **Create a new GameObject** as child of the rig
3. **Name it** "VR_Player"
4. **Add `VRPlayer` component**
5. **Configure VRPlayer:**
   - Player Name: "Player"
   - Role: Any position
   - Team Name: "Home" or "Away"
   - Head Transform: Assign CenterEyeAnchor from OVRCameraRig
   - VR Left Hand: Assign LeftHandAnchor
   - VR Right Hand: Assign RightHandAnchor
   - Use Thumbstick Movement: ☑
   - VR Swim Speed: 2.0

6. **Attach BallGrabAndThrow** (existing script) to hands if not already present

---

## Testing Checklist

### Basic Systems Test

1. ✅ **Play the scene** - GameManager should auto-start
2. ✅ **Check Console** - Should see "Match started!" message
3. ✅ **Watch GameClock** - Time should count down in Inspector
4. ✅ **Check MatchState** - Should be "PLAYING"

### AI Players Test

1. ✅ **AI players should move** towards formation positions
2. ✅ **AI should swim towards ball** when it's free
3. ✅ **AI should shoot** when close to goal with ball
4. ✅ **Check Gizmos** - Enable Gizmos in Scene view to see AI target positions

### Ball Test

1. ✅ **Ball should float** (BallBuoyancy working)
2. ✅ **Ball state changes** - Check BallController component in Inspector
3. ✅ **AI can grab ball** automatically when close
4. ✅ **Ball physics** - Throws should have realistic trajectories

### Goal Detection Test

1. ✅ **Manually throw ball** into goal (or use VR)
2. ✅ **Console should log** "GOAL! [Team] scores!"
3. ✅ **ScoreTable updates** - Check score in Inspector
4. ✅ **Ball resets** to center after goal
5. ✅ **Match continues** after goal

### VR Test (if VR player setup)

1. ✅ **Thumbstick movement** - Can swim around
2. ✅ **Grab ball** with grip button
3. ✅ **Throw ball** with trigger button
4. ✅ **Head tracking** - Body follows head position

---

## Debug Commands

The GameManager has debug context menu items (right-click component):

- **Start Match** - Manually start match
- **Pause Match** - Pause game
- **Resume Match** - Resume game
- **End Match** - Force end match
- **Score Goal (Home)** - Manually award goal to Home team
- **Score Goal (Away)** - Manually award goal to Away team

---

## Expected Behavior

### At Match Start
- Ball spawns at center
- AI players swim to formation positions
- Clock starts counting down
- Shot clock starts at 30 seconds

### During Play
- AI players compete for ball
- Player with ball moves towards opponent goal
- AI shoots when in range (~8m from goal)
- Shot clock resets when possession changes

### When Goal Scored
- Clock stops
- "GOAL!" message in console
- Score updates in ScoreTable
- Ball resets to center after 3 seconds
- Play resumes

### Quarter End
- Clock stops at 0:00
- Quarter ends message in console
- 5 second break
- Next quarter starts (or match ends if Q4)

---

## Common Issues & Solutions

### Issue: "No ScoreTable found!"
**Solution:** Make sure GameManager has ScoreTable component attached.

### Issue: "No ball found with tag 'Ball'"
**Solution:** Select ball GameObject → Inspector → Tag dropdown → "Ball"

### Issue: AI players not moving
**Solution:**
- Check that goals are tagged "Goal"
- Verify AI swim speed > 0
- Check MatchState is "PLAYING"

### Issue: Ball doesn't reset after goal
**Solution:** Verify ball has BallController component with proper ball reference.

### Issue: VR player can't grab ball
**Solution:**
- Verify BallGrabAndThrow is attached to hand GameObject
- Check grabbableLayer is set correctly
- Ensure ball has collider

### Issue: Goals not detecting
**Solution:**
- GoalDetector must be on the CHILD object with trigger collider
- Verify trigger collider has "Is Trigger" checked
- Check ball has Rigidbody and collider

---

## Next Steps (Phase 2)

Phase 1 MVP is complete when all tests pass. Phase 2 will add:

- Formation ScriptableObjects
- Advanced AI decision-making
- Referee system (basic foul detection)
- Improved VR interactions
- Player attributes and variations
- Team tactics

---

## File Structure Reference

```
Assets/WaterPolo/
├── Core/
│   ├── EventBus.cs
│   ├── MatchState.cs
│   ├── GameClock.cs
│   ├── ScoreTable.cs
│   ├── GameManager.cs
│   └── GoalDetector.cs
├── Players/
│   ├── WaterPoloPlayer.cs (abstract)
│   ├── AIPlayer.cs
│   └── VRPlayer.cs
├── Ball/
│   └── BallController.cs
└── README_PHASE1.md (this file)
```

---

## Support

For issues or questions:
1. Check CLAUDE.md for architecture details
2. Review console logs for errors
3. Use Debug context menu commands for testing
4. Enable Gizmos in Scene view to visualize AI behavior

---

**Version:** Phase 1 MVP
**Last Updated:** 2025-01-16
**Compatible Unity Version:** 2022.3+
**VR SDK:** Meta XR SDK (Oculus Integration)
