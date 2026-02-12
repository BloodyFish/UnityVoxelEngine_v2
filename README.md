# Unity Voxel Engine v2
<img width="2108" height="1191" alt="Screenshot 2026-02-11 180337" src="https://github.com/user-attachments/assets/7807a9dd-5a12-47fa-80a8-f1d9213d9541" />

A voxel engine prototype built in **Unity** using a hybrid **C# and C++ architecture**. The project demonstrates integration between Unity and a native DLL for voxel-related processing.

---

## Overview

Unity Voxel Engine v2 is an experimental voxel engine that separates responsibilities between Unity scripts and native C++ code. Unity handles project structure, scripting, and engine integration, while performance-sensitive logic is implemented in a compiled native library.

This repository serves as a technical demonstration of Unity-to-native code interoperability and voxel engine experimentation.

---

## Current Features

- Unity project configured for voxel engine development  
- Native **C++ DLL integration** (`VoxelEngine_v2.dll`)  
- Supporting C++ source project included  
- Organized Unity project structure  
- Functional foundation for voxel engine experimentation  

---

## Project Structure

```
UnityVoxelEngine_v2
│
├── Assets/                # Unity scripts, scenes, and resources
├── Packages/              # Unity package dependencies
├── ProjectSettings/       # Unity configuration files
├── VoxelEngine_v2/        # Native C++ source project
├── VoxelEngine_v2.dll     # Compiled native library used by Unity
├── README.md
├── LICENSE
├── .gitignore
└── .vsconfig
```

---

## Architecture

### Unity (C#)

Responsible for:

- Engine integration  
- Project management  
- Script execution  
- Communication with the native DLL
- Chunk **mesh** generation 

### Native Library (C++)

Responsible for:

- Voxel-related processing implemented outside of Unity  
- Performance-focused logic compiled into a DLL
- Chunk **noise** generation

Unity scripts interface with the native library to execute voxel processing tasks.

---

## Requirements

- Unity (Version compatible with project settings)  
- Visual Studio or another C# development environment  
- Visual Studio with C++ workload (if modifying the native project)  

---

## Setup

### Clone Repository

```bash
git clone https://github.com/BloodyFish/UnityVoxelEngine_v2.git
```

### Open Project

1. Open **Unity Hub**  
2. Click **Add Project**  
3. Select the cloned folder  
4. Open the project  

---

## Native DLL

The project includes a prebuilt native library:

```
VoxelEngine_v2.dll
```

If you modify the C++ source project, rebuild the DLL and replace the existing file inside the Unity project directory.

---

## Purpose of the Project

This project is intended as:

- A voxel engine prototype  
- A demonstration of Unity and native C++ interoperability  
- A foundation for voxel engine experimentation and research  

---

## License

This project is licensed under the **MIT License with Commons Clause**.

### Summary

You are free to:

- Use this engine in personal or commercial games  
- Modify and extend the engine  
- Distribute games built using this engine  
- Share modified versions of the engine with attribution  

You may NOT:

- Sell or redistribute the engine itself as a standalone product  
- Repackage and publish the engine with minimal or no changes for profit  

See the [LICENSE](LICENSE) file for full legal terms.

---

## Author

BloodyFish
