#region
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GUILayout;
#endregion

namespace Lumina.Debugging
{
[EditorWindowTitle(title = "Debug", icon = "Debug.png")]
public class DebugWindow : EditorWindow
{
    static Action activeMenu;
    static bool windowsFoldout;
    static bool optionsFoldout = true;
    static bool addedScenesFoldout;
    static bool manageScenesFoldout = true;
    static bool otherToolsFoldout = true;
    static bool commandsFoldout;
    static bool commandsListFoldout = true;
    static string searchQuery = string.Empty;
    static int maxPingedAssets = 20;
    static bool isSettingMaxPingedAssets;
    static string commandQuery = string.Empty;
    static bool showTextures;

    readonly static Dictionary<string, string> commandDictionary = new ()
    { { "help", "Shows the list of available commands." },
      { "refuel", "Refuels the vehicle." },
      { "repair", "Repairs the vehicle." },
      { "recharge", "Recharges the vehicle." },
      { "null", "Does nothing." } };

    readonly static List<string> addedScenes = new ();

    static Vector2 scrollPosition;

    static DateTime lastDebugLogTime = DateTime.MinValue;

    void OnEnable()
    {
        Initialize();
        EditorApplication.playModeStateChanged += PlayModeState;

        return;

        void Initialize() => activeMenu = DefaultMenu;
    }

    void OnDisable()
    {
        Terminate();

        return;

        void Terminate()
        {
            // Clear the added scenes list.
            addedScenes.Clear();

            // Clear the search query.
            searchQuery = string.Empty;
            commandQuery = string.Empty;

            // Remove the play mode state changed event.
            EditorApplication.playModeStateChanged -= PlayModeState;
        }
    }

    void OnGUI() => activeMenu();

    [MenuItem("Tools/Lumina/Debug Window")]
    public static void ShowWindow()
    {
        // Dock next to inspector. Find the inspector window using reflection
        Type desiredDockNextTo = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
        var window = GetWindow<DebugWindow>("Debug", true, desiredDockNextTo);

        // Set the icon. The icon is found in the Icons folder. Name of icon is "Debug.png"
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Debugging/Icons/Debug.png");
        window.titleContent.image = icon;
        window.minSize = new (350, 200);
        window.maxSize = window.minSize;

        window.Show();
    }

    #region Commands
    static void HelpCommand()
    {
        if (!commandsListFoldout) commandsListFoldout = true;
        else Logger.Log("Commands can be viewed from the commands list at the bottom of the" + nameof(DebugWindow));
    }
    #endregion

    #region GUI
    static void DefaultMenu()
    {
        // Default Menu is scrollable.
        using var scope = new ScrollViewScope(scrollPosition);
        scrollPosition = scope.scrollPosition;

        DrawTopBanner(); // Handles the "Load / Open Scenes" buttons as well as the "Back" button.
        DrawManageScenesFoldout();
        DrawMidBanner(); // Handles the "Other Tools" such as opening the other Editor Windows, Debug Options, as well as the Search and Ping Asset menu.
        DrawOtherToolsFoldout();
    }

    static void DrawTopBanner()
    {
        Space(10);

        using (new HorizontalScope())
        {
            // The amount of FlexibleSpace() calls are annoying and ugly, but they are necessary to center the "Load / Open Scenes" label.
            FlexibleSpace();
            FlexibleSpace();
            FlexibleSpace();

            bool isPlaying = Application.isPlaying;

            Label(isPlaying ? "Load Scene" : "Open Scene", EditorStyles.largeLabel);
            FlexibleSpace();

            DrawBackButton();
        }
    }

    static void DrawManageScenesFoldout()
    {
        Space(10);

        // Foldout that covers the majority of the Debug window.
        using (new VerticalScope("box"))
        {
            manageScenesFoldout = EditorGUILayout.Foldout(manageScenesFoldout, "Manage Scenes", true, EditorStyles.foldoutHeader);
            if (manageScenesFoldout) DrawSceneButtons();
        }
    }

    static void DrawSceneButtons() => DrawBasicSceneButtons();

    static void DrawBasicSceneButtons()
    {
        bool isPlaying = Application.isPlaying;

        Label(isPlaying ? "Runtime" : "Editor", EditorStyles.boldLabel);

        using (new VerticalScope("box"))
        {
            if (isPlaying) DrawSceneLoadButtons(LoadScene);
            else DrawSceneLoadButtons(OpenScene);
        }

        Space(10);
    }

