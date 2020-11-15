using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RecentlyUsed
{
	public abstract class RecentlyUsed : EditorWindow
	{
		private Vector2 vScrollPosition;

		public abstract class RecentlyUsedObject
		{
			[NonSerialized]
			public Object Target;

			public int SelectionCount;
			public bool IsFavorite;
		}

		public void OnGUI()
		{
			vScrollPosition =
					EditorGUILayout.BeginScrollView(vScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
			this.OnDraw();
			EditorGUILayout.EndScrollView();
			
			
		}

		public abstract void OnDraw();
		public abstract void SelectionChanged();

		public abstract void Load();
		public abstract void Save();

		public virtual void OnEnable()
		{
			this.Load();
			Selection.selectionChanged += SelectionChanged;
		}

		public virtual void OnDisable()
		{
			this.Save();
			Selection.selectionChanged -= SelectionChanged;
		}

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

		private int Comparison(RecentlyUsedObject a, RecentlyUsedObject b)
		{
			if (a.SelectionCount > b.SelectionCount) return -1;
			if (a.SelectionCount < b.SelectionCount) return 1;

			return 0;
		}
	}
}