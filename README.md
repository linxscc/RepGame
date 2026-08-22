# RepGame - Unity Multiplayer Card Game

**RepGame** is a sophisticated **real-time multiplayer card battle game** built in Unity with C#. This is a complete, production-ready implementation featuring TCP-based networking, card-based combat mechanics, and a comprehensive UI system.

## 🎮 Core Game Features

### Game Mechanics
- **Turn-based card combat** with real-time networking
- **Card composition system** - combine 3 cards of the same type/level to create 1 higher-level card
- **Bond system** - special card synergies that provide damage bonuses when specific card combinations are played
- **Health management** - players start with 100 HP, battle until one player's health reaches zero
- **Automatic card play** - if a player doesn't act within the time limit, cards are played automatically

### 🃏 Card System
- **Multi-level card hierarchy** (levels 1-5+)
- **Card types with damage values**
- **Unique card identifiers (UID)** for precise tracking
- **Target system** - cards can evolve into higher-level variants
- **Addressable asset system** for dynamic card loading

### 🔗 Bond System
- **Strategic card combinations** that unlock special abilities
- **Damage multipliers** when specific card sets are played together
- **Visual bond indicators** showing available and active bonds
- **Hierarchical bond activation** - higher-level bonds take priority

## 🏗️ Technical Architecture

### Networking
- **Dual networking implementation:**
  - **TCP Client** (`GameTcpClient`) - primary game communication
  - **LiteNetLib** - alternative networking solution for specific features
- **Real-time server communication** with connection management and auto-reconnection
- **Message serialization** using JSON with API response wrapping
- **Multi-threaded request processing** on the server side

### System Architecture
- **Event-driven architecture** using `EventManager` for loose coupling
- **GameStateManager** - centralized game logic without UI dependencies
- **Separation of concerns** - UI, networking, and game logic are cleanly separated
- **Object pooling** for efficient card instantiation
- **Component caching** for optimized UI performance

### 🎨 UI System
- **Multi-panel interface:**
  - Server connection panel
  - Login/start game panel
  - Main game panel with card management
  - Game over panel
- **Dynamic card UI** with selection states and visual feedback
- **Health bars** with smooth animated transitions (DOTween)
- **Bond display system** with tooltips and activation states
- **Message system** for player notifications

## 🔧 Advanced Features

### Development Tools
- **Automated build systems** with custom editor tools
- **Card prefab generator** - automatically creates UI prefabs from images
- **Animator controller generator** - creates animation controllers from JSON configuration
- **FBX animation clip exporter** - converts FBX files to animation clips
- **URP shader converter** - automatically converts materials to URP

### Asset Management
- **Addressable Assets** for dynamic content loading
- **Resource optimization** with texture compression and sprite atlasing
- **Modular asset organization** with clear folder structure
- **Configuration-driven systems** (cards, bonds, animations)

### Input System
- **New Unity Input System** integration
- **Multi-platform input support** (Keyboard, Mouse, Gamepad, Touch, XR)
- **Action-based input mapping** for extensible controls

## 🖥️ Server-Side Implementation

### Game Server
- **LiteNetLib-based** multiplayer server
- **Room-based matchmaking** - automatically pairs players
- **Turn management** with timeout handling
- **Card validation** - server-side verification of all card plays
- **Damage calculation** with bond processing
- **Game state synchronization** between clients

### Game Logic
- **CardManager** - handles card distribution, validation, and composition
- **DamageCalculator** - processes damage with bond bonuses
- **HandleNetworkRequest** - manages all client-server communication
- **Room management** - automatic cleanup and player tracking

## 📚 Code Quality & Architecture

### Design Patterns
- **Singleton pattern** for managers (BondManager, GameStateManager)
- **Observer pattern** through EventManager
- **Factory pattern** for card instantiation
- **Strategy pattern** for different card types and bonds
- **Command pattern** for network message handling

### Error Handling
- **Comprehensive try-catch blocks** throughout the codebase
- **Network disconnection recovery** with automatic reconnection
- **Input validation** on both client and server
- **Graceful degradation** when assets fail to load

### Performance Optimization
- **Object pooling** for frequently created/destroyed objects
- **Component caching** to avoid repeated FindComponent calls
- **Batch processing** for network messages
- **Efficient data structures** (HashSets, Dictionaries) for fast lookups

## 📁 Project Structure

```
📁 Assets/
├── 📁 Scripts/
│   ├── 📁 GameLogic/         # Core game systems
│   ├── 📁 Network/           # Networking implementation
│   ├── 📁 UI/               # User interface components
│   ├── 📁 Models/           # Data models and structures
│   ├── 📁 Core/             # Utility systems and managers
│   ├── 📁 Editor/           # Development tools
│   └── 📁 Animation/        # Animation configurations
├── 📁 Prefabs/             # Game object prefabs
├── 📁 Scenes/              # Unity scenes
├── 📁 StreamingAssets/     # Configuration files
└── 📁 Resources/           # Runtime-loaded assets
```

## 🛠️ Technologies Used

- **Unity 2022.3+** - Game engine
- **C# .NET** - Programming language
- **LiteNetLib** - Networking library
- **DOTween** - Animation system
- **TextMeshPro** - Text rendering
- **Unity Addressables** - Asset management
- **Unity Input System** - Input handling
- **JSON** - Data serialization

## 🚀 Build & Deployment

- **Multi-platform builds** ready for Windows standalone
- **Automated build pipeline** with custom editor tools
- **Development and release configurations**
- **Asset optimization** for different platforms

## ⭐ Key Strengths

1. **Professional Architecture** - Clean separation of concerns with maintainable code
2. **Robust Networking** - Handles disconnections, timeouts, and edge cases
3. **Scalable Design** - Event-driven architecture allows easy feature additions
4. **Performance Optimized** - Efficient memory usage and smooth gameplay
5. **Developer Tools** - Comprehensive automation for content creation
6. **Production Ready** - Error handling, logging, and debugging features

## 🔧 Development Notes

**Note:** This is a trial project demonstrating automated development processes including model creation, animation generation, network implementation, and scene generation.

### Network Service Setup
When using Network Service in local test, you may need these commands:
```cmd
net stop winnat
net start winnat
```

### Missing Assets
Some prefabs are not included in the repository. Importing directly may cause missing reference errors.

### Automation Features Implemented
- Automatically generate animator controller
- Automatically generate UI prefabs
- Automatically convert URP shader
- Automatically convert fbx to animation clip

### Resources
- [Color Palette Reference](https://peiseka.com/index-index-peise-id-2244.html)

---

This project demonstrates advanced Unity development skills, network programming expertise, and professional game development practices. It serves as a complete multiplayer card game foundation suitable for commercial development or as a portfolio showcase of full-stack game development capabilities.
