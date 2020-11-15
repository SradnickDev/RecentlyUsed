using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecentlyUsed
{
	public class RecentlyUsedSceneObjects : RecentlyUsed
	{
		[Serializable]
		public class SceneObject : RecentlyUsedObject
		{
			public int LocalIdentifier;

			public SceneObject(GameObject gameObject)
			{
				this.Target = gameObject;
				this.LocalIdentifier = GetLocalIdentifier(gameObject);
			}
		}

		//unity json can only serialize objects,that's the only purposes of this
		private class JsonData
		{
			public List<SceneObject> Data = new List<SceneObject>();
		}

		//------------------------------------------------------------------------------------------------
		public override string InfoLabel => $"Sum: {iSumCount} Favorite: {iFavoriteCount}";
		private static string DataPath => Application.persistentDataPath + "/Recently_SceneObjects.txt";
		private Dictionary<int, SceneObject> pSceneObjects = new Dictionary<int, SceneObject>();
		private static PropertyInfo pPropertyInfo;
		private int iFavoriteCount;
		private int iSumCount;

		//------------------------------------------------------------------------------------------------
		[MenuItem("RecentlyUsed/Scene Objects", false, -10)]
		private static void Init()
		{
			var pWindow = (RecentlyUsedSceneObjects) GetWindow(typeof(RecentlyUsedSceneObjects));
			pWindow.titleContent = new GUIContent("Scene Objets", EditorGUIUtility.FindTexture("CustomSorting"));
			pWindow.Show();
		}

		//------------------------------------------------------------------------------------------------

		public override void OnRemoveNoneStarred()
		{
			var copy = new Dictionary<int, SceneObject>(pSceneObjects);
			foreach (var pair in copy)
			{
				if (!pair.Value.IsFavorite)
				{
					pSceneObjects.Remove(pair.Key);
				}
			}

			this.Save();
		}

		//------------------------------------------------------------------------------------------------

		public override void OnRemoveAll()
		{
			pSceneObjects = new Dictionary<int, SceneObject>();
			this.Save();
		}

		//------------------------------------------------------------------------------------------------
		public override void OnDraw()
		{
			GUILayout.Space(2);
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

			iFavoriteCount = 0;
			iSumCount = 0;
			var pSortedList = this.Sort(pSceneObjects.Values.ToList());
			var pToRemove = new List<SceneObject>();
			foreach (var sceneObject in pSortedList)
			{
				if (sceneObject.Target == null) continue;

				this.DrawCommonField(sceneObject.Target,
						PrefabUtility.GetIconForGameObject(sceneObject.Target as GameObject),
						ref sceneObject.IsFavorite,
						out var bRemoved);

				if (sceneObject.IsFavorite)
				{
					iFavoriteCount++;
				}

				iSumCount++;
				if (bRemoved)
				{
					pToRemove.Add(sceneObject);
				}
			}

			foreach (var sceneObject in pToRemove)
			{
				pSceneObjects.Remove(sceneObject.LocalIdentifier);
			}

			GUILayout.EndVertical();
		}

		//------------------------------------------------------------------------------------------------
		public override void SelectionChanged()
		{
			//if there is a guid available an project file is the selected, scene gameobjects dont have a guid
			if (Selection.gameObjects != null &&
				Selection.gameObjects.Length > 0 &&
				Selection.assetGUIDs.Length == 0)
			{
				var pSceneObject = this.AddSceneObject(Selection.gameObjects[0]);
				if (pSceneObject != null)
				{
					pSceneObject.SelectionCount++;
					this.Repaint();
				}
			}
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Restore recently used objects from a given collection.
		/// </summary>
		private void FillFromList(IEnumerable<SceneObject> pTargetObjects)
		{
			this.pSceneObjects = new Dictionary<int, SceneObject>();
			foreach (var sceneObject in pTargetObjects)
			{
				if (sceneObject.LocalIdentifier == 0) continue;

				if (!this.pSceneObjects.ContainsKey(sceneObject.LocalIdentifier))
				{
					this.pSceneObjects.Add(sceneObject.LocalIdentifier, sceneObject);
				}
			}
		}

		//------------------------------------------------------------------------------------------------
		public SceneObject AddSceneObject(GameObject pGameObject)
		{
			var iLocalIdentifier = GetLocalIdentifier(pGameObject);
			if (iLocalIdentifier == 0) return null;

			if (!this.pSceneObjects.ContainsKey(iLocalIdentifier))
			{
				var pSceneObject = new SceneObject(pGameObject);
				this.pSceneObjects.Add(iLocalIdentifier, pSceneObject);
			}

			return this.pSceneObjects[iLocalIdentifier];
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Reference to an GameObject cant be serialized, so it will be lost but we can set the reference by comparing the local identifier.
		/// </summary>
		public void SetReference(GameObject pGameObject)
		{
			var iLocalIdentifier = GetLocalIdentifier(pGameObject);
			if (iLocalIdentifier == 0) return;

			if (this.pSceneObjects.ContainsKey(iLocalIdentifier))
			{
				this.pSceneObjects[iLocalIdentifier].Target = pGameObject;
			}
		}

		//------------------------------------------------------------------------------------------------
		private void FindReferences(Scene pScene)
		{
			var root = pScene.GetRootGameObjects();
			for (var j = 0; j < root.Length; j++)
			{
				GetChildren(root[j]);
			}

			this.Repaint();
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Using recursion to get all children from an gameobject.
		/// </summary>
		private void GetChildren(GameObject pGameObject)
		{
			this.SetReference(pGameObject);
			for (var i = 0; i < pGameObject.transform.childCount; i++)
			{
				var child = pGameObject.transform.GetChild(i);
				this.GetChildren(child.gameObject);
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
				EditorApplication.RepaintHierarchyWindow();
			}

			//looking for all references from all open scenes
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				this.FindReferences(scene);
			}
		}

		//------------------------------------------------------------------------------------------------
		public override void Save()
		{
			if (this.pSceneObjects != null)
			{
				var data = new JsonData() {Data = this.pSceneObjects.Values.ToList()};
				var json = JsonUtility.ToJson(data);
				File.WriteAllText(DataPath, json);
			}
		}

		//------------------------------------------------------------------------------------------------
		public override void OnEnable()
		{
			base.OnEnable();
			EditorSceneManager.activeSceneChangedInEditMode += OnSceneOpened;
			EditorSceneManager.sceneOpened += OnActiveSceneChanged;
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Find all reference to restore the recently used objects.
		/// </summary>
		private void OnActiveSceneChanged(Scene pScene, OpenSceneMode mode)
		{
			this.FindReferences(pScene);
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// Find all reference from from new added scene to restore the recently used objects.(multiple scenes)
		/// </summary>
		private void OnSceneOpened(Scene _, Scene pScene)
		{
			this.FindReferences(pScene);
		}

		public override void OnDisable()
		{
			base.OnDisable();
			EditorSceneManager.activeSceneChangedInEditMode -= OnSceneOpened;
			EditorSceneManager.sceneOpened -= OnActiveSceneChanged;
		}

		//------------------------------------------------------------------------------------------------
		/// <summary>
		/// A local identifier is the only persistent value that can be used to track gameobjects in a scene.
		/// </summary>
		public static int GetLocalIdentifier(GameObject pGameObject)
		{
			if (pGameObject == null) return 0;

			if (pPropertyInfo == null)
			{
				pPropertyInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
			}

			int iLocalIdentifier = 0;
			using (var pSerializedObject = new SerializedObject(pGameObject))
			{
				pPropertyInfo.SetValue(pSerializedObject, InspectorMode.Debug, null);
				var pLocalIdentifier = pSerializedObject.FindProperty("m_LocalIdentfierInFile"); //not a typo
				iLocalIdentifier = pLocalIdentifier.intValue;
			}

			return iLocalIdentifier;
		}
	}
}