// =============================================================================
// OdinFallbackAttributes.cs
// Provides fallback attributes when Odin Inspector is not installed.
// When Odin IS installed, define ODIN_INSPECTOR in Player Settings.
// =============================================================================

using System;
using UnityEngine;

namespace InteractiveNPC.Core.Attributes
{
    #region Odin Fallback Attributes
    
    // When Odin Inspector is not installed, these provide no-op fallback attributes
    // that prevent compilation errors. When Odin IS installed, the real Odin
    // attributes will be used via the #if ODIN_INSPECTOR directive in actual code.
    
#if !ODIN_INSPECTOR
    
    /// <summary>
    /// Fallback for Odin's [Title] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class TitleAttribute : PropertyAttribute
    {
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public bool Bold { get; set; } = true;
        public bool HorizontalLine { get; set; } = true;
        
        public TitleAttribute(string title, string subtitle = null)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [BoxGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class BoxGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }
        public bool ShowLabel { get; set; } = true;
        public bool CenterLabel { get; set; } = false;
        
        public BoxGroupAttribute(string groupName = null)
        {
            GroupName = groupName;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [FoldoutGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class FoldoutGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }
        public bool Expanded { get; set; } = false;
        
        public FoldoutGroupAttribute(string groupName, bool expanded = false)
        {
            GroupName = groupName;
            Expanded = expanded;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [TabGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class TabGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }
        public string TabName { get; private set; }
        
        public TabGroupAttribute(string groupName, string tabName = null)
        {
            GroupName = groupName;
            TabName = tabName;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [Button] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        public string Name { get; private set; }
        public int ButtonHeight { get; set; } = 0;
        
        public ButtonAttribute(string name = null)
        {
            Name = name;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [ShowIf] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string Condition { get; private set; }
        public object OptionalValue { get; private set; }
        
        public ShowIfAttribute(string condition)
        {
            Condition = condition;
        }

        public ShowIfAttribute(string condition, object optionalValue)
        {
            Condition = condition;
            OptionalValue = optionalValue;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [HideIf] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HideIfAttribute : PropertyAttribute
    {
        public string Condition { get; private set; }
        
        public HideIfAttribute(string condition)
        {
            Condition = condition;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [ReadOnly] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
    
    /// <summary>
    /// Fallback for Odin's [Required] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class RequiredAttribute : PropertyAttribute
    {
        public string ErrorMessage { get; private set; }
        
        public RequiredAttribute(string errorMessage = null)
        {
            ErrorMessage = errorMessage;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [InfoBox] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public string Message { get; private set; }
        public InfoMessageType MessageType { get; set; } = InfoMessageType.Info;
        
        public InfoBoxAttribute(string message)
        {
            Message = message;
        }

        public InfoBoxAttribute(string message, InfoMessageType messageType)
        {
            Message = message;
            MessageType = messageType;
        }
    }
    
    public enum InfoMessageType
    {
        None,
        Info,
        Warning,
        Error
    }
    
    /// <summary>
    /// Fallback for Odin's [PropertyOrder] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class PropertyOrderAttribute : PropertyAttribute
    {
        public float Order { get; private set; }
        
        public PropertyOrderAttribute(float order)
        {
            Order = order;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [LabelText] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class LabelTextAttribute : PropertyAttribute
    {
        public string Text { get; private set; }
        
        public LabelTextAttribute(string text)
        {
            Text = text;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [PropertySpace] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class PropertySpaceAttribute : PropertyAttribute
    {
        public float SpaceBefore { get; private set; }
        public float SpaceAfter { get; private set; }
        
        public PropertySpaceAttribute(float spaceBefore = 0, float spaceAfter = 0)
        {
            SpaceBefore = spaceBefore;
            SpaceAfter = spaceAfter;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [InlineEditor] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class InlineEditorAttribute : PropertyAttribute
    {
        public InlineEditorModes InlineEditorMode { get; set; } = InlineEditorModes.GUIOnly;
        public bool Expanded { get; set; } = false;
        
        public InlineEditorAttribute()
        {
        }

        public InlineEditorAttribute(InlineEditorModes inlineEditorMode)
        {
            InlineEditorMode = inlineEditorMode;
        }
    }
    
    public enum InlineEditorModes
    {
        GUIOnly,
        GUIAndHeader,
        GUIAndPreview,
        SmallPreview,
        LargePreview,
        FullEditor
    }
    
    /// <summary>
    /// Fallback for Odin's [ValueDropdown] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class ValueDropdownAttribute : PropertyAttribute
    {
        public string ValuesGetter { get; private set; }
        
        public ValueDropdownAttribute(string valuesGetter)
        {
            ValuesGetter = valuesGetter;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [OnValueChanged] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class OnValueChangedAttribute : PropertyAttribute
    {
        public string MethodName { get; private set; }
        
        public OnValueChangedAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [ProgressBar] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class ProgressBarAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float R { get; set; } = 0.15f;
        public float G { get; set; } = 0.47f;
        public float B { get; set; } = 0.74f;
        
        public ProgressBarAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public ProgressBarAttribute(float min, float max, float r, float g, float b)
        {
            Min = min;
            Max = max;
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>
    /// Fallback for Odin's [HideLabel] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class HideLabelAttribute : PropertyAttribute
    {
    }
    
    /// <summary>
    /// Fallback for Odin's [ShowInInspector] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class ShowInInspectorAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Fallback for Odin's [HideInInspector] extended attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class HideInEditorModeAttribute : PropertyAttribute
    {
    }
    
    /// <summary>
    /// Fallback for Odin's [HideInPlayMode] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class HideInPlayModeAttribute : PropertyAttribute
    {
    }
    
    /// <summary>
    /// Fallback for Odin's [ValidateInput] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class ValidateInputAttribute : PropertyAttribute
    {
        public string ValidationMethod { get; private set; }
        public string DefaultMessage { get; set; }
        
        public ValidateInputAttribute(string validationMethod, string defaultMessage = null)
        {
            ValidationMethod = validationMethod;
            DefaultMessage = defaultMessage;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [GUIColor] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class GUIColorAttribute : PropertyAttribute
    {
        public float R { get; private set; }
        public float G { get; private set; }
        public float B { get; private set; }
        public float A { get; private set; }
        
        public GUIColorAttribute(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

#endif
    
    #endregion
}
