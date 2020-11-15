using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RecentlyUsed
{
	public abstract class RecentlyUsed : EditorWindow
	{
		//------------------------------------------------------------------------------------------------
		public abstract class RecentlyUsedObject
		{
			[NonSerialized]
			public Object Target;

			public int SelectionCount;
			public bool IsFavorite;
		}

		public abstract string InfoLabel { get; }
		private Vector2 vScrollPosition;

		//------------------------------------------------------------------------------------------------
		public void OnGUI()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
			if (GUILayout.Button("Options", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				var r = GUILayoutUtility.GetLastRect();
				r.y += EditorGUIUtility.singleLineHeight;
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Remove none starred"), false, this.OnRemoveNoneStarred);
				menu.AddItem(new GUIContent("Remove all"), false, this.OnRemoveAll);
				menu.DropDown(r);
			}

			var pStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
			pStyle.alignment = TextAnchor.LowerCenter;
			GUILayout.Label(InfoLabel, pStyle);

			GUILayout.EndHorizontal();
			vScrollPosition = EditorGUILayout.BeginScrollView(vScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
			this.OnDraw();
			EditorGUILayout.EndScrollView();
		}

		//------------------------------------------------------------------------------------------------
		public abstract void OnRemoveNoneStarred();

		//------------------------------------------------------------------------------------------------
		public abstract void OnRemoveAll();

		//------------------------------------------------------------------------------------------------
		public abstract void OnDraw();

		//------------------------------------------------------------------------------------------------
		public abstract void SelectionChanged();

		//------------------------------------------------------------------------------------------------
		public abstract void Load();

		//------------------------------------------------------------------------------------------------
		public abstract void Save();

		//------------------------------------------------------------------------------------------------
		public virtual void OnEnable()
		{
			this.Load();
			Selection.selectionChanged += SelectionChanged;
		}

		//------------------------------------------------------------------------------------------------
		public virtual void OnDisable()
		{
			this.Save();
			Selection.selectionChanged -= SelectionChanged;
		}

		//------------------------------------------------------------------------------------------------
		protected void DrawCommonField(Object pTarget, Texture pIcon, ref bool bIsFavorite, out bool bRemoved)
		{
			bRemoved = false;
			var iWidth = this.position.width;
			var iHeight = EditorGUIUtility.singleLineHeight;
			GUILayout.BeginHorizontal(GUILayout.Height(iHeight), GUILayout.Width(iWidth));
			GUILayout.Space(2);

			var t = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(new Vector2(iHeight - 1, iHeight - 1));

			GUILayout.Label(new GUIContent(pTarget.name, pIcon),
					GUILayout.Height(iHeight),
					GUILayout.Width(iWidth - 85));

			EditorGUIUtility.SetIconSize(t);

			if (GUILayout.Button(EditorGUIUtility.FindTexture("VisibilityOn"),
					EditorStyles.miniButtonLeft,
					GUILayout.Height(iHeight),
					GUILayout.Width(25)))
			{
				EditorGUIUtility.PingObject(pTarget);
			}

			var pDefaultColor = GUI.color;
			GUI.color = bIsFavorite ? Color.yellow : pDefaultColor;
			if (GUILayout.Toggle(false,
					EditorGUIUtility.FindTexture("Favorite"),
					EditorStyles.miniButtonMid,
					GUILayout.Height(iHeight),
					GUILayout.Width(25)))
			{
				bIsFavorite = !bIsFavorite;
			}

			GUI.color = pDefaultColor;
			if (GUILayout.Button(EditorGUIUtility.FindTexture("winbtn_win_close_h"),
					EditorStyles.miniButtonRight,
					GUILayout.Height(iHeight),
					GUILayout.Width(25)))
			{
				bRemoved = true;
			}

			GUILayout.EndHorizontal();
		}

		//------------------------------------------------------------------------------------------------
		public List<T> Sort<T>(List<T> pRecentlyUsed) where T : RecentlyUsedObject
		{
			pRecentlyUsed.Sort(this.Comparison);
			var sortedList = new List<T>();
			foreach (var projectObject in pRecentlyUsed)
			{
				if (projectObject.IsFavorite)
				{
					sortedList.Insert(0, projectObject);
					continue;
				}

				sortedList.Add(projectObject);
			}

			return sortedList;
		}

		//------------------------------------------------------------------------------------------------
		private int Comparison(RecentlyUsedObject a, RecentlyUsedObject b)
		{
			if (a.SelectionCount > b.SelectionCount) return -1;
			if (a.SelectionCount < b.SelectionCount) return 1;

			return 0;
		}
	}
}