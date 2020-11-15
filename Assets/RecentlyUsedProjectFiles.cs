using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RecentlyUsed
{
	public class RecentlyUsedProjectFiles : RecentlyUsed
	{
		[Serializable]
		public class ProjectFile : RecentlyUsedObject
		{
			public string AssetPath;

			[NonSerialized]
			protected Object CachedTarget;

			public new Object Target
			{
				get
				{
					if (this.CachedTarget == null && !string.IsNullOrEmpty(this.AssetPath))
					{
						this.CachedTarget = AssetDatabase.LoadAssetAtPath<Object>(AssetPath);
					}

					return this.CachedTarget;
				}
				set { this.CachedTarget = value; }
			}
		}

		//unity json can only serialize objects,that's the only purposes of this
		private class JsonData
		{
			public List<ProjectFile> Data = new List<ProjectFile>();
		}

		//------------------------------------------------------------------------------------------------
		public override string InfoLabel => $"Sum: {pProjectFiles.Count} Favorite: {iFavoriteCount}";
		private static string DataPath => Application.persistentDataPath + "/Recently_ProjectFiles.txt";
		private Dictionary<string, ProjectFile> pProjectFiles = new Dictionary<string, ProjectFile>();
		private int iFavoriteCount;

		//------------------------------------------------------------------------------------------------
		[MenuItem("RecentlyUsed/Remove None Favorite Data")]
		private static void RemoveNoneFavoriteData()
		{
			var pWindow = (RecentlyUsedProjectFiles) GetWindow(typeof(RecentlyUsedProjectFiles));
			pWindow.pProjectFiles = new Dictionary<string, ProjectFile>();
		}

		//------------------------------------------------------------------------------------------------
		[MenuItem("RecentlyUsed/Project Files", false, -10)]
		private static void Init()
		{
			var pWindow = (RecentlyUsedProjectFiles) GetWindow(typeof(RecentlyUsedProjectFiles));
			pWindow.titleContent = new GUIContent("Project Files", EditorGUIUtility.FindTexture("CustomSorting"));
			pWindow.Show();
		}

		//------------------------------------------------------------------------------------------------
		public override void OnRemoveNoneStarred()
		{
			var copy = new Dictionary<string, ProjectFile>(pProjectFiles);
			foreach (var pair in copy)
			{
				if (!pair.Value.IsFavorite)
				{
					pProjectFiles.Remove(pair.Key);
				}
			}

			this.Save();
		}

		//------------------------------------------------------------------------------------------------

		public override void OnRemoveAll()
		{
			pProjectFiles = new Dictionary<string, ProjectFile>();
			this.Repaint();
			this.Save();
		}

		//------------------------------------------------------------------------------------------------
		public override void OnDraw()
		{
			GUILayout.Space(2);
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

			var pSortedList = this.Sort(pProjectFiles.Values.ToList());
			var pToRemove = new List<ProjectFile>();
			
			iFavoriteCount = 0;
			foreach (var projectFile in pSortedList)
			{
				if (projectFile.Target == null) continue;

				this.DrawCommonField(projectFile.Target,
						AssetDatabase.GetCachedIcon(projectFile.AssetPath),
						ref projectFile.IsFavorite,
						out var bRemoved);

				if (projectFile.IsFavorite)
				{
					iFavoriteCount++;
				}
				if (bRemoved)
				{
					pToRemove.Add(projectFile);
				}
			}

			foreach (var sceneObject in pToRemove)
			{
				pProjectFiles.Remove(sceneObject.AssetPath);
			}

			GUILayout.EndVertical();
		}

		//------------------------------------------------------------------------------------------------
		public override void SelectionChanged()
		{
			//only objects with a guid are valid project files
			if (Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0)
			{
				this.AddNewProjectFile(Selection.assetGUIDs[0]);
			}
		}

		//------------------------------------------------------------------------------------------------
		private void AddNewProjectFile(string sGuid)
		{
			var pAssetPath = AssetDatabase.GUIDToAssetPath(sGuid);
			if (!pProjectFiles.TryGetValue(pAssetPath, out var pCommonObject))
			{
				pCommonObject = new ProjectFile {AssetPath = pAssetPath};
				pProjectFiles.Add(pAssetPath, pCommonObject);
			}

			pCommonObject.SelectionCount++;
			this.Repaint();
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Restore recently used objects from a given collection.
		/// </summary>
		private void FillFromList(IEnumerable<ProjectFile> pTargetObjects)
		{
			this.pProjectFiles = new Dictionary<string, ProjectFile>();
			foreach (var sceneObject in pTargetObjects)
			{
				if (!this.pProjectFiles.ContainsKey(sceneObject.AssetPath))
				{
					this.pProjectFiles.Add(sceneObject.AssetPath, sceneObject);
				}
			}
		}

		//------------------------------------------------------------------------------------------------
		public override void Load()
		{
			if (File.Exists(DataPath))
			{
				var json = File.ReadAllText(DataPath);
				var jsonData = JsonUtility.FromJson<JsonData>(json);
				this.FillFromList(jsonData.Data);
			}
		}

		//------------------------------------------------------------------------------------------------
		public override void Save()
		{
			if (this.pProjectFiles != null)
			{
				var data = new JsonData() {Data = this.pProjectFiles.Values.ToList()};
				var json = JsonUtility.ToJson(data);
				File.WriteAllText(DataPath, json);
			}
		}
	}
}