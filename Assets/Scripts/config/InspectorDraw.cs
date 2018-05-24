﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
static class RectExtension
{
    public static Rect Split(this Rect rect, int index, int count)
    {
        int r = (int)rect.width % count; // Remainder used to compensate width and position.
        int width = (int)(rect.width / count);
        rect.width = width + (index < r ? 1 : 0) + (index + 1 == count ? (rect.width - (int)rect.width) : 0f);
        if (index > 0)
        { rect.x += width * index + (r - (count - 1 - index)); }

        return rect;
    }
}
#endif //UNITY_EDITOR

[Serializable, HideInInspector]
public class InspectorDraw : object
{
    [SerializeField]
    protected string mName = null;
    public string Name 
    {
        get{return mName == null ? (mName = this.GetType().ToString()) : mName;}
        set{mName = value;}
    }

#if UNITY_EDITOR

    protected bool mFoldOut = false;
    public virtual void DrawInspector(int indent = 0, GUILayoutOption[] guiOpts = null)
    {
        if (mFoldOut)
        {
            Name = EditorGUILayout.TextField("Name", Name);

            var type = this.GetType();
            var fields = type.GetFields(
                      BindingFlags.Default
                    // | BindingFlags.DeclaredOnly // no inherited 
                    | BindingFlags.Instance
                    | BindingFlags.Public
                ).ToList();
            fields.Sort(delegate(FieldInfo i, FieldInfo j){ return i.Name.CompareTo(j.Name);});
            foreach (var i in fields)
            {
                var v = i.GetValue(this);
                if (v is bool)
                {
                    var tmp = EditorGUILayout.Toggle(i.Name, (bool)v);
                    i.SetValue(this, tmp);
                }
                else if (v is int || v is uint || v is long)
                {
                    if (i.Name.ToLower().Contains("time"))
                    {
                        EditorGUILayout.LabelField(i.Name, DateTime.FromFileTime((long)v).ToString());
                    }
                    else
                    {
                        var tmp = EditorGUILayout.IntField(i.Name, (int)v);
                        i.SetValue(this, tmp);
                    }
                }
                else if (v is double || v is float)
                {
                    var tmp = EditorGUILayout.FloatField(i.Name, (float)v);
                    i.SetValue(this, tmp);
                }
                else if (v is string)
                {
                    var tmp = EditorGUILayout.TextField(i.Name, (string)v);
                    i.SetValue(this, tmp);
                }
                else if (v is List<int> || v is List<uint> || v is List<long>)
                {
                    EditorGUILayout.LabelField(i.Name);
                    ++EditorGUI.indentLevel;
                    var l = v as List<int>;
                    var size = l.Count;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Size");
                        size = EditorGUILayout.DelayedIntField(size);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (size < l.Count)
                    {
                        l.RemoveRange(size, l.Count - size);
                    }
                    else if (size > l.Count)
                    {
                        for (var i2 = l.Count; i2 < size; ++i2)
                            l.Add(0);
                    }
                    for (var idx = 0; idx < l.Count; ++idx)
                    {
                        l[idx] = EditorGUILayout.IntField(l[idx]);
                    }
                    --EditorGUI.indentLevel;
                }
                else if (v is List<string>)
                {
                    EditorGUILayout.LabelField(i.Name);
                    ++EditorGUI.indentLevel;
                    var l = v as List<string>;
                    var size = l.Count;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Size");
                        size = EditorGUILayout.DelayedIntField(size);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (size < l.Count)
                    {
                        l.RemoveRange(size, l.Count - size);
                    }
                    else if (size > l.Count)
                    {
                        for (var i2 = l.Count; i2 < size; ++i2)
                            l.Add("");
                    }
                    for (var idx = 0; idx < l.Count; ++idx)
                    {
                        l[idx] = EditorGUILayout.TextField(l[idx]);
                    }
                    --EditorGUI.indentLevel;
                }
                else if (v is string[])
                {
                    EditorGUILayout.LabelField(i.Name);
                    ++EditorGUI.indentLevel;
                    var l = v as string[];
                    for (var idx = 0; idx < l.Length; ++idx)
                    {
                        l[idx] = EditorGUILayout.TextField(l[idx]);
                    }
                    --EditorGUI.indentLevel;
                }
            }
        }
    }

    public virtual void Draw(int indent = 0, GUILayoutOption[] guiOpts = null)
    {
        EditorGUI.indentLevel += indent;
        mFoldOut = EditorGUILayout.Foldout(mFoldOut, Name, true);
        if (mFoldOut) 
        {
            using (var verticalScope = new EditorGUILayout.VerticalScope("box"))
            {
                // ++EditorGUI.indentLevel;
                DrawInspector(indent, guiOpts);
                // --EditorGUI.indentLevel;
            }
        }
        EditorGUI.indentLevel -= indent;
    }

#endif
}
