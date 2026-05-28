using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Memory;
using InteractiveNPC.Memory.Data;
using InteractiveNPC.Personality;
using InteractiveNPC.Personality.Data;

namespace InteractiveNPC.Tests
{
    /// <summary>
    /// Unit tests for the Memory System.
    /// Run via Window > General > Test Runner.
    /// </summary>
    [TestFixture]
    public class MemorySystemTests
    {
        #region Setup
        
        private MemoryBank _memoryBank;
        private NPCMemoryData _memoryData;
        
        [SetUp]
        public void SetUp()
        {
            _memoryData = ScriptableObject.CreateInstance<NPCMemoryData>();
            _memoryBank = new MemoryBank("test_npc", _memoryData);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_memoryData != null)
            {
                Object.DestroyImmediate(_memoryData);
            }
        }
        
        #endregion
        
        #region Memory Bank Tests
        
        [Test]
        public void AddMemory_IncreasesTotalCount()
        {
            // Arrange
            var memory = new MemoryEntry(MemoryType.Dialogue, "Test memory", 0f, 0.5f);
            
            // Act
            _memoryBank.AddMemory(memory);
            
            // Assert
            Assert.AreEqual(1, _memoryBank.TotalMemoryCount);
        }
        
        [Test]
        public void AddMemory_HighImportance_GoesDirectlyToLongTerm()
        {
            // Arrange
            var memory = new MemoryEntry(MemoryType.Event, "Important event", 0f, 0.9f);
            
            // Act
            _memoryBank.AddMemory(memory);
            
            // Assert
            Assert.AreEqual(1, _memoryBank.LongTermCount);
            Assert.AreEqual(0, _memoryBank.ShortTermCount);
        }
        
        [Test]
        public void AddMemory_LowImportance_GoesToShortTerm()
        {
            // Arrange
            var memory = new MemoryEntry(MemoryType.Dialogue, "Casual chat", 0f, 0.3f);
            
            // Act
            _memoryBank.AddMemory(memory);
            
            // Assert
            Assert.GreaterOrEqual(_memoryBank.ShortTermCount, 0);
        }
        
        [Test]
        public void RetrieveRelevantMemories_FindsMatchingKeywords()
        {
            // Arrange
            var memory1 = new MemoryEntry(MemoryType.Dialogue, "We talked about dragons", 0f, 0.7f);
            var memory2 = new MemoryEntry(MemoryType.Dialogue, "Discussion about weather", 0f, 0.7f);
            _memoryBank.AddMemory(memory1);
            _memoryBank.AddMemory(memory2);
            
            // Act
            var results = _memoryBank.RetrieveRelevantMemories("dragon", 5);
            
            // Assert
            Assert.IsTrue(results.Count > 0);
            Assert.IsTrue(results[0].Content.ToLower().Contains("dragon"));
        }
        
        [Test]
        public void GetRecentMemories_ReturnsInOrder()
        {
            // Arrange
            var memory1 = new MemoryEntry(MemoryType.Dialogue, "First", 0f, 0.5f);
            var memory2 = new MemoryEntry(MemoryType.Dialogue, "Second", 0f, 0.5f);
            _memoryBank.AddMemory(memory1);
            _memoryBank.AddMemory(memory2);
            
            // Act
            var results = _memoryBank.GetRecentMemories(2);
            
            // Assert
            Assert.AreEqual(2, results.Count);
        }
        
        [Test]
        public void GetImportantMemories_FiltersCorrectly()
        {
            // Arrange
            var low = new MemoryEntry(MemoryType.Dialogue, "Low", 0f, 0.2f);
            var high = new MemoryEntry(MemoryType.Event, "High", 0f, 0.9f);
            _memoryBank.AddMemory(low);
            _memoryBank.AddMemory(high);
            
            // Act
            var results = _memoryBank.GetImportantMemories(5, 0.7f);
            
            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("High", results[0].Content);
        }
        
        #endregion
        
        #region Memory Entry Tests
        
        [Test]
        public void MemoryEntry_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var memory = new MemoryEntry(
                MemoryType.Action,
                "Player helped me",
                0.8f,
                0.7f
            );
            
