#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NoodledEvents;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class UltNoodleSearchWindow : EditorWindow
{
    private readonly Type[] _cookbookTypes = new Type[]
    {
        typeof(CommonsCookBook),
        typeof(LoopsCookBook),
        typeof(ObjectFieldCookBook),
        typeof(ObjectMethodCookBook),
        typeof(StaticMethodCookBook)
    };

    public static bool IsSearchOpen => _activeWindow != null;
    private static UltNoodleSearchWindow _activeWindow;

    public bool IsFocused => focusedWindow == this;

    private VisualElement _searchMenu;
    private TextField _searchBar;
    private Label _searchText;
    private ScrollView _searchedTypes;

    private Button _settingsButton;
    private VisualElement _settingsMenu;

    private int _curSearchProcess = 0;
    private Vector2 _pendingScreenPos;

    private List<CookBook.NodeDef> FilteredNodeDefs = new();
    private Dictionary<Type, bool> BookFilters = new();

    public static void ForceClose()
    {
        _activeWindow?.Close();
        _activeWindow = null;

        if (UltNoodleEditor.Editor == null || UltNoodleEditor.Editor.TreeView == null) return;
        UltNoodleEditor.Editor.TreeView.PendingEdgeOriginPort = null; // either we made a connection or cancelled, clear this
    }

    public static UltNoodleSearchWindow Open(UltNoodleTreeView graphView, Vector2 screenPos)
    {
        return InternalOpen(graphView, screenPos, null);
    }

    public static UltNoodleSearchWindow Open(UltNoodleTreeView graphView, Vector2 screenPos, Edge edge)
    {
        if (edge.output.userData is not NoodleDataOutput dataOut) return Open(graphView, screenPos); // fallback to generic search
        return InternalOpen(graphView, screenPos, dataOut.Type);
    }

    private static UltNoodleSearchWindow InternalOpen(UltNoodleTreeView graphView, Vector2 screenPos, Type filterType)
    {
        _activeWindow?.Close();
        _activeWindow = null;

        var window = CreateInstance<UltNoodleSearchWindow>();
        _activeWindow = window;
        window._pendingScreenPos = screenPos;
        window.position = new Rect(-10000, -10000, 10, 10); // hide until uxml loads and we can size properly
        if (filterType != null)
            window.SetSearchFilter(true, filterType);
        else
            window.FilteredNodeDefs = UltNoodleEditor.AllNodeDefs;
        window.ShowPopup();
        return window;
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleSearchWindow.uxml");
        visualTree.CloneTree(root);

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{UltNoodleEditor.EditorFolder}/Styles/UltNoodleSearchWindow.uss");
        root.styleSheets.Add(styleSheet);

        _searchMenu = root.Q<VisualElement>("SearchMenu");
        _searchBar = _searchMenu.Q<TextField>("SearchBar");
        _searchText = _searchMenu.Q<Label>("SearchText");
        _searchedTypes = _searchMenu.Q<ScrollView>("SearchedTypes");

        _settingsButton = _searchMenu.Q<Button>("SettingsButton");
        _settingsMenu = _searchMenu.Q<VisualElement>("SettingsMenu");

        // handle setting to mouse position after size is known
        root.schedule.Execute(() =>
            {
                float w = _searchMenu.resolvedStyle.width;
                float h = _searchMenu.resolvedStyle.height;

                if (w > 0 && h > 0)
                {
                    minSize = maxSize = new Vector2(w, h);
                    position = new Rect(_pendingScreenPos, new Vector2(w, h));
                }
            });

        // search bar callbacks
        _searchBar.RegisterValueChangedCallback((txt) =>
        {
            if (EditorPrefs.GetBool("SearchPerChar", true))
                SearchTypes(10);
        });
        _searchBar.RegisterCallback<KeyDownEvent>((evt) =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                SearchTypes(100);
            }
        }, TrickleDown.TrickleDown);

        // settings button
        _settingsButton.clicked += () =>
        {
            _settingsMenu.visible = !_settingsMenu.visible;
        };

        // settings
        Toggle spcToggle = new Toggle("Search Per Character");
        spcToggle.value = EditorPrefs.GetBool("SearchPerChar", true);
        spcToggle.RegisterValueChangedCallback((evt) =>
        {
            EditorPrefs.SetBool("SearchPerChar", evt.newValue);
        });
        _settingsMenu.Add(spcToggle);

        foreach (var cbType in _cookbookTypes)
        {
            var cbToggle = new Toggle(cbType.Name.Replace("CookBook", ""));
            cbToggle.value = EditorPrefs.GetBool($"Search{cbType.Name}", true);
            BookFilters[cbType] = cbToggle.value;
            cbToggle.RegisterValueChangedCallback((evt) =>
            {
                EditorPrefs.SetBool($"Search{cbType.Name}", evt.newValue);
                BookFilters[cbType] = evt.newValue;
                SearchTypes(100);
            });
            _settingsMenu.Add(cbToggle);
        }

        _searchBar.Focus();
        SearchTypes(25);
    }

    public void OnLostFocus()
    {
        ForceClose();
    }

    private void SetSearchFilter(bool pinIn, Type t)
    {
        // lets cache the searchables

        // reset FilteredNodeDefs
        if (FilteredNodeDefs == UltNoodleEditor.AllNodeDefs)
            FilteredNodeDefs = new();
        else
            FilteredNodeDefs.Clear();

        foreach (var node in UltNoodleEditor.AllNodeDefs)
        {
            try
            {
                foreach (var pin in pinIn ? node.Inputs : node.Outputs)
                {
                    if (pin.Flow) continue;

                    if ((pinIn ? pin.Type : t).IsAssignableFrom(pinIn ? t : pin.Type))
                    {
                        FilteredNodeDefs.Add(node);
                        break;
                    }
                }
            }
            catch (TypeLoadException) { /* ignore evil types */ }
        }

        FilteredNodeDefs.Sort((a, b) =>
        {
            var aTs = pinIn ? a.Inputs : a.Outputs;
            var bTs = pinIn ? b.Inputs : b.Outputs;
            bool aHasT = aTs.Any(p => p.Type == t);
            bool bHasT = bTs.Any(p => p.Type == t);
            if (aHasT && !bHasT) return -1;
            if (aHasT == bHasT) return 0;
            if (bHasT && !aHasT) return 1;
            throw new NotImplementedException();
        });
    }

    private void SearchTypes(int findNum)
    {
        EditorApplication.CallbackFunction newSearch = null;
        _curSearchProcess++;
        int thisSearchNum = _curSearchProcess;
        int dispNum = findNum;
        _searchedTypes.Clear();
        string targetSearch = _searchBar.value;
        string[] splitResults = null;
        int j = 0;
        bool firstRun = true;
        if (!targetSearch.StartsWith(".") && targetSearch.Contains("."))
        {
            splitResults = targetSearch.Split('.');
            targetSearch = splitResults[0] + ".";
        }

        // To be replaced with some better comparison algorithm.
        bool CompareString(string stringOne, string stringTwo)
        {
            return stringOne.Contains(stringTwo, StringComparison.CurrentCultureIgnoreCase);
        }

        void EndSearch()
        {
            _searchText.style.display = DisplayStyle.None;
            EditorApplication.update -= newSearch;
        }

        newSearch = () =>
        {
            if (_curSearchProcess != thisSearchNum)
            {
                EndSearch();
                return;
            }
            if (firstRun)
            {
                firstRun = false;
                _searchText.style.display = DisplayStyle.Flex;
            }

            // Collect first x that match
            int i = 0;
            for (; j < FilteredNodeDefs.Count; j++)
            {
                CookBook.NodeDef nd = FilteredNodeDefs[j];
                i++;
                _searchText.text = "(" + Mathf.RoundToInt(Mathf.Clamp(j * 100 / (float)FilteredNodeDefs.Count, 0, 100)) + "%)";

                if (i > 1000)
                    break; // next loop pls

                if (dispNum <= 0)
                {
                    _searchedTypes.Add(GetIncompleteListDisplay());
                    EndSearch();
                    return;
                }
                if (!BookFilters[nd.CookBook.GetType()]) continue;

                // Primary filter, either strict startswith or loose compare
                if (nd.Name.StartsWith(targetSearch, StringComparison.CurrentCultureIgnoreCase))
                {
                    // Secondary filter, second part compare check
                    if ((splitResults != null) && !CompareString(nd.Name, splitResults[1]))
                        continue;

                    dispNum--;
                    _searchedTypes.Add(nd.SearchItem);
                }
            }

            if (j >= FilteredNodeDefs.Count - 1)
            {
                EndSearch();
            }
        };
        EditorApplication.update += newSearch;
        newSearch.Invoke();
    }
    
    private VisualElement GetIncompleteListDisplay()
    {
        var o = new Label() {
            text = "Press Enter for a Full Search! (...)"
        };

        o.style.alignContent = Align.Center;
        o.style.alignSelf = Align.Center;
        o.style.unityTextAlign = TextAnchor.MiddleCenter;

        return o;
    }
}
#endif
