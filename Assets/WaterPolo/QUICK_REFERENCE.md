# Water Polo VR - Quick Reference Guide

## Essential Links

- **Architecture:** [CLAUDE.md](../CLAUDE.md)
- **Setup Guide:** [README_PHASE1.md](README_PHASE1.md)
- **Implementation:** [IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)

---

## Common Tasks

### Add a New Player

```csharp
// 1. Create GameObject
GameObject playerObj = new GameObject("Player_John");

// 2. Add AIPlayer component (or VRPlayer for human)
AIPlayer player = playerObj.AddComponent<AIPlayer>();

// 3. Configure player
player.PlayerName = "John";
player.Role = PlayerRole.CenterForward;
player.TeamName = "Home";

// 4. Assign attributes (create ScriptableObject first)
player.Attributes = myPlayerAttributes;

// 5. Register with team
teamManager.AddPlayer(player);
```

### Create a New Formation

```csharp
// 1. In Unity: Create → WaterPolo → Formation
// 2. Configure in Inspector:

formationName = "4-2"; // Your formation name
positions = new FormationPosition[7] {
    new FormationPosition {
        role = PlayerRole.Goalkeeper,
        attackPosition = new Vector3(0, 0, 0),
        defensePosition = new Vector3(0, 0, 0)
    },
    // ... 6 more positions
};

// 3. Assign to FormationManager
formationManager.SetFormation(myFormation);
```

### Trigger a Foul

```csharp
// Create foul event
FoulEvent foul = new FoulEvent(
    FoulType.Holding,
    offender,       // WaterPoloPlayer
    victim,         // WaterPoloPlayer
    contactPoint,   // Vector3
    severity: 0.7f  // 0-1
);

// Report to referee
refereeSystem.ReportFoul(foul);
```

### Subscribe to Events

```csharp
private void Start()
{
    EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);
}

private void OnDestroy()
{
    EventBus.Instance.Unsubscribe<GoalScoredEvent>(OnGoalScored);
}

private void OnGoalScored(GoalScoredEvent evt)
{
    Debug.Log($"Goal by {evt.ScoringTeam}!");
}
```

### Change Game State

```csharp
// From any system with MatchState reference
matchState.TransitionToState(MatchStateType.PLAYING);
matchState.TransitionToState(MatchStateType.PAUSED);
matchState.TransitionToState(MatchStateType.PENALTY);
```

---

## Key Systems Reference

### GameManager
**Location:** Core/GameManager.cs
**Purpose:** Coordinates all core systems
**Key Methods:**
- `StartMatch()` - Begin match
- `PauseMatch()` / `ResumeMatch()` - Control flow
- `EndMatch()` - Finish match

### EventBus
**Location:** Core/EventBus.cs
**Purpose:** Decoupled event communication
**Usage:**
```csharp
// Publish
EventBus.Instance.Publish(new GoalScoredEvent(team, scorer, newScore));

// Subscribe
EventBus.Instance.Subscribe<GoalScoredEvent>(OnGoalScored);
```

### GameClock
**Location:** Core/GameClock.cs
**Key Properties:**
- `CurrentQuarter` - 1-4
- `QuarterTimeRemaining` - Seconds left
- `ShotClockRemaining` - Shot clock seconds
**Key Methods:**
- `StartClock()` / `StopClock()`
- `ResetShotClock()`
- `StartExclusion(player, duration)`

### ScoreTable
**Location:** Core/ScoreTable.cs
**Key Methods:**
- `RegisterGoal(team, scorer)`
- `RecordShot(team, onTarget)`
- `RecordFoul(team)`
- `GetWinner()`

### RefereeSystem
**Location:** Referee/RefereeSystem.cs
**Key Methods:**
- `ReportFoul(FoulEvent)` - Report detected foul
**States:** OBSERVING → ADVANTAGE_PENDING → WHISTLING → FOUL_MANAGEMENT

---

## Event Types Quick List

```csharp
// Match Flow
GamePausedEvent(reason)
GameResumedEvent()
QuarterEndedEvent(quarter)
MatchEndedEvent(winnerTeam)

// Scoring
GoalScoredEvent(team, scorer, newScore)
GoalValidatedEvent(scorer)
GoalInvalidatedEvent(reason)

// Clock
ShotClockExpiredEvent()
ExclusionStartedEvent(player, duration)
ExclusionEndedEvent(player)

// Ball
BallPossessionChangedEvent(oldOwner, newOwner)

// Tactical
TacticalAdaptationEvent(type, oldTactic, newTactic, reason)

// Referee
FoulDetectedEvent(foul, sanction)

// Communication
BallCallMadeEvent(caller, type, target)
```

