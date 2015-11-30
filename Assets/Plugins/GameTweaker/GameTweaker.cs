#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Reflection;


/// <summary>
/// Should this field be visible in the GameTweaker window
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
sealed class TweakableField : System.Attribute
{
    public enum Options
    {
        DEFAULT = 0,
        SHARED = 1 << 1
    }
    readonly Options options;
    public TweakableField(Options options = Options.DEFAULT)
    {
        this.options = options;
    }
    public TweakableField (bool sharedAmongAllInstances) 
    {
        options = Options.DEFAULT;
        if (sharedAmongAllInstances) options |= Options.SHARED;
    }

    public bool isSharedAmongAllInstances { get { return (options & Options.SHARED) == Options.SHARED; } }
}

public class GameTweaker : EditorWindow {

    private List<TweakableClass> tweakables;
    private Vector2 scroll = Vector2.zero;

    private struct TweakableClass
    {
        // Class type
        public Type type;
        // Tweakable fields
        public FieldInfo[] sharedFields;
        public FieldInfo[] instancedFields;

        public UnityEngine.Object[] objects;
        // gameobjects in scene with script of type
        // public SerializedObject[] objects;

    }

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Game Tweaker")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        GameTweaker window = (GameTweaker)EditorWindow.GetWindow(typeof(GameTweaker));
        window.RefreshContent();
        window.Show();
    }

    /// <summary>
    /// Performs a new scan for tweakable objects.
    /// </summary>
    private void RefreshContent()
    {
        tweakables = new List<TweakableClass>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if(assembly.GetName().Name == "Assembly-CSharp")
                foreach (Type type in assembly.GetTypes())
                {
                    List<FieldInfo> sharedFields = new List<FieldInfo>();
                    List<FieldInfo> instancedFields = new List<FieldInfo>();
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        TweakableField tweakableAttribute = null;
                        bool isSerialized = field.IsPublic;
                        foreach (Attribute att in field.GetCustomAttributes(true))
                        {
                            if (att is TweakableField) tweakableAttribute = att as TweakableField;
                            isSerialized |= att is SerializeField;
                            if (tweakableAttribute != null && isSerialized)
                            {
                                if ((att as TweakableField).isSharedAmongAllInstances)
                                {
                                    sharedFields.Add(field);
                                }
                                else
                                {
                                    instancedFields.Add(field);
                                }
                                break;
                            }
                        }
                        if (tweakableAttribute != null && !isSerialized)
                        {
                            Debug.LogWarning("It appears some tweakable fields aren't seralized. This is probably due to the fields being marked as private without having the [SerializeField] attribute.");
                        }
                    }
                    if (sharedFields.Count > 0 || instancedFields.Count > 0)
                    {
                        TweakableClass c = new TweakableClass();
                        c.type = type;
                        c.sharedFields = sharedFields.ToArray();
                        c.instancedFields = instancedFields.ToArray();
                        c.objects = FindObjectsOfType(type);
                        tweakables.Add(c);
                    }
                }
        }
    }
    
    void OnGUI()
    {
        try
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (TweakableClass c in tweakables)
            {
                if (c.objects.Length > 0)
                {
                    EditorGUILayout.LabelField(c.type.ToString(), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Shared Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    SerializedObject o = new SerializedObject(c.objects[0]);
                    List<SerializedProperty> props = new List<SerializedProperty>();
                    foreach (FieldInfo field in c.sharedFields)
                    {
                        SerializedProperty p = o.FindProperty(field.Name);
                        if (p == null)
                        {
                            Debug.LogWarning("non-properties aren't supported yet. The type: (" + field.FieldType + ") of \"" + c.type + "." + field.Name + "\" is currently illegal.");
                        }
                        else
                        {
                            PropertyField(p);
                            props.Add(p);
                        }
                        
                        
                    }
                    o.ApplyModifiedProperties();
                    
                    EditorGUI.indentLevel--;
                    
                    
                    foreach (UnityEngine.Object obj in c.objects)
                    {
                        EditorGUILayout.LabelField(obj.name, EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        SerializedObject instObj = new SerializedObject(obj);
                        foreach (SerializedProperty p in props)
                        {
                            instObj.CopyFromSerializedProperty(p);
                        }
                        instObj.ApplyModifiedPropertiesWithoutUndo();
                        foreach (FieldInfo field in c.instancedFields)
                        {
                            SerializedProperty prop = instObj.FindProperty(field.Name);
                            if (prop == null)
                            {
                                Debug.LogWarning("non-properties aren't supported yet. The type: (" + field.FieldType + ") of \"" + c.type + "." + field.Name + "\" is currently illegal.");
                            } else
                                PropertyField(prop);
                        }
                        instObj.ApplyModifiedProperties();
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Separator();
                
            }
            EditorGUILayout.EndScrollView();
        }
        catch (UnityException e)
        {
            Debug.LogWarning("error occured, refreshing...\n" + e.Message);
            RefreshContent();
        }
        
    }
    /// <summary>
    /// helper function to automatically handle arrays
    /// </summary>
    /// <param name="prop"></param>
    private void PropertyField(SerializedProperty prop)
    {
        
        if (prop.isArray)
        {
            DrawArrayProperty(prop);
        }
        else
        {
            EditorGUILayout.PropertyField(prop);
        }
    }

    private void DrawArrayProperty(SerializedProperty prop)
    {
        EditorGUILayout.PropertyField(prop);
        if (prop.isExpanded)
        {
            EditorGUI.indentLevel++;
            SerializedProperty propChild = prop.Copy();
            propChild.NextVisible(true);
            EditorGUILayout.PropertyField(propChild);

            for (int i = 0; i < prop.arraySize; i++)
            {
                SerializedProperty item = prop.GetArrayElementAtIndex(i);
                if (item.isArray)
                {
                    DrawArrayProperty(item);
                }
                else
                {
                    EditorGUILayout.PropertyField(item);
                }

            }
            EditorGUI.indentLevel--;
        }

    }

    void OnFocus()
    {
        RefreshContent();
    }
    void OnLostFocus()
    {
        RefreshContent();
    }
    void OnHeirarchyChange()
    {
        RefreshContent();
    }

    
    
}


#endif