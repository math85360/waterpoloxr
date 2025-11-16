# Water Polo VR - Architecture Documentation

## Table des matières

1. [Vue d'ensemble du projet](#vue-densemble-du-projet)
2. [Architecture globale](#architecture-globale)
3. [Systèmes core](#systèmes-core)
4. [Système d'IA et tactiques](#système-dia-et-tactiques)
5. [Système d'arbitrage](#système-darbitrage)
6. [Gestion du ballon](#gestion-du-ballon)
7. [Intégration VR](#intégration-vr)
8. [Modes de jeu](#modes-de-jeu)
9. [Configuration et paramètres](#configuration-et-paramètres)
10. [Phases d'implémentation](#phases-dimplémentation)

---

## Vue d'ensemble du projet

### Concept
Jeu de water-polo en VR pour Meta Quest avec simulation réaliste des règles, tactiques et IA d'équipe.

### Plateformes
- **Principale** : Meta Quest (VR)
- **Future** : Console (multi-joueurs local), Mobile
- **Expérimental** : XR modes (maison avec passthrough)

### Principes de design
- **Modulaire** : Systèmes indépendants et découplés
- **Paramétrable** : Configuration via ScriptableObjects
- **Extensible** : Nouveaux modes sans refonte
- **Data-driven** : Logique séparée des données

---

## Architecture globale

### Hiérarchie des systèmes

```
WaterPoloGame
│
├─ Match Management
│   ├─ GameClock (temps de jeu, shot clocks, exclusions)
│   ├─ RefereeSystem (arbitrage autonome, détection active)
│   ├─ ScoreTable (score, statistiques)
│   └─ MatchState (playing, foul, timeout, etc.)
│
├─ Team Layer
│   ├─ CoachAI (décisions tactiques, remplacements)
│   ├─ TacticalLearningSystem (observation adversaire, Q2+)
│   ├─ TeamTactics (stratégie défense/attaque)
│   └─ RosterManager (actifs/banc/exclus définitifs)
│
├─ Player Layer
│   ├─ WaterPoloPlayer (interface abstraite)
│   │   ├─ AIPlayer (IA pure)
│   │   ├─ VRPlayer (humain actif)
│   │   └─ ObservedAIPlayer (IA observée en VR)
│   ├─ PlayerAttributes (physique, technique, tactique, mental)
│   ├─ PlayerTraits (spécialités, styles)
│   └─ ActionEvaluator (utility-based decisions)
│
├─ Tactical Systems
│   ├─ Formation Management (positionnement base)
│   ├─ Rule-based Positioning (intervalles, écrans, zones)
│   ├─ Adaptation Systems (zone, wall, infériorité)
│   └─ Communication (appels de balle, intentions)
│
├─ Physics & Detection
│   ├─ BallController (états : libre/possédé/vol)
│   ├─ Contact Detection (niveau eau, force, zones)
│   ├─ Shooting Detection (orientation, bras, distance, timing)
│   └─ Foul Mechanics (simulation, grabbing, violence)
│
└─ Display & Interface
    ├─ Platform-specific displays (VR/Console/Mobile)
    ├─ Scoreboard (physique + UI selon plateforme)
    └─ Event visualization (fautes, buts, exclusions)
```

### Communication inter-systèmes

**Architecture événementielle** : Event Bus central pour découpler les systèmes

```csharp
Événements principaux :
├─ FoulDetected, WhistleBlown, SanctionApplied
├─ GoalScored, ShotClockExpired
├─ ExclusionStarted, ExclusionEnded
├─ TacticalAdaptation, FormationChanged
├─ BallCallMade, ShootingStanceEntered
├─ PatternDetected, PlayerReplaced
└─ GameStateChanged
```

---

## Systèmes core

### GameClock

**Responsabilités** :
- Temps de match (4 quarts-temps de 8 minutes)
- Shot clock (30 secondes par possession, 20s en supériorité)
- Horloges d'exclusion indépendantes (20 secondes chacune)
- Contrôle pause/reprise

**Particularités** :
- Shot clock et exclusions sont **indépendantes** du temps de jeu
- Peut gérer plusieurs exclusions simultanées
- Contrôlé principalement par l'arbitre

```csharp
public class GameClock : MonoBehaviour
{
    // Match time
    public float quarterDuration = 480f;  // 8 minutes
    public int currentQuarter = 1;
    public float quarterTimeRemaining = 480f;
    
    // Shot clock
    public float shotClockDuration = 30f;
    public float shotClockRemaining = 30f;
    public bool shotClockRunning = false;
    
    // Exclusion clocks (indépendantes)
    public List<ExclusionClock> activeExclusions;
    
    public bool isRunning = false;
    public bool isPaused = false;
}
```

### ScoreTable

**Responsabilités** :
- Suivi du score
- Statistiques de match (tirs, passes, fautes, exclusions)
- Historique des événements

### MatchState

**États possibles** :
- `PLAYING` : Jeu actif
- `FOUL_CALLED` : Arbitre a sifflé
- `FREE_THROW` : Placement pour coup franc
- `PENALTY` : Pénalty (5m, gardien seul)
- `EXCLUSION` : Placement après exclusion
- `GOAL_SCORED` : Célébration + reset
- `QUARTER_END` : Transition entre quarts
- `TIMEOUT` : Temps mort
- `PAUSED` : Pause générale

---

## Système d'IA et tactiques

### Architecture hiérarchique de décision

```
CoachAI (stratégie globale)
    ↓
TeamTactics (principes collectifs)
    ↓
RoleConfiguration (responsabilités par rôle)
    ↓
PlayerAI (décisions individuelles contextuelles)
    ↓
Actions (exécution)
```

### WaterPoloPlayer (interface abstraite)

**Classe de base pour tous les joueurs** :

```csharp
public abstract class WaterPoloPlayer : MonoBehaviour
{
    [Header("Identity")]
    public PlayerRole role;
    public PlayerAttributes attributes;
    
    [Header("State")]
    public Vector3 targetPosition;
    public PlayerAction currentAction;
    
    // Interface commune
    public abstract void DecideAction(GameContext context);
    public abstract void ExecuteAction();
    public abstract Vector3 GetArmPosition(ArmSide side);
    public abstract Quaternion GetHandRotation(ArmSide side);
}
```

**Implémentations** :
1. **AIPlayer** : IA pure avec DecisionMaker
2. **VRPlayer** : Contrôle humain via manettes/tracking
3. **ObservedAIPlayer** : IA dont les mouvements sont synchronisés en VR (mode tutoriel)

### PlayerAttributes

**Catégories d'attributs** (tous entre 0-1) :

```csharp
[CreateAssetMenu(fileName = "PlayerAttributes", menuName = "WaterPolo/PlayerAttributes")]
public class PlayerAttributes : ScriptableObject
{
    [Header("Physical")]
    public float swimSpeed;        // Vitesse pure
    public float acceleration;     // Explosivité
    public float endurance;        // Fatigue au fil du match
    public float strength;         // Poids, puissance
    public float verticalReach;    // Détente, interceptions hautes
    
    [Header("Technical")]
    public float shotPower;
    public float shotAccuracy;
    public float passAccuracy;
    public float ballControl;      // Garde le ballon sous pression
    public float catchAbility;     // Réception difficile
    
    [Header("Tactical")]
    public float gameReading;      // Reconnaissance défense
    public float positioning;      // Placement tactique
    public float anticipation;     // Prédiction actions
    public float decisionSpeed;    // Rapidité de décision
    
    [Header("Mental")]
    public float composure;        // Sous pression temporelle
    public float aggression;       // Cherche le contact
    public float creativity;       // Actions inattendues
    
    [Header("Specialties")]
    public bool foulDrawingExpert;     // Provoque facilement fautes
    public bool screenSpecialist;      // Bon en écrans
    public bool wristGrabber;          // Attrape poignets défenseur
    public bool counterAttackThreat;   // Dangereux en transition
}
```

### Formations et tactiques

**WaterPoloFormation (ScriptableObject)** :

```csharp
[CreateAssetMenu(fileName = "Formation", menuName = "WaterPolo/Formation")]
public class WaterPoloFormation : ScriptableObject
{
    public string formationName;
    
    [Header("Positions")]
    public FormationPosition[] attackPositions;  // Positions en attaque
    public FormationPosition[] defensePositions; // Positions en défense
}

[System.Serializable]
public class FormationPosition
{
    public PlayerRole role;          // "Pointe", "Ailier gauche", etc.
    public Vector3 basePosition;     // Position relative au terrain
    public float influenceRadius;    // Zone de responsabilité
    public List<PositioningRule> positioningRules;
}
```

**PositioningRule (règles de placement)** :
- `FormationBase` : Position fixe de la formation
- `IntervalDefense` : Se place entre deux adversaires
- `PressCarrier` : Presse le porteur du ballon
- `ZoneCoverage` : Couvre une zone spécifique
- `SupportBallCarrier` : Soutien du porteur
- `OppositeWing` : Va sur l'aile opposée au ballon

### Système de reconnaissance défensive

**Types de défense détectables** :
- **Zone** : Défenseurs couvrent des espaces, pas des joueurs
- **Homme-à-homme** : Chaque défenseur suit un attaquant
- **Pressing** : Pression agressive sur le porteur

**Reconnaissance (début Q2)** :

```csharp
public class TacticalLearningSystem : MonoBehaviour
{
    // Observation patterns adverses
    public float shotDistanceDistribution;  // Tirent de loin vs proche
    public float playThroughPivot;          // % passes par la pointe
    public float shotFrequency;             // Fréquence de tir
    
    // Reconnaissance défense
    public DefenseType detectedDefense;
    public float confidenceLevel;           // 0-1, seuil 0.7 pour adapter
    
    // Déclenché au Q2 (temps d'observation : Q1)
}
```

**Adaptations tactiques selon défense détectée** :

```
Contre ZONE :
├─ Menace le gardien pour attirer défenseur
├─ Cherche les intervalles entre zones
└─ Passe quand défenseur s'engage

Contre PRESSING :
├─ Provocation de faute (simulation)
├─ Passes rapides avant pression
└─ Exploitation fautes pour coups francs

Contre HOMME :
├─ Écrans pour libérer coéquipiers
├─ Traversées pour désorganiser marquages
└─ Isolations 1v1 si avantage physique
```

### Adaptation à l'infériorité numérique

**Réorganisation automatique** :

```
7v7 (normal) :
└─ Tactiques standards (Zone/Homme/Pressing)

6v7 (-1 joueur) :
├─ Transition automatique → ZONE
├─ Formation en arc devant but
├─ 5 joueurs de champ + gardien
└─ Moins d'agressivité (éviter nouvelles exclusions)

5v7 ou moins (-2+ joueurs) :
├─ Formation "WALL" (mur défensif)
├─ Tous les joueurs devant le but
├─ 1 bras levé par joueur (mur de bras)
├─ Objectif : Bloquer visuellement le but
└─ Statique, pas de sorties sur tireurs
```

### Communication entre joueurs

**Système d'appels de ballon** :

```csharp
public enum CallType
{
    RequestBall,    // "Ballon !" - Je suis libre
    ImOpen,         // "Je suis seul !" - Insistance
    Screen,         // "Écran !" - Annonce blocage
    Switch,         // "Change !" - Échange de marquage
    Shot,           // "Tire !" - Encouragement
    Time,           // "Temps !" - Shot clock critique
    Defense         // "Reviens !" - Repli défensif
}
```

**Déclenchement automatique (IA)** :
- Joueur démarqué appelle s'il n'est pas dans le champ de vision du porteur
- Joueur qui va faire un écran l'annonce
- Alerte temps si shot clock < 5s

**Représentation VR** :
- Audio spatialisé 3D (position du joueur qui appelle)
- Indicateur visuel au-dessus de la tête
- Couleur selon urgence (vert → orange → rouge)

---

## Système d'arbitrage

### RefereeSystem - Agent autonome décisionnaire

**L'arbitre est un système indépendant qui** :
- Observe activement le jeu (détecteur actif, pas passif)
- Détecte les fautes en temps réel
- Applique la règle de l'avantage (attend jusqu'à 1.5s si action offensive)
- Contrôle le GameClock
- Décide des sanctions (faute simple, exclusion, penalty)
- Peut se tromper (perception limitée, erreurs paramétrables)

### RefereeProfile (paramétrable)

```csharp
[CreateAssetMenu(fileName = "RefereeProfile", menuName = "WaterPolo/RefereeProfile")]
public class RefereeProfile : ScriptableObject
{
    [Header("Tolerance")]
    [Range(0, 1)] public float strictness = 0.5f;           // 0=permissif, 1=strict
    [Range(0, 1)] public float advantageOrientation = 0.7f; // Laisse jouer
    [Range(0, 1)] public float consistencyLevel = 0.9f;     // Même faute = même décision
    
    [Header("Detection")]
    [Range(0, 1)] public float visionAccuracy = 0.9f;
    [Range(0, 1)] public float twoMeterLineVigilance = 0.8f;
    [Range(0, 1)] public float penaltyThreshold = 0.6f;
    [Range(0, 0.2f)] public float errorRate = 0.05f;        // % fautes ratées/sifflées à tort
    
    [Header("Specific Rules")]
    [Range(0, 1)] public float exclusionTendency = 0.5f;
    [Range(0, 1)] public float handCheckStrictness = 0.7f;  // Vérifie mains levées après faute
    
    [Header("Physical")]
    public float movementSpeed = 2.5f;                      // Vitesse déplacement au bord
    public float anticipationDistance = 1.5f;
}
```

### États de l'arbitre

```
OBSERVING (état par défaut)
├─ Analyse continue
├─ Détecte infractions
└─ Évalue contexte

ADVANTAGE_PENDING (faute détectée, attend issue)
├─ Faute en mémoire
├─ Chronomètre d'avantage actif (~1.5s max)
├─ Observe issue offensive
└─ Décision en suspens

WHISTLING (décision de siffler)
├─ Détermine type de faute
├─ Arrête GameClock
├─ Signale la faute
└─ Transition vers gestion de faute

FOUL_MANAGEMENT (après sifflet)
├─ Positionne les joueurs
├─ Attend que joueur soit prêt
├─ Surveille délais
└─ Signale autorisation de jouer

RESUMING (reprise du jeu)
├─ Surveille exécution
├─ Redémarre GameClock
└─ Retour à OBSERVING
```

### Règle de l'avantage

**Timeline** :
```
t=0s : Faute détectée, tireur en position de tir
├─ Arbitre mémorise faute
├─ Active advantage timer (max 1.5s)
└─ Continue observation

t=0s à t=1.5s : Fenêtre d'observation
├─ Surveille issue (tir + but ?)
├─ Peut changer d'avis si action cassée
└─ Décision dynamique

Issue 1 : But marqué (< 1.5s)
└─ Valide but, ignore faute simple
    (exclusion appliquée quand même si brutale)

Issue 2 : Pas de but OU action cassée
└─ Siffle, applique sanction
```

**Particularités** :
- Certains arbitres plus laxistes (advantageOrientation élevé)
- D'autres sifflent immédiatement (advantageOrientation faible)
- En cas de **brutalité**, peut attendre fin d'action mais exclusion garantie

### Types de fautes

```csharp
public enum FoulType
{
    None,
    Holding,              // Retenir bras/maillot
    Sinking,              // Couler joueur
    Obstruction,          // Obstruction
    Brutality,            // Brutalité (violence)
    BallUnderwater,       // Ballon sous l'eau
    ShotClockViolation,   // Shot clock expiré
    IllegalMovement,      // Mouvement illégal
    GoalkeeperViolation,  // Faute gardien
    TwoMeterViolation     // Joueur dans zone 2m sans ballon
}
```

### Sanctions selon contexte

**Faute sur tireur** (position relative défenseur/tireur) :

```
Défenseur DEVANT le tireur :
├─ Contact normal défensif
└─ Sanction : Faute ordinaire (sauf si violent → exclusion)

Défenseur SUR LE CÔTÉ :
├─ Évaluation sévérité contact
└─ Sanction variable :
    ├─ Contact léger → Faute ordinaire
    ├─ Contact empêche tir → Exclusion
    └─ Contact violent → Exclusion + penalty

Défenseur DERRIÈRE le tireur :
├─ Impossible de jouer ballon
├─ Contact = illégal par nature
└─ Sanction : Exclusion (+ penalty si action cassée)
```

### Comptabilisation et exclusions définitives

```
PlayerMatchRecord :
├─ ordinaryFouls : int (pas de limite)
├─ exclusions : int (20s temporaires)
└─ majorSanctions : int (exclusions + penalties)

Règle d'exclusion définitive :
└─ SI (exclusions + majorSanctions) >= 3
    └─ Joueur exclu définitivement du match
        ├─ Ne peut plus revenir
        └─ Remplaçant entre si disponible
```

### Sanctions graves immédiates

```
Violence (brutality aggravated) :
├─ Coup délibéré, contact tête
└─ Exclusion définitive immédiate (red card)

Rébellion arbitrale (dissent) :
├─ Contester agressivement
├─ Gestes irrespectueux
└─ Exclusion définitive si grave
```

### Vérification "mains levées"

Après une faute, le fautif doit :
- Lever les mains (animation)
- Respecter 1m de distance avec porteur

**Si non-conforme ET handCheckStrictness > seuil** :
→ Exclusion supplémentaire (20s)

### GoalJudges (juges de ligne)

**Deux juges, un par but** :
- BoxCollider trigger sur ligne de but
- Détecte si ballon entièrement passé
- Signale au MainReferee
- MainReferee valide/invalide le but

### Mode arbitre VR (formation)

**Fonctionnalités pédagogiques** :
- Vue à travers les yeux de l'arbitre
- Annotations visuelles (contacts, fautes potentielles)
- Timer d'avantage visible
- Suggestions IA : "Faute détectée, attendre avantage"
- Replay avec analyse des décisions
- Scoring de performance (fautes correctement identifiées)

---

## Gestion du ballon

### BallController - États du ballon

**Le ballon a plusieurs états** :

```csharp
public enum BallState
{
    FREE,              // Ballon libre dans l'eau (physique complète)
    POSSESSED,         // Ballon possédé par un joueur (physique désactivée)
    PASSING,           // En vol lors d'une passe
    SHOOTING,          // En vol lors d'un tir
    BOUNCING           // Rebond sur surface
}
```

**Transitions** :
```
FREE → POSSESSED : Joueur attrape le ballon
POSSESSED → PASSING/SHOOTING : Joueur lance/tire
PASSING/SHOOTING → FREE : Ballon retombe
PASSING → POSSESSED : Réception par coéquipier
```

### Physique selon état

**État FREE** :
- Rigidbody activé
- Collisions avec tout
- Flottaison (buoyancy)
- Résistance de l'eau

**État POSSESSED** :
- Physique désactivée
- Ballon "parented" à la main du joueur
- Position attachée à un bone (IK)
- Suit les mouvements du joueur

**État PASSING/SHOOTING** :
- Trajectoire calculée (peut être assistée)
- Mélange physique réelle + interpolation
- Prédiction pour IA (anticipation réception)

### Détection de possession

**Critères pour prendre possession** :
- Proximité ballon < seuil (ex: 0.3m)
- Main proche du ballon
- Input joueur (VR : grip button) ou intention IA
- Pas déjà possédé par adversaire

### Mécanique de simulation de faute

**Action coordonnée de l'attaquant** :

```
1. Relâcher le ballon légèrement
   └─ Ballon pousse devant (~1m), coule légèrement

2. Plonger la tête simultanément
   └─ Animation tête sous l'eau, vers défenseur

3. Arbitre évalue crédibilité
   ├─ Défenseur proche ? Contact réel ?
   ├─ Timing cohérent ?
   └─ Attribut foulDrawingExpert ?
   
4. Décision arbitre
   ├─ Crédible → Faute sifflée
   ├─ Doute → Dépend strictness
   └─ Évident → Pas de sifflet (voire contre-faute si répété)
```

---

## Intégration VR

### Modes VR

**1. Mode Observateur (Tutorial/Learning)** :
- Joueur VR "possède" un corps de joueur IA
- Voit à travers ses yeux (première personne)
- Voit les bras/mains du joueur IA agir
- L'IA contrôle entièrement
- **Objectif** : Apprendre par observation

**2. Mode Joueur Actif (Gameplay)** :
- Joueur VR contrôle directement un joueur
- Manettes + tracking bras/mains
- IA contrôle les 6 autres coéquipiers
- **Objectif** : Jouer activement

**3. Mode Arbitre (Formation)** :
- Joueur VR voit par les yeux de l'arbitre
- IA suggère décisions (annotations visuelles)
- Humain peut valider/annuler
- **Objectif** : Formation arbitres

### Contrôles VR (Mode Actif)

**Déplacement** :
- Joystick : Direction de nage voulue
- Nage automatique selon attribut swimSpeed
- Option : Gestuelles bras pour accélérations

**Actions ballon** :
- Grip : Attraper ballon (si proche)
- Trigger : Passer/Tirer (selon direction visée)
- Détection geste de lancer (vélocité main)

**Actions tactiques** :
- Bouton A : Appel de ballon
- Bouton B : Commandes tactiques rapides

### Adaptation des coéquipiers IA au joueur VR

**Pour les IA, le joueur VR = un coéquipier comme les autres** :
- Observent sa position/mouvement
- Déduisent ses intentions
- S'adaptent (compensent, créent opportunités)

**Pas de code spécial "si humain alors..."**
→ Interface commune `WaterPoloPlayer`

### Affichage VR

**Mode minimal (par défaut)** :
- Pas de HUD (réalisme)
- OU shot clock discret en périphérie

**Mode complet (sur demande, bouton X)** :
- Panneau 3D dans le monde
- Score, temps, shot clock, exclusions
- Se positionne devant le joueur

**Tableau physique dans le monde** :
- Scoreboard au bord de la piscine
- Visible de tous (réalisme)

---

## Modes de jeu

### Mode Competitive (principal)

**Règles complètes** :
- 4 quarts-temps de 8 minutes
- Shot clock 30s (20s en supériorité)
- Arbitrage complet
- IA tactique avancée
- Équipes de 7 joueurs (+ remplaçants)

### Modes Casual

**1. Passe à 10 (Keep Away)** :
- Objectif : 10 passes sans interception
- Pas de tir, pas de buts
- IA défensive essaie d'intercepter

**2. Target Practice** :
- Buts avec zones (coins = 10pts, centre = 5pts)
- Pas d'adversaires
- Focus technique

**3. Speed Challenge** :
- Course contre la montre
- Récupérer ballon → Nager → Tirer
- Leaderboard

**4. Goalkeeper Training** :
- Joueur = gardien
- IA tire depuis positions variées
- Score basé sur % arrêts

### Mode XR (Home Challenge)

**Concept innovant** :
- Utilise passthrough Meta Quest
- Portes/fenêtres de la maison = buts
- Ballon spawn aléatoire dans la pièce
- Une cible activée (highlight)
- Joueur doit récupérer et tirer dans la bonne

**Variations** :
- Time Attack (max points en 2min)
- Survival (X vies)
- Combo (cibles multiples)

### Architecture des modes

```csharp
public abstract class GameMode : MonoBehaviour
{
    public abstract void Setup();
    public abstract void StartGame();
    public abstract void UpdateGameLogic();
    public abstract bool CheckWinCondition();
    public abstract void OnGoalScored(Goal goal);
}

// Modes héritent et implémentent logique spécifique
public class CompetitiveWaterPoloMode : GameMode { }
public class KeepAwayMode : GameMode { }
public class HomeXRMode : GameMode { }
```

---

## Configuration et paramètres

### Tout est paramétrable (ScriptableObjects)

**Principe fondamental** : Aucune valeur hardcodée dans le code

### TacticalConfiguration

```csharp
[CreateAssetMenu(fileName = "TacticalConfig", menuName = "WaterPolo/Config")]
public class TacticalConfiguration : ScriptableObject
{
    [Header("Temporal Parameters")]
    public float transitionPhaseEnd = 8f;      // Fin phase installation
    public float recognitionPhaseEnd = 12f;    // Fin phase reconnaissance
    public float urgencyPhaseStart = 24f;      // Début phase urgence
    
    [Header("Recognition")]
    public float minConfidenceToAct = 0.7f;
    public float maxProbeActionDuration = 3f;
    
    [Header("Urgency")]
    public float criticalTimeRemaining = 5f;
    public float desperateTimeRemaining = 2f;
}
```

### GameRuleSet

```csharp
[CreateAssetMenu(fileName = "GameRuleSet", menuName = "WaterPolo/RuleSet")]
public class GameRuleSet : ScriptableObject
{
    [Header("Match Format")]
    public int quarterCount = 4;
    public float quarterDuration = 480f;
    
    [Header("Shot Clock")]
    public bool useShotClock = true;
    public float shotClockDuration = 30f;
    public float exclusionShotClock = 20f;
    
    [Header("Fouls & Exclusions")]
    public bool foulDetectionEnabled = true;
    public bool exclusionsEnabled = true;
    public float exclusionDuration = 20f;
    
    [Header("Field Size")]
    public FieldSize fieldSize = FieldSize.Full;
}
```

### PlayStyleProfile (variations comportementales)

```csharp
[CreateAssetMenu(fileName = "PlayStyle", menuName = "WaterPolo/PlayStyle")]
public class PlayStyleProfile : ScriptableObject
{
    [Range(0, 1)] public float recognitionSpeed;  // 0=lent/sûr, 1=rapide/risqué
    [Range(0, 1)] public float aggressiveness;
    [Range(0, 1)] public float patternVariation;
    
    public bool preferEarlyShot;
    public float comfortZoneEnd;  // Quand commence le stress
}
```

### AnimationCurve pour variations non-linéaires

**Exemple : Courbe d'urgence selon temps** :
```csharp
public AnimationCurve urgencyCurve;

// Designer peut créer dans Inspector :
// t=28s → urgency=0.0
// t=15s → urgency=0.2
// t=8s  → urgency=0.5
// t=3s  → urgency=0.9
```

---

## Phases d'implémentation

### Phase 1 : Fondations (MVP)
```
✓ Terrain + eau (surface plane)
✓ Joueurs (squelette basique, pas d'animations complexes)
✓ Ballon (états de base : libre/possédé)
✓ Mouvement basique joueurs (nage simple)
✓ GameClock simple (temps qui défile)
✓ Score basique (incrémente au but)
```

### Phase 2 : Gameplay core
```
- BallController complet (possession, passes, tirs)
- PlayerAI basique (positionnement, actions simples)
- Formations de base (1-2 par équipe)
- RefereeSystem simple (fautes évidentes uniquement)
- VRPlayer fonctionnel (contrôles de base)
- IK skeleton pour détection précise (bras levés, etc.)
```

### Phase 3 : Tactiques
```
- TeamTactics (3 types défense : Zone/Homme/Pressing)
- RoleConfiguration (positionnement tactique avancé)
- Communication (appels de balle, audio spatialisé)
- Adaptations basiques (infériorité numérique → Zone/Wall)
- Shooting detection (orientation, bras armé, distance, timing)
```

### Phase 4 : Profondeur
```
- PlayerAttributes & Traits (variations individuelles)
- TacticalLearningSystem (adaptation Q2, patterns adverses)
- RefereeSystem avancé (avantage, erreurs, contextes)
- Foul mechanics (simulation, grabbing, violence)
- Contact detection précise (niveau eau, forces)
- Exclusions et gestion du banc
```

### Phase 5 : Polish & Features
```
- Multi-plateforme (affichage Console, Mobile)
- Modes alternatifs (Passe à 10, Target Practice, etc.)
- Mode arbitre VR (formation)
- Statistiques avancées
- Système de replay
```

### Phase 6 : Extensions (long terme)
```
- Multijoueur (architecture déjà prête)
- Mode XR (Home Challenge avec passthrough)
- Apprentissage persistant entre matchs
- Éditeur de tactiques avancé
- Campagne / carrière
```

---

## Détails techniques importants

### Détection d'action de tir

**Conditions cumulatives TOUTES requises** :

```csharp
ShootingAction (état joueur) :
├─ 1. Possession du ballon (hasBall = true)
├─ 2. Orientation vers but (angle < 45°)
├─ 3. Bras armé (IK : hauteur main > épaule + seuil)
├─ 4. Distance au but < 5m
└─ 5. Timer < maxDuration (1.5s paramétrable)

SI une condition devient false :
└─ Exit SHOOTING_STANCE immédiatement
```

**Pas de faux positif possible** : Sans possession, pas de shooting state

### Vitesses de nage en waterpolo

**Références réalistes** :
- 50m NL meilleurs joueurs : 26s (départ plongé) = ~1.92 m/s
- En waterpolo match (départ dans l'eau, tête hors de l'eau) : ~1.2-1.5 m/s

**Timeline installation début de match** :
```
t=0s : Engagement milieu terrain
├─ Pointe, Ailiers : 10m à parcourir (~7s)
├─ Demis : 6-7m (~5s)
└─ Défense-pointe : 10m (~7s)

t=6-8s : Installation complète
└─ DÉBUT phase tactique réelle

Temps disponible pour exploitation : 28s - 8s = ~20s
```

### Zone des 2 mètres

**Règle** : Attaquant ne peut entrer dans la zone si ballon absent

**Détection** :
- BoxCollider 2m de profondeur devant chaque but
- Surveillance arbitre : `(attaquant IN zone) AND (ballon OUT zone)` = infraction
- Tolérance temporelle : ~0.5-1s pour sortir si ballon part

### Contact detection (niveau de l'eau)

**Zones** :
- **Au-dessus de l'eau** : Contacts évidents, arbitre voit bien
- **Au niveau / sous l'eau** : Contacts subtils, détection difficile

**Mécaniques** :
- Holding (retenir maillot/bras)
- Sinking (couler joueur)
- Grabbing (attraper poignets)

**Paramètres** :
- Force du contact
- Zone de contact (torse vs sous l'eau)
- Durée du contact (> 1s plus détectable)

---

## Événements système

### Liste complète des événements

```csharp
// Arbitrage
FoulDetected(FoulEvent foul)
AdvantageApplied(FoulEvent originalFoul, string reason)
WhistleBlown(FoulType type, FoulSanction sanction)
SanctionApplied(WaterPoloPlayer player, SanctionType type)
HandCheckViolation(WaterPoloPlayer player)
TwoMeterViolation(WaterPoloPlayer player)
GoalValidated(WaterPoloPlayer scorer)
GoalInvalidated(string reason)

// Match flow
GamePaused(string reason)
GameResumed(GameContext context)
ShotClockExpired()
QuarterEnded(int quarter)
MatchEnded(Team winner)

// Exclusions
ExclusionStarted(ExclusionClock exclusion)
ExclusionEnded(ExclusionClock exclusion)
PlayerReplacementNeeded(WaterPoloPlayer exitingPlayer)
PlayerReplaced(WaterPoloPlayer old, WaterPoloPlayer new)

// Tactiques
TacticalAdaptation(TacticType oldTactic, TacticType newTactic, string reason)
FormationChanged(Formation newFormation, int playerCount)
PatternDetected(TacticalPattern pattern, float confidence)
DefenseTypeRecognized(DefenseType type, float confidence)

// Communication
BallCallMade(WaterPoloPlayer caller, CallType type, WaterPoloPlayer target)
ShootingStanceEntered(WaterPoloPlayer shooter)
ShootingStanceExited(WaterPoloPlayer shooter)

// Ballon
BallPossessionChanged(WaterPoloPlayer oldOwner, WaterPoloPlayer newOwner)
BallStateChanged(BallState oldState, BallState newState)
ShotAttempted(WaterPoloPlayer shooter, Vector3 target)
PassAttempted(WaterPoloPlayer passer, WaterPoloPlayer receiver)
```

---

## Glossary (Termes waterpolo)

**Pointe** : Joueur pivot devant le but adverse (comme un "9" au football)
**Ailiers** : Joueurs sur les côtés (gauche/droite)
**Demis** : Joueurs au milieu du terrain
**Défense-pointe** : Défenseur qui marque la pointe
**Traversée** : Mouvement latéral rapide pour changer de côté
**Écran** : Blocage d'un défenseur pour libérer un coéquipier
**Pressing** : Défense agressive qui presse le porteur
**Zone** : Défense où chacun couvre un espace, pas un joueur
**Homme-à-homme** : Défense où chaque défenseur marque un attaquant spécifique
**Coup franc** : Reprise de jeu après faute ordinaire
**Pénalty** : Tir à 5m, seul face au gardien (faute grave sur tireur)
**Exclusion** : Joueur sort 20s (ou définitivement si 3ème)
**Shot clock** : 30 secondes pour tirer (20s en supériorité)
**Ligne des 2m** : Zone devant le but, interdite sans ballon
**Avantage** : Arbitre laisse jouer malgré faute si action offensive favorable
**Corner** : Coup franc depuis la marque des 2m (ballon sorti par défense)

---

## Notes d'implémentation Unity

### Structure de dossiers recommandée

```
Assets/
├─ WaterPolo/
│  ├─ Core/
│  │  ├─ GameClock.cs
│  │  ├─ ScoreTable.cs
│  │  ├─ MatchState.cs
│  │  └─ EventBus.cs
│  │
│  ├─ Players/
│  │  ├─ WaterPoloPlayer.cs (abstract)
│  │  ├─ AIPlayer.cs
│  │  ├─ VRPlayer.cs
│  │  ├─ ObservedAIPlayer.cs
│  │  └─ PlayerAttributes.cs (ScriptableObject)
│  │
│  ├─ AI/
│  │  ├─ DecisionMaker.cs
│  │  ├─ ActionEvaluator.cs
│  │  ├─ TacticalLearningSystem.cs
│  │  └─ CoachAI.cs
│  │
│  ├─ Tactics/
│  │  ├─ TeamTactics.cs
│  │  ├─ WaterPoloFormation.cs (ScriptableObject)
│  │  ├─ RoleConfiguration.cs
│  │  └─ PositioningRules/
│  │
│  ├─ Referee/
│  │  ├─ RefereeSystem.cs
│  │  ├─ RefereeProfile.cs (ScriptableObject)
│  │  ├─ FoulDetection.cs
│  │  └─ GoalJudge.cs
│  │
│  ├─ Ball/
│  │  ├─ BallController.cs
│  │  └─ BallPhysics.cs
│  │
│  ├─ GameModes/
│  │  ├─ GameMode.cs (abstract)
│  │  ├─ CompetitiveMode.cs
│  │  ├─ CasualModes/
│  │  └─ XRModes/
│  │
│  ├─ UI/
│  │  ├─ VRDisplay/
│  │  ├─ ConsoleUI/
│  │  └─ Scoreboard/
│  │
│  └─ Configuration/
│     ├─ TacticalConfiguration.cs (ScriptableObject)
│     ├─ GameRuleSet.cs (ScriptableObject)
│     └─ PlayStyleProfile.cs (ScriptableObject)
```

### Dépendances externes

**Obligatoires** :
- Meta XR SDK (pour Quest)
- TextMeshPro (UI)

**Recommandées** :
- DOTween (animations fluides)
- Cinemachine (caméras replay/spectateur)

**Optionnelles** :
- ML-Agents (apprentissage machine futur)
- Photon/Mirror (multijoueur futur)

### Performance considerations

**Optimisations VR** :
- Target 72 FPS minimum (Quest 2/3)
- LOD sur joueurs lointains
- Occlusion culling (joueurs sous l'eau)
- Baked lighting autant que possible
- Avoid complex shaders (eau simple mais belle)

**IA** :
- Utility AI evaluation : 1x par frame max par joueur
- Tactical learning : Update chaque seconde, pas chaque frame
- Pathfinding : A* simplifié (2D dans l'eau)

---

## Checklist de validation architecture

### ✅ Complétude

- [x] Tous les rôles définis (joueurs, arbitre, coach)
- [x] Tous les états de jeu couverts (normal, faute, exclusion, but)
- [x] Toutes les mécaniques clés (tir, passe, faute, simulation)
- [x] Systèmes tactiques complets (formation, adaptation, apprentissage)
- [x] Multi-plateformes prévu (VR, Console, Mobile)
- [x] Extensibilité (multijoueur, modes alternatifs)

### ✅ Modularité

- [x] Systèmes découplés (Event Bus)
- [x] Interfaces abstraites (WaterPoloPlayer, GameMode, PlaySpace, Goal)
- [x] Paramétrage via ScriptableObjects
- [x] Pas de hardcoding de valeurs

### ✅ Testabilité

- [x] Chaque système testable isolément
- [x] Mock events possible
- [x] Debug visualization prévue
- [x] Replay système envisagé

### ✅ Extensibilité future

- [x] Nouveaux modes = nouvelles classes, pas refonte
- [x] Nouveaux types de joueurs (Remote) supportés
- [x] Nouvelles plateformes = nouveaux adapters display
- [x] Apprentissage ML peut remplacer utility AI sans casser architecture

---

## Contacts et ressources

### Documentation officielle waterpolo

- FINA (règles internationales)
- LEN (règles européennes)
- Vidéos de matchs professionnels pour référence

### Ressources Unity

- Meta XR SDK documentation
- Unity Physics best practices
- VR performance optimization guides

### TODO / Questions ouvertes

- [ ] Persistance entre matchs (stats, apprentissage) - long terme
- [ ] Système de progression (carrière) - long terme
- [ ] Éditeur tactique UI - moyen terme
- [ ] Apprentissage machine (remplacer utility AI) - très long terme
- [ ] Choix réseau multijoueur (Photon vs Mirror vs autre) - long terme

---

## Version et changelog

**Version** : 1.0 - Architecture initiale
**Date** : 2025-01-16
**Auteur** : Architecture définie en collaboration avec Claude (Anthropic)

**Changelog** :
- 1.0 (2025-01-16) : Document initial complet
  - Architecture globale
  - Tous les systèmes définis
  - Phases d'implémentation
  - Configuration complète

---

*Ce document est un guide architectural vivant. Il sera mis à jour au fil de l'implémentation et des découvertes.*
