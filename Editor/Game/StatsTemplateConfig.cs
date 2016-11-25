﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace M8 {
    public class StatsTemplateConfig : EditorWindow {
        public const string projConfigTextStatsTemplate = "game.statsTemplate";
        public const string projConfigScriptFolder = "game.scriptFolder";

        public const string defaultFilename = "StatID.cs";
        
        private static List<StatTemplateData> mStats = null;

        private TextAsset mTextFileMapper;
        private string mTextNameMapper = "";
        private string mTextFilePathMapper = "";
        private string mGenerateScriptFolder;

        private uint mUnknownCount = 0;

        private Vector2 mScroll;

        public static List<StatTemplateData> stats {
            get {
                if(mStats == null) {
                    //try to load it
                    TextAsset text = ProjectConfig.GetObject<TextAsset>(projConfigTextStatsTemplate);
                    if(text)
                        GetStatNames(text);
                }

                return mStats;
            }
        }

        public static int GetStatsIndex(int id) {
            var _stats = stats;
            for(int i = 0; i < _stats.Count; i++) {
                if(_stats[i].id == id)
                    return i;
            }

            return -1;
        }

        public static void Open() {
            EditorWindow.GetWindow(typeof(StatsTemplateConfig));
        }

        private static void GetStatNames(TextAsset cfg) {
            if(cfg != null) {
                mStats = StatTemplateList.FromJSON(cfg.text);
                if(mStats == null)
                    mStats = new List<StatTemplateData>();
            }
        }

        private void GenerateScript() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("//AUTOGENERATED - DO NOT TOUCH!");
            sb.AppendLine("public struct StatID {");

            for(int i = 0; i < mStats.Count; i++) {
                sb.AppendFormat("    public const int {0} = {1};", mStats[i].name, mStats[i].id);
                sb.AppendLine();
            }
            
            sb.AppendLine("}");

            using(StreamWriter output = new StreamWriter(mGenerateScriptFolder+"/"+defaultFilename)) {
                output.Write(sb.ToString());
            }
        }

        private int GetAvailableID() {
            if(mStats != null) {
                for(int id = 1; id < int.MaxValue; id++) {
                    bool isFound = false;
                    for(int i = 0; i < mStats.Count; i++) {
                        if(mStats[i].id == id) {
                            isFound = true;
                            break;
                        }
                    }

                    if(!isFound)
                        return id;
                }
            }

            return 1;
        }

        private int GetStatIndex(int id, int excludeIndex) {
            for(int i = 0; i < mStats.Count; i++) {
                if(i != excludeIndex && mStats[i].id == id)
                    return i;
            }

            return -1;
        }

        void OnEnable() {
            mTextFileMapper = ProjectConfig.GetObject<TextAsset>(projConfigTextStatsTemplate);
            mGenerateScriptFolder = ProjectConfig.GetString(projConfigScriptFolder, Application.dataPath+"/Scripts");
        }

        void OnDisable() {
            if(mTextFileMapper != null)
                ProjectConfig.SetObject(projConfigTextStatsTemplate, mTextFileMapper);

            ProjectConfig.SetString(projConfigScriptFolder, mGenerateScriptFolder);
        }

        void OnGUI() {
            Color defaultBkgrndClr = GUI.backgroundColor;
            bool defaultEnabled = GUI.enabled;

            mScroll = GUILayout.BeginScrollView(mScroll);//, GUILayout.MinHeight(100));

            TextAsset prevTextFile = mTextFileMapper;

            EditorGUIUtility.labelWidth = 80.0f;

            //Text Mapping
            GUILayout.BeginHorizontal();

            bool doCreate = false;

            if(mTextFileMapper == null) {
                GUI.backgroundColor = Color.green;
                doCreate = GUILayout.Button("Create", GUILayout.Width(76f));

                GUI.backgroundColor = Color.white;
                mTextNameMapper = GUILayout.TextField(mTextNameMapper);
            }

            GUILayout.EndHorizontal();

            if(mTextFileMapper != null) {
                mTextNameMapper = mTextFileMapper.name;
                mTextFilePathMapper = AssetDatabase.GetAssetPath(mTextFileMapper);
            }
            else if(!string.IsNullOrEmpty(mTextNameMapper)) {
                mTextFilePathMapper = EditorExt.Utility.GetSelectionFolder() + mTextNameMapper + ".txt";
            }

            if(doCreate && !string.IsNullOrEmpty(mTextNameMapper)) {
                File.WriteAllText(mTextFilePathMapper, "");

                AssetDatabase.Refresh();

                mTextFileMapper = (TextAsset)AssetDatabase.LoadAssetAtPath(mTextFilePathMapper, typeof(TextAsset));
            }
                        
            GUILayout.BeginHorizontal();

            GUILayout.Label("Select: ");

            mTextFileMapper = (TextAsset)EditorGUILayout.ObjectField(mTextFileMapper, typeof(TextAsset), false);

            GUILayout.EndHorizontal();

            if(!string.IsNullOrEmpty(mTextFilePathMapper))
                GUILayout.Label("Path: " + mTextFilePathMapper);
            else {
                GUILayout.Label("Path: <none>" + mTextFilePathMapper);
            }
            //

            //Script Generator
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Generate Script");

            EditorGUIUtility.labelWidth = 46.0f;
            GUILayout.BeginHorizontal();
            mGenerateScriptFolder = EditorGUILayout.TextField("Folder", mGenerateScriptFolder);
            if(GUILayout.Button("Browse", GUILayout.Width(55f))) {
                string path = EditorUtility.SaveFolderPanel("Select Directory", mGenerateScriptFolder, "");
                if(!string.IsNullOrEmpty(path)) mGenerateScriptFolder = path;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Path: "+mGenerateScriptFolder+"/"+defaultFilename);

            GUILayout.EndVertical();

            GUILayout.Space(6f);

            //////////
            // Stat list
            EditorExt.Utility.DrawSeparator();

            GUILayout.BeginVertical();

            bool canSave = true;

            if(mTextFileMapper != null) {
                if(prevTextFile != mTextFileMapper || mStats == null)
                    GetStatNames(mTextFileMapper);

                //list actions
                int removeInd = -1;

                Regex r = new Regex("^[a-zA-Z0-9]*$");

                for(int i = 0; i < mStats.Count; i++) {
                    //check if it's a duplicate
                    int dupInd = GetStatIndex(mStats[i].id, i);
                    if(dupInd != -1) {
                        canSave = false;
                        GUI.backgroundColor = Color.red;
                    }
                    else
                        GUI.backgroundColor = defaultBkgrndClr;

                    GUILayout.BeginHorizontal(GUI.skin.box);

                    string text = GUILayout.TextField(mStats[i].name, 255);

                    GUILayout.Space(10f);
                    GUILayout.Label("ID", GUILayout.MaxWidth(20f));
                    int id = EditorGUILayout.IntField(mStats[i].id, GUILayout.MaxWidth(60));

                    GUILayout.Space(20f);
                                        
                    if(GUILayout.Button("DEL", GUILayout.MaxWidth(40))) {
                        removeInd = i;
                    }

                    if(text.Length > 0 && (r.IsMatch(text) && !char.IsDigit(text[0])))
                        mStats[i] = new StatTemplateData() { name=text, id=id };
                    else
                        mStats[i] = new StatTemplateData() { name=mStats[i].name, id=id };

                    GUILayout.EndHorizontal();
                }

                if(removeInd != -1)
                    mStats.RemoveAt(removeInd);

                GUI.backgroundColor = defaultBkgrndClr;

                if(GUILayout.Button("Add")) {
                    mStats.Add(new StatTemplateData() { name = "Unknown" + (mUnknownCount++), id = GetAvailableID() });
                }
            }
            
            GUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 0.0f;

            GUILayout.EndScrollView();

            //////// Save

            EditorExt.Utility.DrawSeparator();

            GUI.backgroundColor = Color.green;
            GUI.enabled = canSave && mTextFileMapper != null && mStats != null;

            if(GUILayout.Button("Save")) {
                //save mapping
                string statString = StatTemplateList.ToJSON(mStats, false);
                File.WriteAllText(mTextFilePathMapper, statString);

                GenerateScript();
                //

                AssetDatabase.Refresh();
            }

            GUI.backgroundColor = defaultBkgrndClr;
            GUI.enabled = defaultEnabled;
        }
    }
}