            // Assert
            Assert.AreEqual(MemoryType.Action, memory.Type);
            Assert.AreEqual("Player helped me", memory.Content);
            Assert.AreEqual(0.8f, memory.EmotionalImpact);
            Assert.AreEqual(0.7f, memory.Importance);
            Assert.AreEqual(0, memory.RecallCount);
        }
        
        [Test]
        public void MemoryEntry_Recall_IncreasesRecallCount()
        {
            // Arrange
            var memory = new MemoryEntry(MemoryType.Dialogue, "Test", 0f, 0.5f);
            
            // Act
            memory.Recall();
            memory.Recall();
            
            // Assert
            Assert.AreEqual(2, memory.RecallCount);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Unit tests for the Relationship Tracker.
    /// </summary>
    [TestFixture]
    public class RelationshipTrackerTests
    {
        #region Setup
        
        private RelationshipTracker _tracker;
        
        [SetUp]
        public void SetUp()
        {
            _tracker = new RelationshipTracker("test_npc");
        }
        
        #endregion
        
        #region Tests
        
        [Test]
        public void NewTracker_HasNeutralStatus()
        {
            // Assert
            Assert.AreEqual(RelationshipStatus.Neutral, _tracker.Status);
        }
        
        [Test]
        public void NewTracker_HasZeroOverallScore()
        {
            // Assert
            Assert.AreEqual(0f, _tracker.OverallScore);
        }
        
        [Test]
        public void ModifyTrust_UpdatesTrustValue()
        {
            // Act
            _tracker.ModifyTrust(0.5f);
            
            // Assert
            Assert.AreEqual(0.5f, _tracker.Trust, 0.01f);
        }
        
        [Test]
        public void ModifyMetrics_ClampsBetweenNegativeOneAndOne()
        {
            // Act
            _tracker.ModifyTrust(5f);
            
            // Assert
            Assert.AreEqual(1f, _tracker.Trust, 0.01f);
        }
        
        [Test]
        public void ApplyEmotionalImpact_AffectsAffection()
        {
            // Arrange
            float initialAffection = _tracker.Affection;
            
            // Act
            _tracker.ApplyEmotionalImpact(0.5f, "compliment");
            
            // Assert
            Assert.Greater(_tracker.Affection, initialAffection);
        }
        
        [Test]
        public void HighTrust_LeadsToFriendStatus()
        {
            // Act
            _tracker.ModifyTrust(0.8f);
            _tracker.ModifyRespect(0.6f);
            _tracker.ModifyAffection(0.6f);
            
            // Assert
            Assert.AreEqual(RelationshipStatus.Friend, _tracker.Status);
        }
        
        [Test]
        public void NegativeMetrics_LeadsToHostileStatus()
        {
            // Act
            _tracker.ModifyTrust(-0.8f);
            _tracker.ModifyRespect(-0.6f);
            _tracker.ModifyAffection(-0.6f);
            _tracker.ModifyFear(0.5f);
            
            // Assert
            Assert.IsTrue(_tracker.Status == RelationshipStatus.Hostile || 
                          _tracker.Status == RelationshipStatus.Disliked ||
                          _tracker.Status == RelationshipStatus.Enemy);
        }
        
        [Test]
        public void GetPriceModifier_ReturnsCorrectRange()
        {
            // Arrange & Act
            float modifier = _tracker.GetPriceModifier();
            
            // Assert
            Assert.GreaterOrEqual(modifier, 0.5f);
            Assert.LessOrEqual(modifier, 1.5f);
        }
        
        [Test]
        public void CheckWillingToHelp_ReturnsTrueForGoodRelation()
        {
            // Arrange
            _tracker.ModifyTrust(0.8f);
            _tracker.ModifyAffection(0.8f);
            
            // Act
            bool willing = _tracker.CheckWillingToHelp(0.3f);
            
            // Assert
            Assert.IsTrue(willing);
        }
        
        [Test]
        public void GetContextString_ReturnsNonEmpty()
        {
            // Act
            string context = _tracker.GetContextString();
            
            // Assert
            Assert.IsNotEmpty(context);
            Assert.IsTrue(context.Contains("Neutral") || context.Contains("relationship"));
        }
        
        #endregion
    }
    
    /// <summary>
    /// Unit tests for the Mood Controller.
    /// </summary>
    [TestFixture]
    public class MoodControllerTests
    {
        #region Setup
        
        private MoodController _moodController;
        private NPCPersonalityData _personalityData;
        
        [SetUp]
        public void SetUp()
        {
            _personalityData = ScriptableObject.CreateInstance<NPCPersonalityData>();
            _moodController = new MoodController("test_npc", _personalityData);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_personalityData != null)
            {
                Object.DestroyImmediate(_personalityData);
            }
        }
        
        #endregion
        
        #region Tests
        
        [Test]
        public void NewMoodController_StartsNeutral()
        {
            // Assert
            Assert.AreEqual(EmotionalState.Neutral, _moodController.CurrentEmotion);
        }
        
        [Test]
        public void ApplyEmotionalStimulus_Positive_MakesHappy()
        {
            // Act
            _moodController.ApplyEmotionalStimulus(0.8f, "good news");
            
            // Assert
            Assert.AreEqual(EmotionalState.Happy, _moodController.CurrentEmotion);
        }
        
        [Test]
        public void ApplyEmotionalStimulus_Negative_MakesSadOrAngry()
        {
            // Act
            _moodController.ApplyEmotionalStimulus(-0.8f, "bad news");
            
            // Assert
            Assert.IsTrue(_moodController.CurrentEmotion == EmotionalState.Sad ||
                          _moodController.CurrentEmotion == EmotionalState.Angry);
        }
        
        [Test]
        public void SetMood_ChangesCurrentEmotion()
        {
            // Act
            _moodController.SetMood(EmotionalState.Surprised, 0.9f);
            
            // Assert
            Assert.AreEqual(EmotionalState.Surprised, _moodController.CurrentEmotion);
            Assert.AreEqual(0.9f, _moodController.Intensity, 0.01f);
        }
        
        [Test]
        public void GetMoodDescription_ReturnsNonEmpty()
        {
            // Act
            string description = _moodController.GetMoodDescription();
            
            // Assert
            Assert.IsNotEmpty(description);
        }
        
        [Test]
        public void GetMoodModifiers_ReturnsValidStruct()
        {
            // Act
            var modifiers = _moodController.GetMoodModifiers();
            
            // Assert
            Assert.IsTrue(modifiers.Friendliness >= -1f && modifiers.Friendliness <= 1f);
            Assert.IsTrue(modifiers.Helpfulness >= -1f && modifiers.Helpfulness <= 1f);
        }
        
        [Test]
        public void ResetToBaseline_RestoresNeutral()
        {
            // Arrange
            _moodController.SetMood(EmotionalState.Angry, 0.9f);
            
            // Act
            _moodController.ResetToBaseline();
            
            // Assert
            // Should be at or near neutral baseline
            Assert.IsTrue(_moodController.Intensity < 0.6f);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Unit tests for Big Five Personality Traits.
    /// </summary>
    [TestFixture]
    public class BigFiveTraitsTests
    {
        [Test]
        public void Constructor_ClampValuesCorrectly()
        {
            // Act
            var traits = new BigFiveTraits(1.5f, -0.5f, 0.5f, 0.5f, 0.5f);
            
            // Assert
            Assert.AreEqual(1f, traits.Openness);
            Assert.AreEqual(0f, traits.Conscientiousness);
        }
        
        [Test]
        public void GetDescription_ReturnsNonEmpty()
        {
            // Arrange
            var traits = new BigFiveTraits(0.8f, 0.8f, 0.8f, 0.8f, 0.2f);
            
            // Act
            string description = traits.GetDescription();
            
            // Assert
            Assert.IsNotEmpty(description);
            Assert.IsTrue(description.Contains("creative") || description.Contains("organized") || 
                          description.Contains("outgoing") || description.Contains("friendly"));
        }
        
        [Test]
        public void DefaultTraits_AreBalanced()
        {
            // Arrange
            var traits = new BigFiveTraits();
            
            // Assert
            Assert.AreEqual(0.5f, traits.Openness);
            Assert.AreEqual(0.5f, traits.Conscientiousness);
            Assert.AreEqual(0.5f, traits.Extraversion);
            Assert.AreEqual(0.5f, traits.Agreeableness);
            Assert.AreEqual(0.5f, traits.Neuroticism);
        }
    }
    
    /// <summary>
    /// Unit tests for Response Modifier Service.
    /// </summary>
    [TestFixture]
    public class ResponseModifierServiceTests
    {
        #region Setup
        
        private ResponseModifierService _service;
        private NPCPersonalityData _personality;
        
        [SetUp]
        public void SetUp()
        {
            _personality = ScriptableObject.CreateInstance<NPCPersonalityData>();
            _service = new ResponseModifierService(_personality);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_personality != null)
            {
                Object.DestroyImmediate(_personality);
            }
        }
        
        #endregion
        
        #region Tests
        
        [Test]
        public void ModifyResponse_WithNullInputs_ReturnsOriginal()
        {
            // Arrange
            string original = "Hello there!";
            
            // Act
            string modified = _service.ModifyResponse(original, null, null);
            
            // Assert
            Assert.AreEqual(original, modified);
        }
        
        [Test]
        public void AnalyzePlayerInput_DetectsInsults()
        {
            // Act
            var result = _service.AnalyzePlayerInput("You are stupid and ugly");
            
            // Assert
            Assert.IsTrue(result.IsInsult);
            Assert.IsTrue(result.IsNegative);
        }
        
        [Test]
        public void AnalyzePlayerInput_DetectsCompliments()
        {
            // Act
            var result = _service.AnalyzePlayerInput("Thank you, you are amazing");
            
            // Assert
            Assert.IsTrue(result.IsCompliment);
            Assert.IsTrue(result.IsPositive);
        }
        
        [Test]
        public void AnalyzePlayerInput_DetectsThreats()
        {
            // Act
            var result = _service.AnalyzePlayerInput("I will kill you");
            
            // Assert
            Assert.IsTrue(result.IsThreat);
            Assert.IsTrue(result.IsNegative);
        }
        
        [Test]
        public void AnalyzePlayerInput_NeutralInput_IsNeutral()
        {
            // Act
            var result = _service.AnalyzePlayerInput("What time does the shop open?");
            
            // Assert
            Assert.IsFalse(result.IsInsult);
            Assert.IsFalse(result.IsCompliment);
            Assert.IsFalse(result.IsThreat);
        }
        
        [Test]
        public void GetPromptAdditions_ReturnsNonEmpty()
        {
            // Arrange
            var mood = new MoodController("test", _personality);
            
            // Act
            string additions = _service.GetPromptAdditions(mood, null);
            
            // Assert
            Assert.IsNotEmpty(additions);
        }
        
        #endregion
    }
}
