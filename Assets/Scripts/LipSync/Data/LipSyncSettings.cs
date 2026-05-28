using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveNPC.LipSync.Data
{
    #region Viseme Type Enum
    
    /// <summary>
    /// Standard viseme types for lip synchronization.
    /// Based on the MPEG-4 viseme set.
    /// </summary>
    public enum VisemeType
    {
        Silence = 0,   // Rest position
        PP = 1,        // p, b, m
        FF = 2,        // f, v
        TH = 3,        // th
        DD = 4,        // t, d, n, l
        KK = 5,        // k, g
        CH = 6,        // ch, j, sh
        SS = 7,        // s, z
        NN = 8,        // n
        RR = 9,        // r
        AA = 10,       // a
        E = 11,        // e
        IH = 12,       // i
        OH = 13,       // o
        U = 14         // u, oo
    }
    
    #endregion
    
    #region Blend Shape Mapping
    
    /// <summary>
    /// Maps a viseme to a set of blend shape influences.
    /// </summary>
    [Serializable]
    public class VisemeBlendShapeMapping
    {
        [SerializeField, Tooltip("The viseme type this mapping applies to")]
        private VisemeType _visemeType;
        
        [SerializeField, Tooltip("Blend shape influences for this viseme")]
        private List<BlendShapeInfluence> _blendShapeInfluences = new List<BlendShapeInfluence>();
        
        public VisemeType VisemeType => _visemeType;
        public IReadOnlyList<BlendShapeInfluence> BlendShapeInfluences => _blendShapeInfluences;
        
        public VisemeBlendShapeMapping() { }
        
        public VisemeBlendShapeMapping(VisemeType type)
        {
            _visemeType = type;
        }
        
        public void AddInfluence(string blendShapeName, float weight)
        {
            _blendShapeInfluences.Add(new BlendShapeInfluence(blendShapeName, weight));
        }
    }
    
    /// <summary>
    /// Represents a blend shape influence with name and weight.
    /// </summary>
    [Serializable]
    public struct BlendShapeInfluence
    {
        [SerializeField, Tooltip("Name of the blend shape")]
        public string BlendShapeName;
        
        [SerializeField, Tooltip("Weight of influence (0-100)")]
        [Range(0f, 100f)]
        public float Weight;
        
        public BlendShapeInfluence(string name, float weight)
        {
            BlendShapeName = name;
            Weight = weight;
        }
    }
    
    #endregion
    
    #region Lip Sync Settings ScriptableObject
    
    /// <summary>
    /// ScriptableObject for configuring lip sync settings.
    /// Maps visemes to blend shapes and configures transition parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "LipSyncSettings", menuName = "Interactive NPC/LipSync/Lip Sync Settings")]
    public class LipSyncSettings : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Transition Settings")]
        [SerializeField, Tooltip("Time to transition between visemes")]
        [Range(0.01f, 0.2f)]
        private float _transitionTime = 0.05f;
        
        [SerializeField, Tooltip("Smoothing factor for blend shape transitions")]
        [Range(0f, 1f)]
        private float _smoothing = 0.3f;
        
        [SerializeField, Tooltip("Overall intensity multiplier for lip movements")]
        [Range(0f, 2f)]
        private float _intensityMultiplier = 1f;
        
        [Header("Audio Analysis")]
        [SerializeField, Tooltip("Minimum audio level to trigger lip movement")]
        [Range(0f, 0.1f)]
        private float _audioThreshold = 0.01f;
        
        [SerializeField, Tooltip("Audio analysis window size in samples")]
        private int _analysisWindowSize = 512;
        
        [Header("Viseme Mappings")]
        [SerializeField, Tooltip("Mappings from visemes to blend shapes")]
        private List<VisemeBlendShapeMapping> _visemeMappings = new List<VisemeBlendShapeMapping>();
        
        [Header("Default Blend Shape Names")]
        [SerializeField, Tooltip("Common blend shape name patterns for auto-detection")]
        private BlendShapeNameConfig _blendShapeNames = new BlendShapeNameConfig();
        
        #endregion
        
        #region Properties
        
        public float TransitionTime => _transitionTime;
        public float Smoothing => _smoothing;
        public float IntensityMultiplier => _intensityMultiplier;
        public float AudioThreshold => _audioThreshold;
        public int AnalysisWindowSize => _analysisWindowSize;
        public IReadOnlyList<VisemeBlendShapeMapping> VisemeMappings => _visemeMappings;
        public BlendShapeNameConfig BlendShapeNames => _blendShapeNames;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get the blend shape mapping for a viseme type.
        /// </summary>
        public VisemeBlendShapeMapping GetMapping(VisemeType viseme)
        {
            return _visemeMappings.Find(m => m.VisemeType == viseme);
        }
        
        /// <summary>
        /// Get the blend shape mapping for a viseme ID.
        /// </summary>
        public VisemeBlendShapeMapping GetMapping(int visemeId)
        {
            if (visemeId < 0 || visemeId > 14) return null;
            return GetMapping((VisemeType)visemeId);
        }
        
        /// <summary>
        /// Initialize with default mappings for common blend shape setups.
        /// </summary>
        public void InitializeDefaultMappings()
        {
            _visemeMappings.Clear();
            
            // Create default mappings
            var silence = new VisemeBlendShapeMapping(VisemeType.Silence);
            
            var pp = new VisemeBlendShapeMapping(VisemeType.PP);
            pp.AddInfluence("mouthClose", 80);
            pp.AddInfluence("mouthPucker", 30);
            
            var ff = new VisemeBlendShapeMapping(VisemeType.FF);
            ff.AddInfluence("mouthFunnel", 60);
            ff.AddInfluence("mouthLowerDownLeft", 20);
            ff.AddInfluence("mouthLowerDownRight", 20);
            
            var th = new VisemeBlendShapeMapping(VisemeType.TH);
            th.AddInfluence("tongueOut", 40);
            th.AddInfluence("mouthOpen", 20);
            
            var dd = new VisemeBlendShapeMapping(VisemeType.DD);
            dd.AddInfluence("mouthOpen", 30);
            dd.AddInfluence("tongueOut", 20);
            
            var kk = new VisemeBlendShapeMapping(VisemeType.KK);
            kk.AddInfluence("mouthOpen", 40);
            kk.AddInfluence("jawOpen", 30);
            
            var ch = new VisemeBlendShapeMapping(VisemeType.CH);
            ch.AddInfluence("mouthShrugLower", 40);
            ch.AddInfluence("mouthPucker", 20);
            
            var ss = new VisemeBlendShapeMapping(VisemeType.SS);
            ss.AddInfluence("mouthSmileLeft", 30);
            ss.AddInfluence("mouthSmileRight", 30);
            
            var nn = new VisemeBlendShapeMapping(VisemeType.NN);
            nn.AddInfluence("mouthOpen", 20);
            nn.AddInfluence("mouthClose", 40);
            
            var rr = new VisemeBlendShapeMapping(VisemeType.RR);
            rr.AddInfluence("mouthPucker", 40);
            rr.AddInfluence("mouthFunnel", 30);
            
            var aa = new VisemeBlendShapeMapping(VisemeType.AA);
            aa.AddInfluence("jawOpen", 70);
            aa.AddInfluence("mouthOpen", 60);
            
            var e = new VisemeBlendShapeMapping(VisemeType.E);
            e.AddInfluence("mouthSmileLeft", 40);
            e.AddInfluence("mouthSmileRight", 40);
            e.AddInfluence("mouthOpen", 30);
            
            var ih = new VisemeBlendShapeMapping(VisemeType.IH);
            ih.AddInfluence("mouthSmileLeft", 50);
            ih.AddInfluence("mouthSmileRight", 50);
            ih.AddInfluence("mouthOpen", 20);
            
            var oh = new VisemeBlendShapeMapping(VisemeType.OH);
            oh.AddInfluence("mouthFunnel", 60);
            oh.AddInfluence("jawOpen", 50);
            
            var u = new VisemeBlendShapeMapping(VisemeType.U);
            u.AddInfluence("mouthPucker", 70);
            u.AddInfluence("mouthFunnel", 40);
            
            _visemeMappings.AddRange(new[] { silence, pp, ff, th, dd, kk, ch, ss, nn, rr, aa, e, ih, oh, u });
        }
        
        #endregion
    }
    
    #endregion
    
    #region Blend Shape Name Config
    
    /// <summary>
    /// Configuration for common blend shape names across different character rigs.
    /// </summary>
    [Serializable]
    public class BlendShapeNameConfig
    {
        [SerializeField] public string JawOpen = "jawOpen";
        [SerializeField] public string MouthOpen = "mouthOpen";
        [SerializeField] public string MouthClose = "mouthClose";
        [SerializeField] public string MouthSmileLeft = "mouthSmileLeft";
        [SerializeField] public string MouthSmileRight = "mouthSmileRight";
        [SerializeField] public string MouthPucker = "mouthPucker";
        [SerializeField] public string MouthFunnel = "mouthFunnel";
        [SerializeField] public string TongueOut = "tongueOut";
    }
    
    #endregion
}