---

## Enums Reference

### PlayerRole
```csharp
Goalkeeper, CenterForward, LeftWing, RightWing,
LeftDriver, RightDriver, CenterBack
```

### MatchStateType
```csharp
PREGAME, PLAYING, FOUL_CALLED, FREE_THROW, PENALTY,
EXCLUSION, GOAL_SCORED, QUARTER_END, TIMEOUT, PAUSED, POSTGAME
```

### FoulType
```csharp
None, Holding, Sinking, Obstruction, Brutality,
BallUnderwater, ShotClockViolation, IllegalMovement,
GoalkeeperViolation, TwoMeterViolation, PushingOff, Interference
```

### FoulSanction
```csharp
None, OrdinaryFoul, Exclusion, Penalty,
ExclusionAndPenalty, PermanentExclusion
```

### DefenseType
```csharp
ManToMan, Zone, Pressing, Wall
```

### CallType
```csharp
RequestBall, ImOpen, Screen, Switch, Shot,
Time, Defense, Help, Cut, Post
```

---

## ScriptableObject Templates

### PlayerAttributes Template
```
Physical: swimSpeed, acceleration, endurance, strength, verticalReach
Technical: shotPower, shotAccuracy, passAccuracy, ballControl, catchAbility
Tactical: gameReading, positioning, anticipation, decisionSpeed
Mental: composure, aggression, creativity
Specialties: foulDrawingExpert, screenSpecialist, counterAttackThreat, etc.
```

**Quick Generate:** Context Menu → "Generate Random Attributes"

### RefereeProfile Template
```
Tolerance: strictness, advantageOrientation, consistencyLevel
Detection: visionAccuracy, twoMeterLineVigilance, errorRate
Rules: exclusionTendency, handCheckStrictness, penaltyThreshold
Physical: movementSpeed, anticipationDistance
```

### Formation Template
```
formationName: "3-3" or "4-2" etc.
positions[7]: One for each role
  - attackPosition: Vector3
  - defensePosition: Vector3
  - influenceRadius: float
  - attackRules: List<PositioningRule>
  - defenseRules: List<PositioningRule>
```

---

## Debug Commands

### In Inspector (Context Menu)

**GameManager:**
- Right-click → "Start Match"
- Right-click → "Pause Match"
- Right-click → "Score Goal (Home)"

**PlayerAttributes:**
- Right-click → "Generate Random Attributes"
- Right-click → "Make Elite Player"

**TacticalLearningSystem:**
- Right-click → "Force Analysis"

**CoachAI:**
- Right-click → "Review Tactics Now"

### In Code
```csharp
// Enable Gizmos in Scene view to see:
// - Formation positions
// - AI target positions
// - Ball possession radius

// Check Inspector during Play mode:
// - All [SerializeField] fields update in real-time
// - Watch MatchState.CurrentState
// - Monitor GameClock times
// - See ScoreTable scores
```

---

## Common Integration Points

### Integrate Existing Ball Scripts
```csharp
// BallController uses existing BallBuoyancy
GameObject ball = GameObject.FindGameObjectWithTag("Ball");
BallController controller = ball.AddComponent<BallController>();
// BallBuoyancy keeps working, BallController adds state management
```

### Integrate VR Hands
```csharp
// VRPlayer references existing BallGrabAndThrow
VRPlayer vrPlayer = ...;
vrPlayer._ballGrabSystem = GetComponentInChildren<BallGrabAndThrow>();
// Both systems cooperate
```

### Add Goal Detection
```csharp
// Find GoalCollider child (the trigger collider)
Transform goalCollider = goalPrefab.Find("GoalCollider");
GoalDetector detector = goalCollider.gameObject.AddComponent<GoalDetector>();
detector._goalTeam = "Home"; // or "Away"
```

---

## Performance Tips

1. **Throttle Updates**
   ```csharp
   if (Time.frameCount % 60 == 0) // Once per second
   {
       ExpensiveOperation();
   }
   ```

