#region
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
#endregion

public static class ScriptWriter
{
	const string templatePath = "Assets/_Project/Runtime/_Scripts/Scriptables/Effects/KE_Default.cs";
	const string savePath = "Assets/_Project/Runtime/_Scripts/Scriptables/Effects/";

	public static void CreateComboEffect(string effectName)
	{
		// Read the template
		string template = File.ReadAllText(templatePath);

		// Replace class name
		bool hasPrefix = effectName.StartsWith("KE_");
		string newClassName = hasPrefix ? effectName : $"KE_{effectName}";
		template = template.Replace("KE_Default", newClassName);

		// Write new script
		string newFilePath = Path.Combine(savePath, newClassName + ".cs");
		File.WriteAllText(newFilePath, template);

		// overwrite the fileName and menuName in the CreateAssetMenu attribute
		string fileName = hasPrefix ? effectName.Replace("KE_", "") : effectName;
		string menuName = $"Combos/New {fileName}";
		string createAssetMenuAttribute = $"[CreateAssetMenu(fileName = \"{fileName}\", menuName = \"{menuName}\", order = 0)]";
		template = template.Replace("[CreateAssetMenu(fileName = \"Default Key Effect (None)\", menuName = \"Combos/New Default Key Effect (None)\", order = 0)]", createAssetMenuAttribute);
		File.WriteAllText(newFilePath, template);

		AssetDatabase.ImportAsset(newFilePath);
		AssetDatabase.Refresh();
	}
}

public class Pipeline : AssetPostprocessor
{
	public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		if (importedAssets.Length > 0)
		{
			foreach (string assetPath in importedAssets)
			{
				if (assetPath.EndsWith(".cs"))
				{
					var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

					if (monoScript != null)
					{
						Type type = monoScript.GetClass();

						if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
						{
							ScriptableObject asset = ScriptableObject.CreateInstance(type);
							string niceName = type.Name.StartsWith("KE_") ? type.Name.Replace("KE_", string.Empty) : type.Name;

							string SAVE_PATH = "Assets/_Project/Runtime/Resources/Scriptables/Effects" + "/" + niceName + ".asset";
							AssetDatabase.CreateAsset(asset, SAVE_PATH);
							Debug.Log($"Created ScriptableObject asset at: {SAVE_PATH}");
						}
						else { Debug.LogWarning($"Class not found or is not a ScriptableObject for script: {assetPath}"); }
					}
				}
			}

			AssetDatabase.Refresh();
			AssetDatabase.SaveAssets();
		}
	}
}
