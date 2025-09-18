#region
using System.IO;
using UnityEditor;
#endregion

public static class ScriptWriter
{
	const string templatePath = "Assets/_Project/Runtime/_Scripts/Scriptables/Combos/CE_Default.cs";
	const string savePath = "Assets/_Project/Runtime/_Scripts/Scriptables/Combos/";

	public static void CreateComboEffect(string effectName)
	{
		// Read the template
		string template = File.ReadAllText(templatePath);

		// Replace class name
		bool hasPrefix = effectName.StartsWith("CE_");
		string newClassName = hasPrefix ? effectName : $"CE_{effectName}";
		template = template.Replace("CE_Default", newClassName);

		// Write new script
		string newFilePath = Path.Combine(savePath, newClassName + ".cs");
		File.WriteAllText(newFilePath, template);

		// overwrite the fileName and menuName in the CreateAssetMenu attribute
		string fileName = hasPrefix ? effectName.Replace("CE_", "") : effectName;
		string menuName = $"Combos/New {fileName}";
		string createAssetMenuAttribute = $"[CreateAssetMenu(fileName = \"{fileName}\", menuName = \"{menuName}\", order = 0)]";
		template = template.Replace("[CreateAssetMenu(fileName = \"Default Key Effect (None)\", menuName = \"Combos/New Default Key Effect (None)\", order = 0)]", createAssetMenuAttribute);

		// set the 'order' in the CreateAssetMenu attribute based on existing scripts
		string[] existingFiles = Directory.GetFiles(savePath, "CE_*.cs");
		int newOrder = existingFiles.Length + 1;
		template = template.Replace("order = 0", $"order = {newOrder}");

		File.WriteAllText(newFilePath, template);

		AssetDatabase.ImportAsset(newFilePath);
		AssetDatabase.Refresh();
	}
}
