<div align="center">

<p align="center">
  <img src="docs/assets/portfolio-mark.svg" alt="🤖 Interactive NPC System for Unity logo" width="96" height="96">
</p>

<h1 align="center">🤖 Interactive NPC System for Unity</h1>
**A Modular, AI-Driven Framework for Next-Generation NPCs**

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

</div>

The **Interactive NPC System** is a powerful, highly extensible framework designed to bring your Non-Player Characters to life. By integrating Large Language Models (LLMs), dynamic memory banks, personality traits, and Text-to-Speech (TTS), your NPCs can engage in natural, unscripted conversations that evolve based on player interactions.

---



<p align="center"><a href="#key-features">✨ Key Features</a> &nbsp;·&nbsp; <a href="#quick-start-demo-scene">🚀 Quick Start (Demo Scene)</a> &nbsp;·&nbsp; <a href="#system-architecture">🏗️ System Architecture</a> &nbsp;·&nbsp; <a href="#creating-custom-npcs-manually">🔧 Creating Custom NPCs Manually</a></p>

## ✨ Key Features

* 🧠 **LLM Agnostic Integration**  
  Built-in support for cloud providers (**OpenAI**) and local execution (**Ollama**). Easily swap models without touching the core NPC logic.
* 🎭 **Dynamic Personalities & Moods**  
  Configure NPCs using the Big Five personality traits. Moods dynamically shift during conversations based on sentiment analysis of the player's input, affecting both text responses and animations.
* 📚 **Memory & Relationship System**  
  NPCs categorize memories into short-term and long-term storage. They remember past interactions, track trust and affection, and recall relevant context dynamically using semantic memory retrieval.
* 🗣️ **Voice & Lip Sync**  
  Integrated **ElevenLabs** TTS support. Includes real-time viseme processing for accurate 3D lip-synchronization with Unity's Animator.
* 💬 **Sleek Conversation UI**  
  A modern, customizable chat interface inspired by modern messaging apps, fully integrated with Unity's Event System.
* 🛠️ **Developer-Friendly Editor Tools**  
  One-click setup wizards, custom ScriptableObject inspectors, and runtime Memory Debugging tools make configuring NPCs effortless.

---

## 🚀 Quick Start (Demo Scene)

The easiest way to understand the system is to generate the automated Demo Scene.

1. Open your Unity project.
2. Navigate to the top menu: `Tools > Interactive NPC > Create Demo Scene`.
3. The automated script will generate a new scene (`Assets/Scenes/DemoScene.unity`) complete with a Player, an NPC, and a fully wired UI Canvas.
4. **Configure your LLM:**
   * Go to `Assets/Data/DemoNPC/Demo_LLM.asset`.
   * **For Ollama (Local/Free):** Set *Provider Type* to `Ollama`. Ensure your local Ollama server is running (e.g., `ollama run llama3.2`).
   * **For OpenAI:** Set *Provider Type* to `OpenAI` and paste your API Key.
5. Hit **Play**. Walk up to the purple NPC using `W, A, S, D` and press `E` to interact!

---

## 🏗️ System Architecture

The framework is built using solid SOLID principles and Dependency Injection, split into several distinct namespaces:

* `InteractiveNPC.Core`: Handles global event routing and Service Locator patterns.
* `InteractiveNPC.LLM`: Contains the Prompt Builder, request/response formatting, and Provider implementations (`OpenAIProvider`, `OllamaProvider`).
* `InteractiveNPC.Memory`: Manages the `MemoryBank` and `RelationshipTracker`. Handles memory persistence and context retrieval.
* `InteractiveNPC.Personality`: Controls `MoodController` and `ResponseModifierService` to alter prompts based on emotional states.
* `InteractiveNPC.Speech`: Interfaces for STT (Speech-to-Text) and TTS (Text-to-Speech), including Viseme data handling.
* `InteractiveNPC.NPC`: The bridge layer. Contains the `NPCInteractionController` MonoBehavior which glues all systems together on a specific GameObject.

---

## 🔧 Creating Custom NPCs Manually

To set up an NPC in your own scene:

1. **Create Data Assets:** Use `Tools > Interactive NPC > NPC Setup Wizard` to generate the foundational ScriptableObjects (Configuration, Personality, Memory).
2. **Setup the GameObject:**
   * Add an `NPCInteractionController`.
   * Add a `PlayerInteractionTrigger` (requires a SphereCollider set to *Is Trigger*).
   * Assign the generated ScriptableObjects to the `NPCInteractionController`.
3. **Connect UI:** Add the `ConversationUI` prefab to your scene and link it to the NPC's `PlayerInteractionTrigger` script.

---

## 📦 Dependencies

This project relies on a few standard Unity packages to function perfectly:
* **Universal Render Pipeline (URP)** - *For default materials and lighting.*
* **TextMeshPro** - *For crisp, scalable UI text.*
* **DOTween** - *For smooth UI transitions and procedural head tracking animations.*
* **UniTask** - *For efficient, allocation-free async/await task handling.*
* **Odin Inspector** *(Optional but recommended)* - *For enhanced editor scripting.*

---

## 🤝 Contributing

Contributions are welcome! If you'd like to add a new LLM provider (e.g., Anthropic, Gemini) or a new TTS engine (e.g., Azure, Coqui), simply:
1. Fork the repository.
2. Implement the `ILLMProvider` or `ITextToSpeech` interface.
3. Submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.