2. **Cache References**
   ```csharp
   // DON'T do this in Update:
   FindObjectOfType<GameClock>()

   // DO cache in Awake/Start:
   _gameClock = FindObjectOfType<GameClock>();
   ```

3. **Unsubscribe Events**
   ```csharp
   private void OnDestroy()
   {
       EventBus.Instance.Unsubscribe<EventType>(Handler);
   }
   ```

---

## Troubleshooting

### "No ScoreTable found!"
- GameManager needs ScoreTable component attached
- Or ScoreTable on another GameObject in scene

### AI Players Not Moving
- Check MatchState is PLAYING
- Verify goals are tagged "Goal"
- Ensure swim speed > 0

### Goals Not Detecting
- GoalDetector on CHILD object with trigger collider
- Ball needs tag "Ball"
- Ball needs Rigidbody + Collider

### VR Player Can't Grab Ball
- BallGrabAndThrow attached to hand
- Grabbable layer set correctly
- Ball has collider

### Events Not Firing
- Check Subscribe called in Start/Awake
- Check Unsubscribe in OnDestroy
- Verify EventBus.Instance exists

---

## Extending the System

### Add New Positioning Rule
```csharp
// 1. Add to enum in WaterPoloFormation.cs
public enum PositioningRuleType
{
    ...
    MyNewRule
}

// 2. Implement in FormationManager.CalculateRuleOffset()
case PositioningRuleType.MyNewRule:
    return CalculateMyNewRuleOffset(player);
```

### Add New Game Mode
```csharp
// 1. Create new script inheriting GameMode
public class MyGameMode : GameMode
{
    public override void Setup() { /* ... */ }
    public override void StartGame() { /* ... */ }
    protected override void UpdateGameLogic() { /* ... */ }
    public override bool CheckWinCondition() { /* ... */ }
    public override void OnGoalScored(GoalScoredEvent evt) { /* ... */ }
    public override void EndGame() { /* ... */ }
}

// 2. Add to GameObject and configure
```

### Add New Event Type
```csharp
// In EventBus.cs, add:
public class MyNewEvent : GameEvent
{
    public string Data { get; private set; }

    public MyNewEvent(string data)
    {
        Data = data;
    }
}

// Publish:
EventBus.Instance.Publish(new MyNewEvent("test"));

// Subscribe:
EventBus.Instance.Subscribe<MyNewEvent>(OnMyEvent);
```

---

## Best Practices

1. **Always use EventBus** for system communication
2. **Cache all FindObjectOfType** results
3. **Use ScriptableObjects** for configuration
4. **Subscribe in Start**, unsubscribe in OnDestroy
5. **Add [Header]** attributes for Inspector clarity
6. **Document public methods** with XML comments
7. **Use SerializeField** for Inspector visibility
8. **Regions** for code organization
9. **Gizmos** for spatial debugging
10. **Context menus** for testing

---

## Key Design Patterns

- **Observer:** EventBus for decoupled communication
- **Strategy:** GameMode, PositioningRule variants
- **State Machine:** RefereeSystem, MatchState
- **Component:** Unity ECS philosophy
- **Factory:** ScriptableObjects as data factories
- **Template Method:** GameMode abstract base

---

## File Naming Conventions

- **Classes:** PascalCase (GameManager, TeamTactics)
- **Files:** Match class name (TeamTactics.cs)
- **Variables:** _camelCase for private, camelCase for local
- **Properties:** PascalCase
- **Events:** PascalCase + "Event" suffix
- **Enums:** PascalCase + "Type" suffix (optional)

---

## Testing Workflow

1. **Setup Scene** → README_PHASE1.md
2. **Create ScriptableObjects** → Formations, Profiles, Attributes
3. **Assign References** → GameManager, Teams, Goals
4. **Play Mode** → Watch Inspector values
5. **Enable Gizmos** → See formations, targets
6. **Use Context Menus** → Trigger events manually
7. **Check Console** → Debug logs show system activity

---

## Questions?

- **Architecture:** See [CLAUDE.md](../CLAUDE.md)
- **Setup:** See [README_PHASE1.md](README_PHASE1.md)
- **Implementation:** See [IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)
- **This Guide:** Quick reference for common tasks

---

**Version:** 1.0
**Last Updated:** 2025-01-16
**For:** Water Polo VR (Unity 2022.3+)
