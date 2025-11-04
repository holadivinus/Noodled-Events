#if UNITY_EDITOR
using NoodledEvents;
using SLZ.Marrow.Warehouse;
using SLZ.Marrow.Zones;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using UltEvents;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UltNoodleEditor : EditorWindow
{
    [MenuItem("NoodledEvents/Noodle Editor")]
    public static void OpenWindow()
    {
        UltNoodleEditor wnd = GetWindow<UltNoodleEditor>();
        wnd.titleContent = new GUIContent("Scene Noodle Editor");
    }

    [SerializeField] public CookBook CommonsCookBook;
    [SerializeField] public CookBook StaticCookBook;
    [SerializeField] public CookBook ObjectCookBook;
    [SerializeField] public CookBook ObjectFCookBook;
    [SerializeField] public CookBook LoopsCookBook;
    public static CookBook[] AllBooks;
    public static List<CookBook.NodeDef> AllNodeDefs = new();
    public static UltNoodleEditor Editor;

    public static string BaseFolder => InPackage() ? "Packages/com.holadivinus.noodledevents/" : "Assets/Noodled-Events/";
    public static string EditorFolder => BaseFolder + "Editor/";

    private static bool InPackage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
        return packageInfo != null;
    }

    async void GetRequest(string url, Action<string> response)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue() { NoCache = true };
            var resp = await client.GetAsync(url);
            var stringGet = await resp.Content.ReadAsStringAsync();
            response.Invoke(stringGet);
        }
    }

    public UltNoodleTreeView TreeView => treeView;
    
    private UltNoodleTreeView treeView;
    private UltNoodleInspectorView inspectorView;
    private UltNoodleBowlSelector bowlSelector;
    private SerializedBowl _bowlToReselect = null;

    public void CreateGUI()
    {
        Editor = this;

        VisualElement root = rootVisualElement;

        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{EditorFolder}/Styles/UltNoodleEditor.uxml");
        visualTree.CloneTree(root);

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{EditorFolder}/Styles/UltNoodleEditor.uss");
        root.styleSheets.Add(styleSheet);

        EditorApplication.update += OnUpdate;
        void contextChanged(bool resetViews = true)
        {
            if (resetViews)
                ResetViews();
            OnFocus();
        }
        EditorSceneManager.sceneOpened += (_, _) => contextChanged();
        PrefabStage.prefabStageOpened += (_) => contextChanged();
        PrefabStage.prefabStageClosing += (_) => contextChanged(); // described as "Prefab stage is about to be opened" in docs but functions as "Prefab stage is closed"
        Selection.selectionChanged += () => contextChanged(EditorPrefs.GetBool("SelectedBowlsOnly", true));

        treeView = root.Q<UltNoodleTreeView>();
        inspectorView = root.Q<UltNoodleInspectorView>();
        bowlSelector = root.Q<UltNoodleBowlSelector>();
        LoadingText = root.Q<Label>(nameof(LoadingText));

        treeView.InitializeSearch(this);
        treeView.OnNodeSelected += (nodeView) => inspectorView.UpdateSelection(nodeView);

        bowlSelector.AttachToEditor(this);

        var mainSplit = root.Q<TwoPaneSplitView>("MainSplit");
        var leftSplit = root.Q<TwoPaneSplitView>("LeftSplit");

        // setup toolbar options
        var nodesMenu = root.Q<ToolbarMenu>("NodesMenu");
        var viewMenu = root.Q<ToolbarMenu>("ViewMenu");
        var compilationMenu = root.Q<ToolbarMenu>("CompilationMenu");
        var helpMenu = root.Q<ToolbarMenu>("HelpMenu");

        nodesMenu.menu.AppendAction("Regenerate Nodes", (a) => CollectNodes(), (a) => _collecting ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

        void UpdateSplit() => UpdateLeftSplitVisibility(leftSplit, mainSplit, inspectorView.visible, bowlSelector.visible);
        viewMenu.menu.AppendAction("Inspector", (a) => { inspectorView.visible = !inspectorView.visible; UpdateSplit(); }, (a) => inspectorView.visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        viewMenu.menu.AppendAction("Bowl Selector", (a) => { bowlSelector.visible = !bowlSelector.visible; UpdateSplit(); }, (a) => bowlSelector.visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        viewMenu.menu.AppendAction("Grid Background", (a) => treeView.ToggleGrid(), (a) => treeView.GridEnabled ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        viewMenu.menu.AppendAction("Selected Bowls Only", (a) =>
        {
            bool enabled = !EditorPrefs.GetBool("SelectedBowlsOnly", true);
            EditorPrefs.SetBool("SelectedBowlsOnly", enabled);
            _bowlToReselect = !enabled || Selection.activeGameObject == _currentBowl?.SerializedData?.gameObject
                ? _currentBowl?.SerializedData
                : null; // only reselect if we're disabling the toggle or the current bowl's gameobject is selected
            
            ResetViews();
            OnFocus(); // update displays
        }, (a) => EditorPrefs.GetBool("SelectedBowlsOnly", true) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        viewMenu.menu.AppendAction("Rebuild View", (a) => contextChanged(), (a) => DropdownMenuAction.Status.Normal);

        compilationMenu.menu.AppendAction("Add Debug Logs", (_) => 
            UltNoodleRuntimeExtensions.DEBUG_IN_COMP = !UltNoodleRuntimeExtensions.DEBUG_IN_COMP, 
            (_) => UltNoodleRuntimeExtensions.DEBUG_IN_COMP ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        compilationMenu.menu.AppendAction("Use Inline Ultswaps", (_) => 
        {
            bool enabled = !EditorPrefs.GetBool("InlineUltswaps");
            EditorPrefs.SetBool("InlineUltswaps", enabled);
        }, (_) => EditorPrefs.GetBool("InlineUltswaps") ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        helpMenu.menu.AppendAction("GitHub", (a) => Application.OpenURL("https://github.com/holadivinus/Noodled-Events"), (a) => DropdownMenuAction.Status.Normal);

        if (InPackage())
            CheckForUpdates(root.Q<Label>("NoticeLabel"),
                () => { helpMenu.menu.AppendAction("Update", (a) => TryUpdate(), (a) => DropdownMenuAction.Status.Normal); },
                () => { helpMenu.menu.AppendAction("Up to Date", (a) => { }, (a) => DropdownMenuAction.Status.Disabled); });

        if ((AllNodeDefs.Count == 0 || AllBooks == null) && !Application.isPlaying)
        {
            CollectNodes();
        }
        _created = true;
    }

    private void CheckForUpdates(Label noticeLabel, Action ifUpdate, Action ifUpToDate)
    {
        TextAsset packageData = AssetDatabase.LoadAssetAtPath<TextAsset>(BaseFolder + "package.json");
        string version = packageData.text.Split("\"version\": \"")[1].Split('"')[0];

        noticeLabel.text = "Checking for Updates...";
        GetRequest("https://raw.githubusercontent.com/holadivinus/Noodled-Events/refs/heads/main/package.json", (resp) =>
        {
            string remoteVersion = resp.Split("\"version\": \"")[1].Split('"')[0];
            Debug.Log(remoteVersion);
            if (new Version(remoteVersion) > new Version(version))
            {
                noticeLabel.text = $"Update available! ({remoteVersion})";
                ifUpdate.Invoke();
            }
            else
            {
                noticeLabel.text = "";
                ifUpToDate.Invoke();
            }
        });
    }

    private static bool _isUpdating;
    private void TryUpdate()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        var req = Client.Add("https://github.com/holadivinus/Noodled-Events.git");
        Debug.Log("Updating...");
        void check()
        {
            if (req.Error != null)
            {
                Debug.LogError("Failed to update: " + req.Error.message);
                _isUpdating = false;
                EditorApplication.update -= check;
            }
        }
        EditorApplication.update += check; // we're going to get reloaded if the update works, so this gets automatically detached
    }

    private Label LoadingText;
    private static bool _collecting;
    
    private static Type[] s_tps;
    public static Type[] SearchableTypes
    {
        get
        {
            if (s_tps == null)
            {
                s_tps = UltNoodleExtensions.GetAllTypes();
                EditorUtility.ClearProgressBar();
            }

            return s_tps;
        }
    }

    private void CollectNodes()
    {
        if (_collecting) return;
        _collecting = true;

        // Creates all NodeDefs, clearing the pre-existing ones
        AllNodeDefs.Clear();

        CollectBooks();

        Dictionary<CookBook, float> nodeProgression = new();
        foreach (var b in AllBooks)
            nodeProgression[b] = new();

        Debug.Log($"Loading nodes from {SearchableTypes.Length} Types!");

        LoadingText.text = $"!LOADING NODES!";

        foreach (var book in AllBooks)
        {
            var b = book;
            var list = nodeProgression[b];
            b.CollectDefs((newNodes, percentage) => MainThread.Enqueue(() =>
            {
                AllNodeDefs.AddRange(newNodes);
                nodeProgression[b] = percentage;
                LoadingText.text = $"!LOADING NODES! progress: [";
                foreach (var kvp in nodeProgression)
                    if (kvp.Value != 1)
                        LoadingText.text += $"{kvp.Key.name}: {Mathf.RoundToInt(kvp.Value * 100)}%, ";
                if (LoadingText.text.EndsWith(", "))
                    LoadingText.text = LoadingText.text[..^2];
                LoadingText.text += $"] | {AllNodeDefs.Count} Nodes Available.";
            }),
            () => MainThread.Enqueue(() =>
            {
                nodeProgression[b] = 1;
                if (nodeProgression.Values.All(p => p == 1))
                {
                    LoadingText.text = $"All {AllNodeDefs.Count} Nodes Loaded!";
                    _collecting = false;
                }
            }));
        }
        /*
        int cur = 0;
        CookBook book = AllBooks[cur];
        IEnumerator mover = null;
        int final = AllBooks.Count();

        EditorApplication.CallbackFunction loop = null;
        loop = () =>
        {
            //EditorUtility.DisplayProgressBar("Loading Noodle Editor...", book.name, (float)cur / final);

            if (mover == null)
                mover = book.CollectDefs(AllNodeDefs).GetEnumerator();

            bool joever = !mover.MoveNext();
            if (joever)
            {
                mover = null;
                cur++;
                if (cur == final)
                {
                    ResetSearchFilter();
                    EditorUtility.ClearProgressBar(); 
                    _collecting = false;
                    LoadingText.text = "";
                    return;
                }
                book = AllBooks[cur];
            }
            EditorApplication.delayCall += loop;
        };
        loop.Invoke();*/

    }
    private void CollectBooks()
    {
        AllBooks = null;

        var cookBooks = AssetDatabase.FindAssets("t:" + nameof(CookBook)).Select(guid => AssetDatabase.LoadAssetAtPath<CookBook>(AssetDatabase.GUIDToAssetPath(guid)));
        if (!cookBooks.Contains(CommonsCookBook)) cookBooks = cookBooks.Append(CommonsCookBook);
        if (!cookBooks.Contains(LoopsCookBook)) cookBooks = cookBooks.Append(LoopsCookBook);
        if (!cookBooks.Contains(ObjectCookBook)) cookBooks = cookBooks.Append(ObjectCookBook);
        if (!cookBooks.Contains(ObjectFCookBook)) cookBooks = cookBooks.Append(ObjectFCookBook);
        if (!cookBooks.Contains(StaticCookBook)) cookBooks = cookBooks.Append(StaticCookBook);
        AllBooks = cookBooks.ToArray();
    }

    private bool _created = false;

    public List<UltNoodleBowl> Bowls = new();
    private UltNoodleBowl _currentBowl;
    public Action OnBowlsChanged;
    public UltNoodleBowl CurrentBowl
    {
        get => _currentBowl;
        set => SelectBowl(value);
    }

    public void SelectBowl(UltNoodleBowl bowl)
    {
        if (bowl == null || !Bowls.Contains(bowl)) return;
        _currentBowl = bowl;
        _bowlToReselect = bowl.SerializedData;

        void onReady()
        {
            treeView.PopulateView(bowl);
            inspectorView.UpdateSelection(null);
            OnBowlsChanged?.Invoke();
        }

        if (!_created || treeView == null)
            EditorApplication.delayCall += onReady;
        else
            onReady();
    }

    private void OnFocus()
    {
        // on focus we refresh the nodes
        // lets get all ult event sources in the scene;
        // then, graph them out

        var curScene = SceneManager.GetActiveScene();

        foreach (var bowl in Bowls.ToArray())
            bowl.Validate();
        treeView?.Validate();

        // autogen bowlsUIs
        void ProcessBowls()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            IEnumerable<SerializedBowl> bowls = prefabStage?.prefabContentsRoot.GetComponentsInChildren<SerializedBowl>(true)
                            ?? Resources.FindObjectsOfTypeAll<SerializedBowl>();

            foreach (var bowl in bowls)
            {
                if (bowl == null || (prefabStage == null && bowl.gameObject.scene != curScene))
                    continue;

                bool isBowlNew = !Bowls.Any(b => b.SerializedData == bowl);
                bool isBowlSelected = Selection.gameObjects.Contains(bowl.gameObject)
                                    || Selection.transforms.Any(t => bowl.transform.IsChildOf(t))
                                    || !EditorPrefs.GetBool("SelectedBowlsOnly", true);
                bool isNotPartOfPrefab = !PrefabUtility.IsPartOfAnyPrefab(bowl);

                if (isBowlNew && isBowlSelected && isNotPartOfPrefab)
                {
                    bool shouldSelect = (_bowlToReselect == null || _bowlToReselect == bowl)
                            && (_currentBowl == null || !Bowls.Contains(_currentBowl));

                    NewBowl(bowl.EventHolder, bowl.BowlEvtHolderType, bowl.EventFieldPath, shouldSelect);

                    if (shouldSelect)
                        _bowlToReselect = null;
                }
            }

            Bowls.Sort((a, b) =>
            {
                Transform ta = a.SerializedData.transform;
                Transform tb = b.SerializedData.transform;

                // sort 1: hierarchy depth (shallower comes first)
                int depthA = GetDepth(ta);
                int depthB = GetDepth(tb);
                if (depthA != depthB)
                    return depthA.CompareTo(depthB);

                // sort 2: sibling index at this level
                int siblingCompare = ta.GetSiblingIndex().CompareTo(tb.GetSiblingIndex());
                if (siblingCompare != 0)
                    return siblingCompare;

                // sort 3: component order if same GameObject
                if (ta == tb)
                {
                    var compsA = ta.GetComponents<SerializedBowl>();
                    var compsB = tb.GetComponents<SerializedBowl>();

                    int indexA = Array.IndexOf(compsA, a.SerializedData);
                    int indexB = Array.IndexOf(compsB, b.SerializedData);

                    return indexA.CompareTo(indexB);
                }

                return 0;
            });

            OnBowlsChanged?.Invoke();
        }

        if (!_created)
            EditorApplication.delayCall += ProcessBowls;
        else
            ProcessBowls();

        int GetDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }
    }

    public UltNoodleBowl NewBowl(Component eventComponent, SerializedType fieldType, string eventField, bool select = true)
    {
        var existingBowl = Bowls.FirstOrDefault(b => b.SerializedData != null && b.Component == eventComponent && b.EventFieldPath == eventField);
        if (existingBowl != null)
        {
            if (select)
                SelectBowl(existingBowl);
            return existingBowl;
        }

        var newBowl = new UltNoodleBowl(this, eventComponent, fieldType, eventField);
        Bowls.Add(newBowl);

        if (select)
            SelectBowl(newBowl);
        else // SelectBowl already invokes OnBowlsChanged
            OnBowlsChanged?.Invoke();

        return newBowl;
    }

    #region Noodle Bowl Prompts
    [MenuItem("CONTEXT/UltEventHolder/Noodle Bowl", true)] static bool v1(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/UltEventHolder/Noodle Bowl")]
    static void BowlSingle(MenuCommand command)
    {
        if (Editor == null) OpenWindow();
        Editor.NewBowl((UltEventHolder)command.context, new SerializedType(typeof(UltEventHolder)), "_Event");
    }
    [MenuItem("CONTEXT/CrateSpawner/Noodle Bowl", true)] static bool v2(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/CrateSpawner/Noodle Bowl")]
    static void CrateBowl(MenuCommand command)
    {
        if (Editor == null) OpenWindow();
        Editor.NewBowl((CrateSpawner)command.context, new SerializedType(typeof(CrateSpawner)), "onSpawnEvent");
    }
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Awake()", true)] static bool v3(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Awake()")]
    static void LifeCycleEvents_Awake(MenuCommand command)
    {
        var targ = command.context as LifeCycleEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(LifeCycleEvents)), "_AwakeEvent");
        
    }
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Start()", true)] static bool v4(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Start()")]
    static void LifeCycleEvents_StartEvent(MenuCommand command)
    {
        var targ = command.context as LifeCycleEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(LifeCycleEvents)), "_StartEvent");
    }
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Enable()", true)] static bool v5(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Enable()")]
    static void LifeCycleEvents_EnableEvent(MenuCommand command)
    {
        var targ = command.context as LifeCycleEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(LifeCycleEvents)), "_EnableEvent");
    }
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Disable()", true)] static bool v6(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Disable()")]
    static void LifeCycleEvents_DisableEvent(MenuCommand command)
    {
        var targ = command.context as LifeCycleEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(LifeCycleEvents)), "_DisableEvent");    
    }
    
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Destroy()", true)] static bool v7(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/LifeCycleEvents/Noodle Bowl/Destroy()")]
    static void LifeCycleEvents_DestroyEvent(MenuCommand command)
    {
        var targ = command.context as LifeCycleEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(LifeCycleEvents)), "_DestroyEvent");
        
    }
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Update()", true)] static bool v8(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Update()")]
    static void UpdateEvents_UpdateEvent(MenuCommand command)
    {
        var targ = command.context as UpdateEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(UpdateEvents)), "_UpdateEvent");
    }
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Late Update()", true)] static bool v9(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Late Update()")]
    static void UpdateEvents_LateUpdateEvent(MenuCommand command)
    {
        var targ = command.context as UpdateEvents;
            if (Editor == null) OpenWindow();
            Editor.NewBowl(targ, new SerializedType(typeof(UpdateEvents)), "_LateUpdateEvent");
    }
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Fixed Update()", true)] static bool v10(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/UpdateEvents/Noodle Bowl/Fixed Update()")]
    static void UpdateEvents_FixedUpdateEvent(MenuCommand command)
    {
        var targ = command.context as UpdateEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(UpdateEvents)), "_FixedUpdateEvent");
    }
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Enter()", true)] static bool v11(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Enter()")]
    static void CollisionEvents3D_CollisionEnterEvent(MenuCommand command)
    {
        var targ = command.context as CollisionEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(CollisionEvents3D)), "_CollisionEnterEvent");
    }
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Stay()", true)] static bool v12(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Stay()")]
    static void CollisionEvents3D_CollisionStayEvent(MenuCommand command)
    {
        var targ = command.context as CollisionEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(CollisionEvents3D)), "_CollisionStayEvent");
    }
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Exit()", true)] static bool v13(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/CollisionEvents3D/Noodle Bowl/Collision Exit()")]
    static void CollisionEvents3D_CollisionExitEvent(MenuCommand command)
    {
        var targ = command.context as CollisionEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(CollisionEvents3D)), "_CollisionExitEvent");
    }
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Enter()", true)] static bool v14(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Enter()")]
    static void ZoneEvents_onZoneEnter(MenuCommand command)
    {
        var targ = command.context as ZoneEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(ZoneEvents)), "onZoneEnter");
    }
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Enter OneShot()", true)] static bool v15(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Enter OneShot()")]
    static void ZoneEvents_onZoneEnterOneShot(MenuCommand command)
    {
        var targ = command.context as ZoneEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(ZoneEvents)), "onZoneEnterOneShot");
    }
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Exit()", true)] static bool v16(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/ZoneEvents/Noodle Bowl/On Zone Exit()")]
    static void ZoneEvents_onZoneExit(MenuCommand command)
    {
        var targ = command.context as ZoneEvents;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(ZoneEvents)), "onZoneExit");
    }

    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Enter()", true)] static bool v17(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Enter()")]
    static void TriggerEvents3D_TriggerEnterEvent(MenuCommand command)
    {
        var targ = command.context as TriggerEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(TriggerEvents3D)), "_TriggerEnterEvent");
    }
    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Stay()", true)] static bool v18(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Stay()")]
    static void TriggerEvents3D_TriggerStayEvent(MenuCommand command)
    {
        var targ = command.context as TriggerEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(TriggerEvents3D)), "_TriggerStayEvent");
    }
    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Exit()", true)] static bool v19(MenuCommand command) => !PrefabUtility.IsPartOfAnyPrefab(command.context);
    [MenuItem("CONTEXT/TriggerEvents3D/Noodle Bowl/Trigger Exit()")]
    static void TriggerEvents3D_TriggerExitEvent(MenuCommand command)
    {
        var targ = command.context as TriggerEvents3D;
        if (Editor == null) OpenWindow();
        Editor.NewBowl(targ, new SerializedType(typeof(TriggerEvents3D)), "_TriggerExitEvent");
    }
    #endregion

    public void OnLostFocus()
    {
        // TODO: this doesn't handle focus going from search -> editor/outside noodle editor, not sure how important that would be
        if (UltNoodleSearchWindow.IsSearchOpen)
            return; // we don't need to compile if we're just switching to search

        foreach (var bowl in Bowls.ToArray())
        {
            if (bowl.SerializedData != null)
                bowl.SerializedData.Compile();
        }
        Debug.Log("Compiled!");
    }

    private void ResetViews()
    {
        _currentBowl = null;
        Bowls.Clear();

        OnBowlsChanged?.Invoke();
        treeView?.PopulateView(null);
        inspectorView?.UpdateSelection(null);
    }

    private void OnUpdate()
    {
        while (MainThread.TryDequeue(out Action act))
            act.Invoke();

        if (!EditorPrefs.GetBool("SelectedBowlsOnly", true))
        {
            foreach (var bowl in Bowls)
            {
                if (bowl == null
                    || bowl.SerializedData == null
                    || bowl.Component == null)
                {
                    Bowls.Remove(bowl);
                    if (_currentBowl == bowl)
                    {
                        _currentBowl = null;
                        inspectorView?.UpdateSelection(null);
                        treeView?.PopulateView(null);
                    }
                    OnBowlsChanged?.Invoke();
                    break; // we modified the collection, so we need to restart
                }
            }
        }
    }

    // for whatever ungodly reason, TwoPaneSplitViews don't let you uncollapse a single child, so we have to do this mess instead
    private void UpdateLeftSplitVisibility(TwoPaneSplitView leftSplit, TwoPaneSplitView mainSplit, bool topShouldBeVisible, bool bottomShouldBeVisible)
    {
        if (leftSplit == null || mainSplit == null)
            return;

        if (topShouldBeVisible && bottomShouldBeVisible)
        {
            // show both
            leftSplit.UnCollapse();
            mainSplit.UnCollapse();
            return;
        }

        if (!topShouldBeVisible && !bottomShouldBeVisible)
        {
            // hide entire left column
            mainSplit.CollapseChild(0); // assuming left panel is child 0 of mainSplit
            return;
        }

        // exactly one child should be visible:
        // make sure left column is visible first...
        leftSplit.UnCollapse();
        mainSplit.UnCollapse();

        // then collapse the OTHER child so only the desired one remains visible
        if (topShouldBeVisible)
        {
            // collapse bottom (child index 1)
            leftSplit.CollapseChild(1);
        }
        else // bottomShouldBeVisible
        {
            // collapse top (child index 0)
            leftSplit.CollapseChild(0);
        }
    }

    public static ConcurrentQueue<Action> MainThread = new();

    private void OnDestroy()
    {
        EditorApplication.update -= OnUpdate;
    }
}
#endif