    // unused in current project
    static void DrawCustomSceneButtons()
    {
        using (new VerticalScope("box"))
        {
            Label("Custom Scenes", EditorStyles.boldLabel);

            // Button to add a custom scene
            if (Button("Add Scene", Height(25)))
            {
                // Open Windows Explorer to select a scene
                string path = EditorUtility.OpenFilePanel("Select a scene", Application.dataPath, "unity");

                if (!string.IsNullOrEmpty(path))
                {
                    // Add the button
                    addedScenes.Add(path);
                    addedScenesFoldout = true;
                }
            }

            DrawAddedScenesFoldout();
        }
    }

    static void DrawAddedScenesFoldout()
    {
        addedScenesFoldout = EditorGUILayout.Foldout(addedScenesFoldout, "Added Scenes", true, EditorStyles.foldoutHeader);

        if (addedScenesFoldout && addedScenes.Count == 0)

            // Warning that there are no added scenes.
            EditorGUILayout.HelpBox("No scenes have been added.", MessageType.Warning, true);

        // Add a button for each added scene
        AddCustomScenes();
    }

    static void DrawSceneLoadButtons(Action<int> sceneAction)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (Button(sceneName, Height(30))) sceneAction(i);
        }
    }

    static void DrawMidBanner()
    {
        Space(10);

        using (new HorizontalScope())
        {
            FlexibleSpace();

            Label("Other Tools", EditorStyles.largeLabel);

            FlexibleSpace();
        }
    }

    static void DrawOtherToolsFoldout()
    {
        Space(10);

        using (new VerticalScope("box"))
        {
            otherToolsFoldout = EditorGUILayout.Foldout(otherToolsFoldout, "Other Tools", true, EditorStyles.foldoutHeader);
            if (otherToolsFoldout) DrawOtherTools();
        }
    }

    static void DrawOtherTools()
    {
        DrawSearchAndPingAssetMenu();
        DrawEditorWindowsMenu();
        DrawDebugOptionsMenu();
        DrawConsoleCommandMenu();
        DrawTexturesMenu();
    }

    static void DrawEditorWindowsMenu()
    {
        Space(10);

        using (new VerticalScope("box"))
        {
            windowsFoldout = EditorGUILayout.Foldout(windowsFoldout, "Editor Windows", true, EditorStyles.foldoutHeader);

            if (windowsFoldout) CreateButtonWithAction("Nothing to Display.", () => Logger.LogWarning("Nothing to display."));
        }
    }

    static bool simpleMode;

    static void DrawDebugOptionsMenu()
    {
        Space(10);

        using (new VerticalScope("box"))
        {
            optionsFoldout = EditorGUILayout.Foldout(optionsFoldout, "Debug Options", true, EditorStyles.foldoutHeader);

            if (optionsFoldout)
            {
                var simpleModeContent = new GUIContent("Simple Mode", "Enabling \"Simple Mode\" reduces the amount of options displayed in the Debug Window.");
                var enterPlaymodeOptionsContent = new GUIContent("Enter Playmode Options", "Enabling \"Enter Playmode Options\" improves Unity's workflow by significantly reducing the time it takes to enter play mode.");

                simpleMode = EditorGUILayout.Toggle(simpleModeContent, simpleMode);

                if (simpleMode) EditorSettings.enterPlayModeOptionsEnabled = EditorGUILayout.Toggle(enterPlaymodeOptionsContent, EditorSettings.enterPlayModeOptionsEnabled);
                else EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions) EditorGUILayout.EnumPopup(enterPlaymodeOptionsContent, EditorSettings.enterPlayModeOptions, EditorStyles.popup);
            }
        }
    }

    static void DrawSearchAndPingAssetMenu()
    {
        Space(10);

        Label("Asset Search", EditorStyles.boldLabel);

        // Search bar
        using var scope = new HorizontalScope("box");

        Label("Search", Width(50));
        searchQuery = TextField(searchQuery, Height(25));

        if (Button("Search", Width(100), Height(25)))
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                Logger.LogWarning("Search query is empty.");
                return;
            }

            if (searchQuery.Equals("Help", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Type a search query to find an asset. \nIf there are too many assets found, the editor will only ping the first {maxPingedAssets} assets.");
                Logger.Log(@"Type \\""\Settings\"" to change the maxPingedAssets.");
                searchQuery = string.Empty;
                return;
            }

            if (ChangeSettingsCommand) return;

            string[] guids = QueryGUIDs;

            switch (guids.Length)
            {
                case 0:
                    Logger.LogWarning("No assets found. \nPlease check your search query.");
                    break;

                case > 1:
                    // Show a warning popup if more than one asset is found and ask the user if they want to ping all assets or only the exact match (if found).
                    int pingExact = EditorUtility.DisplayDialogComplex
                        ("Multiple Assets Found", "More than one asset found. Would you like to ping all found assets or only the exact match (if found)?", "Exact Match", "All Assets", "Cancel");

                    switch (pingExact)
                    {
                        case 0:
                            FindAndPingExactAsset(guids);
                            break;

                        case 1:
                            FindAndLogAllAssets(guids);
                            break;

                        case 2:
                            Logger.LogWarning("Asset search cancelled.");
                            searchQuery = string.Empty;
                            return;
                    }

                    break;

                default: // Pings the asset if only one asset is found.
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0])));
                    break;
            }
        }
    }

    static bool ChangeSettingsCommand
    {
        get
        {
            if (isSettingMaxPingedAssets)
            {
                if (int.TryParse(searchQuery, out int newMax))
                {
                    maxPingedAssets = newMax;
                    isSettingMaxPingedAssets = false;

                    Logger.Log($"Max pinged assets set to {maxPingedAssets}.");
                    searchQuery = string.Empty;
                    return true;
                }

                isSettingMaxPingedAssets = EditorUtility.DisplayDialog("Settings", "Enter a number to be the new maximum amount of pinged assets.", "OK", "Cancel");
                if (!isSettingMaxPingedAssets) Logger.LogWarning("Settings command cancelled.");
                return true;
            }

            if (searchQuery.Equals(@"\\Settings", StringComparison.OrdinalIgnoreCase))
            {
                isSettingMaxPingedAssets = EditorUtility.DisplayDialog("Settings", "Enter a number to be the new maximum amount of pinged assets.", "OK", "Cancel");

                if (!isSettingMaxPingedAssets) Logger.LogWarning("Settings command cancelled.");
                else searchQuery = string.Empty;

                return true;
            }

            return false;
        }
    }

    static void DrawConsoleCommandMenu()
    {
        Space(10);

        using (new VerticalScope("box"))
        {
            Label("Console Commands", EditorStyles.boldLabel);

            commandsFoldout = EditorGUILayout.Foldout(commandsFoldout, "Button Commands", true, EditorStyles.foldoutHeader);

            if (commandsFoldout) CreateButtonWithAction("Reset PlayerPrefs", PlayerPrefs.DeleteAll);

            Space(10);

            Label("Custom Command", EditorStyles.boldLabel);

            using (new HorizontalScope())
            {
                commandQuery = TextField(commandQuery, Height(25));
                if (Button("Execute", Width(100), Height(25)) && CommandExecuted()) return;
            }

            commandsListFoldout = EditorGUILayout.Foldout(commandsListFoldout, "Console Commands", true, EditorStyles.foldoutHeader);

            if (commandsListFoldout)

                // Text for the commands.
                foreach (KeyValuePair<string, string> command in commandDictionary) { Label($"{command.Key} - {command.Value}", EditorStyles.wordWrappedMiniLabel); }
        }

        return;

        bool CommandExecuted()
        {
            if (!string.IsNullOrEmpty(commandQuery)) ExecuteCommand();
            else Logger.LogWarning("Command is empty.");

            return false;
        }

        void ExecuteCommandInPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ExecuteCommand();
                EditorApplication.playModeStateChanged -= ExecuteCommandInPlayMode;
            }
        }

        void ExecuteCommand()
        {
            string cmd = commandDictionary.Keys.FirstOrDefault(key => key.Equals(commandQuery.ToLower()));

            if (cmd == null)
            {
                Debug.LogWarning($"Command \"{commandQuery}\" not recognized." + "\n");
                return;
            }

            string message = $"Executing command: \"{cmd}\"" + "\n" + commandDictionary[cmd];
            bool canExecute = !Application.isPlaying && cmd != "help";

            if (canExecute)
            {
                const string msg = "Can't execute a command while not in play mode. \nWould you like to enter playmode and execute the command?:";

                if (EditorUtility.DisplayDialog("Error", msg, "OK", "Cancel"))
                {
                    EditorApplication.EnterPlaymode();
                    EditorApplication.playModeStateChanged += ExecuteCommandInPlayMode;
                }
            }

            switch (cmd.ToLower())
            {
                case var command when command.Contains("help"):
                    HelpCommand();
                    break;

                default:
                    Debug.LogWarning($"Command + \"{commandQuery}\" not recognized.");
                    break;
            }

            commandQuery = string.Empty;
        }
    }

    static void DrawTexturesMenu()
    {
        using (new VerticalScope("box"))
        {
            showTextures = EditorGUILayout.Foldout(showTextures, "Textures", true, EditorStyles.foldoutHeader);

            if (showTextures)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
                foreach (GUIContent content in EditorTextures.Textures.Select(texture => new GUIContent(texture.Key, texture.Value))) EditorGUILayout.LabelField(content);
                EditorGUI.indentLevel--;
            }
        }
    }
    #endregion

    #region Utility
    void PlayModeState(PlayModeStateChange state)
    { // Repaint the window when entering play mode.
        if (state == PlayModeStateChange.EnteredPlayMode) Repaint();
    }

    static void DrawBackButton()
    {
        const int pixels = 52; // The width of the button. "52" is the exact width to center the "Other Tools" label.

        using (new HorizontalScope())
        {
            FlexibleSpace();

            if (Button("Back", Width(pixels)))
            {
                Debug.LogWarning("Back button is not yet implemented.");

                // -- End --
                activeMenu = DefaultMenu;
            }
        }
    }

    static void CreateButtonWithAction(string buttonText, Action action)
    {
        if (Button(buttonText, Height(30))) action();
    }

    static void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
        Debug.LogWarning("Loaded a scene using the debug menu! \nThe scene might not behave as expected.");
    }

    static void OpenScene(int sceneIndex)
    {
        // Get the scene path by the build index.
        string path = SceneUtility.GetScenePathByBuildIndex(sceneIndex);

        // Prompt to save the scene before opening a new one.
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            if ((DateTime.Now - lastDebugLogTime).TotalHours >= 2)
            {
                Logger.LogWarning("Opened a scene using the debug menu!");
                lastDebugLogTime = DateTime.Now;
            }
        }
        else { Logger.LogWarning("Scene Load Cancelled."); }
    }

    static void AddCustomScenes()
    {
        foreach (string scenePath in addedScenes)
        {
            // derive sceneName from path
            string sceneName = scenePath[(scenePath.LastIndexOf('/') + 1)..];
            sceneName = sceneName[..^6];

            if (Button(sceneName, Height(30)) && addedScenesFoldout)
            {
                if (!Application.isPlaying)
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Debug.LogWarning("Loaded a scene using the debug menu!\nThe scene might not behave as expected.");
                }
                else { Debug.LogWarning("Can't load a scene that isn't in the build settings."); }
            }
        }
    }

    // -- Asset Database --

    static string[] QueryGUIDs
    {
        get
        {
            string searchFilter;

            // Check if searchQuery has quotes for an exact match, else perform a loose match.
            if (searchQuery.StartsWith('"') && searchQuery.EndsWith('"')) searchFilter = "t:Object " + searchQuery;
            else searchFilter = "t:Object \"" + searchQuery + "\"";

            string[] guids = AssetDatabase.FindAssets(searchFilter);
            return guids;
        }
    }

    static void FindAndPingExactAsset(IEnumerable<string> strings)
    {
        //Find the exact match if any
        foreach (string guid in strings)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName.Equals(searchQuery))
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
                return;
            }
        }

        //If we are here it means exact match was not found
        Debug.LogWarning($"No single asset with the exact name found for '{searchQuery}'. \nPlease check your search query.");
    }

    static void FindAndLogAllAssets(IReadOnlyCollection<string> guids)
    {
        foreach (string assetGUID in guids)
        {
            if (guids.Count > maxPingedAssets)
            {
                Debug.LogWarning("Too many assets found. \nPlease narrow your search query. \nThis is done to prevent the editor from crashing.");
                break;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            Debug.Log($"Pinging asset: {assetName}" + $"\n{assetPath}", AssetDatabase.LoadMainAssetAtPath(assetPath));
        }
    }
    #endregion
}
}
