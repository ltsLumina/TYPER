#region
using System.IO;
using UnityEditor;
#endregion

public static class ScriptWriter
{
	const string templateName = "CE_Template";
	readonly static string templatePath = $"Assets/_Project/Runtime/_Scripts/Scriptables/{templateName}.cs"; // Note: not "Combos" folder. It's in Scriptables directly.
	const string savePath = "Assets/_Project/Runtime/_Scripts/Scriptables/Combos/";

	public static void CreateComboEffect(string effectName)
	{
		// Read the template
		string contents = File.ReadAllText(templatePath);
		
		// strip the #if false and #endif lines; they prevent the class from being compiled, but we want the new script to compile
		contents = contents.Replace("#if false", string.Empty);
		contents = contents.Replace("#endif", string.Empty);

		// Replace class name
		bool hasPrefix = effectName.StartsWith("CE_");
		string newClassName = hasPrefix ? effectName : $"CE_{effectName}";
		contents = contents.Replace(templateName, newClassName);

		// Write new script
		string newFilePath = Path.Combine(savePath, $"{newClassName}.cs");
		File.WriteAllText(newFilePath, contents);

		// overwrite the fileName and menuName in the CreateAssetMenu attribute
		string fileName = hasPrefix ? effectName.Replace("CE_", string.Empty) : effectName;
		string niceName = fileName.Replace("CE_", string.Empty);
		string menuName = $"Combos/{niceName}";

		// set the 'order' in the CreateAssetMenu attribute based on existing scripts
		string[] existingFiles = Directory.GetFiles(savePath, "CE_*.cs");
		if (existingFiles.Length == 0) Logger.LogError($"No existing ComboEffect scripts found in {savePath}. This should never happen since we just created one from a template.", null, "ScriptWriter");
		int newOrder = existingFiles.Length + 1;
		
		string createAssetMenuAttribute = $"[CreateAssetMenu(fileName = \"{niceName}\", menuName = \"{menuName}\", order = {newOrder})]";
		contents = contents.Replace($"[CreateAssetMenu(fileName = \"{templateName}\", menuName = \"Combos/{templateName}\", order = 0)]", createAssetMenuAttribute);

		File.WriteAllText(newFilePath, contents);

		AssetDatabase.ImportAsset(newFilePath);
		AssetDatabase.Refresh();
	}
}
