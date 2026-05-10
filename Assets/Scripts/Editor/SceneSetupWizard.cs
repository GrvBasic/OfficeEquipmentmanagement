#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using OEMS.Core;
using OEMS.UI;

namespace OEMS.Editor
{
    /// <summary>
    /// ONE-CLICK SETUP WIZARD.
    /// Menu: OEMS → Setup → Build Full Scene
    /// Builds an entire ready-to-play scene with Canvas, all panels,
    /// UIManager, DataManager, EventSystem, and complete button wiring.
    /// </summary>
    public class SceneSetupWizard : EditorWindow
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_NAME   = "MainScene.unity";

        // Color palette
        private static Color colorPrimary    = new Color(0.149f, 0.388f, 0.922f);  // blue
        private static Color colorPrimaryDk  = new Color(0.106f, 0.286f, 0.690f);
        private static Color colorAccent     = new Color(0.086f, 0.639f, 0.290f);  // green
        private static Color colorWarn       = new Color(0.918f, 0.345f, 0.047f);  // orange
        private static Color colorBgLight    = new Color(0.965f, 0.969f, 0.976f);
        private static Color colorBgPanel    = new Color(1f, 1f, 1f);
        private static Color colorBgHeader   = new Color(0.149f, 0.388f, 0.922f);
        private static Color colorTextDark   = new Color(0.114f, 0.157f, 0.231f);
        private static Color colorTextLight  = Color.white;
        private static Color colorBorder     = new Color(0.875f, 0.886f, 0.910f);

        [MenuItem("OEMS/Setup/Build Full Scene", priority = 1)]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                "OEMS Scene Setup",
                "This will create a new scene at 'Assets/Scenes/MainScene.unity' with the complete UI of the Office Equipment Management System.\n\nAny existing MainScene will be overwritten.\n\nProceed?",
                "Build Scene", "Cancel"))
                return;

            // Make sure folders exist
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // New empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Build root objects
            BuildEventSystem();
            var canvasRoot = BuildCanvas();
            var dataMgrGO  = BuildDataManager();
            var uiMgrGO    = BuildUIManagerObject();

            // Header bar (top of every screen)
            BuildHeaderBar(canvasRoot);

            // All panels
            var dashboard    = BuildDashboardPanel(canvasRoot);
            var registration = BuildRegistrationPanel(canvasRoot);
            var inventory    = BuildInventoryPanel(canvasRoot);
            var assignment   = BuildAssignmentPanel(canvasRoot);
            var returnPnl    = BuildReturnPanel(canvasRoot);
            var viewEmps     = BuildListViewPanel(canvasRoot, "ViewEmployeesPanel", "Employees");
            var viewInv      = BuildListViewPanel(canvasRoot, "ViewInventoryPanel", "Inventory");
            var viewAsn      = BuildListViewPanel(canvasRoot, "ViewAssignmentsPanel", "Assignments");

            // Toast (sits above everything)
            var toast = BuildToast(canvasRoot);

            // Wire UIManager
            var uim = uiMgrGO.GetComponent<UIManager>();
            uim.dashboardPanel        = dashboard;
            uim.registrationPanel     = registration;
            uim.inventoryPanel        = inventory;
            uim.assignmentPanel       = assignment;
            uim.returnPanel           = returnPnl;
            uim.viewEmployeesPanel    = viewEmps;
            uim.viewInventoryPanel    = viewInv;
            uim.viewAssignmentsPanel  = viewAsn;
            uim.toastPanel            = toast.gameObject;
            uim.toastText             = toast.GetComponentInChildren<TextMeshProUGUI>();

            // Save scene
            string scenePath = SCENE_FOLDER + "/" + SCENE_NAME;
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to build settings
            AddSceneToBuildSettings(scenePath);

            EditorUtility.DisplayDialog(
                "OEMS Setup Complete",
                "Scene built successfully at:\n" + scenePath +
                "\n\n• Press Play to test the application.\n" +
                "• On Android, the data file is saved to Application.persistentDataPath/oems_data.txt.\n\n" +
                "Use OEMS → Tools → Open Persistent Data Folder to inspect the saved file.",
                "OK");
        }

        // ============================================================
        // ROOT OBJECTS
        // ============================================================
        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static GameObject BuildCanvas()
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // portrait phone
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            // Solid background image behind everything
            var bg = CreateUIObject("Background", go.transform);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = colorBgLight;
            FillParent(bg.GetComponent<RectTransform>());

            return go;
        }

        private static GameObject BuildDataManager()
        {
            var go = new GameObject("DataManager");
            go.AddComponent<DataManager>();
            return go;
        }

        private static GameObject BuildUIManagerObject()
        {
            var go = new GameObject("UIManager");
            go.AddComponent<UIManager>();
            return go;
        }

        // ============================================================
        // HEADER BAR
        // ============================================================
        private static GameObject BuildHeaderBar(GameObject parent)
        {
            var header = CreateUIObject("Header", parent.transform);
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 160);
            var img = header.AddComponent<Image>();
            img.color = colorBgHeader;

            var title = CreateUIObject("HeaderText", header.transform);
            var trt = title.GetComponent<RectTransform>();
            FillParent(trt);
            var t = title.AddComponent<TextMeshProUGUI>();
            t.text = "Office Equipment Management System";
            t.fontSize = 36;
            t.fontStyle = FontStyles.Bold;
            t.color = colorTextLight;
            t.alignment = TextAlignmentOptions.Center;

            return header;
        }

        // ============================================================
        // DASHBOARD PANEL
        // ============================================================
        private static GameObject BuildDashboardPanel(GameObject parent)
        {
            var panel = CreateScreenPanel(parent, "DashboardPanel");
            var dash = panel.AddComponent<DashboardController>();

            // Stats container
            var stats = CreateUIObject("StatsContainer", panel.transform);
            var srt = stats.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0, -40);
            srt.sizeDelta = new Vector2(-80, 700);
            var grid = stats.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(480, 200);
            grid.spacing = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            dash.employeeCountText = CreateStatCard(stats.transform, "EMPLOYEES", colorPrimary);
            dash.itemTypesText     = CreateStatCard(stats.transform, "ITEM TYPES", colorPrimary);
            dash.availableQtyText  = CreateStatCard(stats.transform, "AVAILABLE",  colorAccent);
            dash.assignedQtyText   = CreateStatCard(stats.transform, "ASSIGNED",   colorPrimaryDk);
            dash.damagedQtyText    = CreateStatCard(stats.transform, "DAMAGED",    colorWarn);
            dash.consumedQtyText   = CreateStatCard(stats.transform, "CONSUMED",   new Color(0.42f, 0.45f, 0.50f));

            // Buttons grid
            var btnContainer = CreateUIObject("ButtonContainer", panel.transform);
            var brt = btnContainer.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0, 60);
            brt.sizeDelta = new Vector2(-80, 800);
            var bg = btnContainer.AddComponent<GridLayoutGroup>();
            bg.cellSize = new Vector2(480, 180);
            bg.spacing = new Vector2(20, 20);
            bg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            bg.constraintCount = 2;
            bg.childAlignment = TextAnchor.UpperCenter;

            dash.registerEmployeeButton  = CreateNavButton(btnContainer.transform, "Register Employee",  colorPrimary);
            dash.addInventoryButton      = CreateNavButton(btnContainer.transform, "Add Inventory",      colorPrimary);
            dash.assignItemButton        = CreateNavButton(btnContainer.transform, "Assign Item",        colorAccent);
            dash.returnItemButton        = CreateNavButton(btnContainer.transform, "Return Item",        colorWarn);
            dash.viewEmployeesButton     = CreateNavButton(btnContainer.transform, "View Employees",     colorPrimaryDk);
            dash.viewInventoryButton     = CreateNavButton(btnContainer.transform, "View Inventory",     colorPrimaryDk);
            dash.viewAssignmentsButton   = CreateNavButton(btnContainer.transform, "View Assignments",   colorPrimaryDk);

            return panel;
        }

        private static TextMeshProUGUI CreateStatCard(Transform parent, string label, Color tint)
        {
            var card = CreateUIObject(label + "Card", parent);
            var img = card.AddComponent<Image>();
            img.color = colorBgPanel;
            var outline = card.AddComponent<Outline>();
            outline.effectColor = colorBorder;
            outline.effectDistance = new Vector2(1, -1);

            // Strip on left for color
            var strip = CreateUIObject("Strip", card.transform);
            var srt = strip.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(8, 0);
            strip.AddComponent<Image>().color = tint;

            // Label
            var labelGO = CreateUIObject("Label", card.transform);
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0, -20);
            lrt.sizeDelta = new Vector2(-40, 40);
            var lt = labelGO.AddComponent<TextMeshProUGUI>();
            lt.text = label;
            lt.fontSize = 22;
            lt.fontStyle = FontStyles.Bold;
            lt.color = new Color(0.42f, 0.45f, 0.50f);
            lt.alignment = TextAlignmentOptions.Center;

            // Value
            var valueGO = CreateUIObject("Value", card.transform);
            var vrt = valueGO.GetComponent<RectTransform>();
            FillParent(vrt);
            var vt = valueGO.AddComponent<TextMeshProUGUI>();
            vt.text = "0";
            vt.fontSize = 80;
            vt.fontStyle = FontStyles.Bold;
            vt.color = tint;
            vt.alignment = TextAlignmentOptions.Center;
            return vt;
        }

        // ============================================================
        // FORM PANELS
        // ============================================================
        private static GameObject BuildRegistrationPanel(GameObject parent)
        {
            var panel = CreateScreenPanel(parent, "RegistrationPanel");
            var ctl = panel.AddComponent<RegistrationController>();

            CreateScreenTitle(panel.transform, "Register Employee");

            var form = CreateFormContainer(panel.transform, 0, -260);
            var lblId = CreateFormLabel(form.transform, "ID:", -20);
            ctl.generatedIdText = lblId;
            ctl.generatedIdText.text = "ID: (auto)";
            ctl.generatedIdText.fontStyle = FontStyles.Bold;

            ctl.nameInput       = CreateLabeledInput(form.transform, "Full Name *",  -130);
            ctl.departmentInput = CreateLabeledInput(form.transform, "Department *", -300);
            ctl.contactInput    = CreateLabeledInput(form.transform, "Contact",      -470);

            ctl.submitButton = CreateActionButton(panel.transform, "Register", colorAccent, new Vector2(0, -1100));
            ctl.backButton   = CreateActionButton(panel.transform, "Back",     new Color(0.42f, 0.45f, 0.50f), new Vector2(0, -1280));

            return panel;
        }

        private static GameObject BuildInventoryPanel(GameObject parent)
        {
            var panel = CreateScreenPanel(parent, "InventoryPanel");
            var ctl = panel.AddComponent<InventoryController>();

            CreateScreenTitle(panel.transform, "Add Inventory");

            var form = CreateFormContainer(panel.transform, 0, -260);
            ctl.generatedIdText = CreateFormLabel(form.transform, "Item ID: (auto)", -20);
            ctl.generatedIdText.fontStyle = FontStyles.Bold;

            ctl.itemNameInput    = CreateLabeledInput(form.transform, "Item Name *",       -130);
            ctl.quantityInput    = CreateLabeledInput(form.transform, "Quantity *",        -300);
            ctl.descriptionInput = CreateLabeledInput(form.transform, "Description",       -470);

            // Category toggles
            var togglesParent = CreateUIObject("Toggles", form.transform);
            var tprt = togglesParent.GetComponent<RectTransform>();
            tprt.anchorMin = new Vector2(0, 1);
            tprt.anchorMax = new Vector2(1, 1);
            tprt.pivot = new Vector2(0.5f, 1f);
            tprt.anchoredPosition = new Vector2(0, -640);
            tprt.sizeDelta = new Vector2(-40, 80);

            var toggleGroup = togglesParent.AddComponent<ToggleGroup>();
            ctl.indispensableToggle = CreateToggle(togglesParent.transform, "Indispensable (returnable)", new Vector2(-200, 0), toggleGroup, true);
            ctl.dispensableToggle   = CreateToggle(togglesParent.transform, "Dispensable (consumable)",   new Vector2(200, 0), toggleGroup, false);

            ctl.submitButton = CreateActionButton(panel.transform, "Add Item", colorAccent, new Vector2(0, -1100));
            ctl.backButton   = CreateActionButton(panel.transform, "Back",     new Color(0.42f, 0.45f, 0.50f), new Vector2(0, -1280));

            return panel;
        }

        private static GameObject BuildAssignmentPanel(GameObject parent)
        {
            var panel = CreateScreenPanel(parent, "AssignmentPanel");
            var ctl = panel.AddComponent<AssignmentController>();

            CreateScreenTitle(panel.transform, "Assign Item");

            var form = CreateFormContainer(panel.transform, 0, -260);

            CreateFormLabel(form.transform, "Employee:", -20);
            ctl.employeeDropdown = CreateDropdown(form.transform, -90);

            CreateFormLabel(form.transform, "Item:", -240);
            ctl.itemDropdown = CreateDropdown(form.transform, -310);

            ctl.itemAvailableText = CreateFormLabel(form.transform, "Available: 0", -440);
            ctl.itemAvailableText.color = colorAccent;
            ctl.itemAvailableText.fontStyle = FontStyles.Bold;

            ctl.quantityInput = CreateLabeledInput(form.transform, "Quantity *", -550);

            ctl.submitButton = CreateActionButton(panel.transform, "Assign", colorAccent, new Vector2(0, -1100));
            ctl.backButton   = CreateActionButton(panel.transform, "Back",   new Color(0.42f, 0.45f, 0.50f), new Vector2(0, -1280));

            return panel;
        }

        private static GameObject BuildReturnPanel(GameObject parent)
        {
            var panel = CreateScreenPanel(parent, "ReturnPanel");
            var ctl = panel.AddComponent<ReturnController>();

            CreateScreenTitle(panel.transform, "Return Item");

            var form = CreateFormContainer(panel.transform, 0, -260);

            CreateFormLabel(form.transform, "Active Assignment:", -20);
            ctl.assignmentDropdown = CreateDropdown(form.transform, -90);

            CreateFormLabel(form.transform, "Condition:", -240);
            var togglesParent = CreateUIObject("ConditionToggles", form.transform);
            var tprt = togglesParent.GetComponent<RectTransform>();
            tprt.anchorMin = new Vector2(0, 1);
            tprt.anchorMax = new Vector2(1, 1);
            tprt.pivot = new Vector2(0.5f, 1f);
            tprt.anchoredPosition = new Vector2(0, -310);
            tprt.sizeDelta = new Vector2(-40, 80);

            var grp = togglesParent.AddComponent<ToggleGroup>();
            ctl.goodToggle     = CreateToggle(togglesParent.transform, "Good",     new Vector2(-300, 0), grp, true);
            ctl.damagedToggle  = CreateToggle(togglesParent.transform, "Damaged",  new Vector2(0, 0),    grp, false);
            ctl.consumedToggle = CreateToggle(togglesParent.transform, "Consumed", new Vector2(300, 0),  grp, false);

            ctl.notesInput = CreateLabeledInput(form.transform, "Notes (optional)", -460);

            ctl.submitButton = CreateActionButton(panel.transform, "Process Return", colorAccent, new Vector2(0, -1100));
            ctl.backButton   = CreateActionButton(panel.transform, "Back",           new Color(0.42f, 0.45f, 0.50f), new Vector2(0, -1280));

            return panel;
        }

        // ============================================================
        // LIST VIEW PANEL
        // ============================================================
        private static GameObject BuildListViewPanel(GameObject parent, string panelName, string defaultTitle)
        {
            var panel = CreateScreenPanel(parent, panelName);
            var ctl = panel.AddComponent<ListViewController>();

            ctl.titleText = CreateScreenTitle(panel.transform, defaultTitle);

            // Scrollable content
            var scrollGO = CreateUIObject("Scroll", panel.transform);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.offsetMin = new Vector2(40, 280);
            srt.offsetMax = new Vector2(-40, -260);
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = colorBgPanel;
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = CreateUIObject("Viewport", scrollGO.transform);
            var vrt = viewport.GetComponent<RectTransform>();
            FillParent(vrt);
            viewport.AddComponent<RectMask2D>();
            var vimg = viewport.AddComponent<Image>();
            vimg.color = new Color(1, 1, 1, 0.01f);
            scroll.viewport = vrt;

            var content = CreateUIObject("Content", viewport.transform);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 2000);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            scroll.content = crt;

            var contentText = CreateUIObject("ContentText", content.transform);
            var ct = contentText.AddComponent<TextMeshProUGUI>();
            ct.text = "Loading...";
            ct.fontSize = 26;
            ct.color = colorTextDark;
            ct.richText = true;
            ct.alignment = TextAlignmentOptions.TopLeft;
            ctl.contentText = ct;

            ctl.backButton = CreateActionButton(panel.transform, "Back", new Color(0.42f, 0.45f, 0.50f), new Vector2(0, 100));
            // override anchor to bottom
            var backRT = ctl.backButton.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0);
            backRT.anchorMax = new Vector2(0.5f, 0);
            backRT.pivot = new Vector2(0.5f, 0);
            backRT.anchoredPosition = new Vector2(0, 80);

            return panel;
        }

        // ============================================================
        // TOAST
        // ============================================================
        private static GameObject BuildToast(GameObject parent)
        {
            var toast = CreateUIObject("Toast", parent.transform);
            var rt = toast.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 240);
            rt.sizeDelta = new Vector2(900, 100);
            var img = toast.AddComponent<Image>();
            img.color = new Color(0.114f, 0.157f, 0.231f, 0.95f);

            var txt = CreateUIObject("ToastText", toast.transform);
            var trt = txt.GetComponent<RectTransform>();
            FillParent(trt);
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 28;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            toast.SetActive(false);
            return toast;
        }

        // ============================================================
        // UI HELPERS
        // ============================================================
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateScreenPanel(GameObject parent, string name)
        {
            var panel = CreateUIObject(name, parent.transform);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -160); // leave room for header
            return panel;
        }

        private static TextMeshProUGUI CreateScreenTitle(Transform parent, string text)
        {
            var go = CreateUIObject("Title", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -80);
            rt.sizeDelta = new Vector2(0, 100);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 48;
            t.fontStyle = FontStyles.Bold;
            t.color = colorTextDark;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        private static GameObject CreateFormContainer(Transform parent, float x, float y)
        {
            var f = CreateUIObject("Form", parent);
            var rt = f.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(-80, 800);
            var img = f.AddComponent<Image>();
            img.color = colorBgPanel;
            return f;
        }

        private static TextMeshProUGUI CreateFormLabel(Transform parent, string text, float yOffset)
        {
            var go = CreateUIObject("Label_" + text, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(-40, 50);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 28;
            t.color = colorTextDark;
            t.alignment = TextAlignmentOptions.Left;
            t.margin = new Vector4(20, 0, 20, 0);
            return t;
        }

        private static TMP_InputField CreateLabeledInput(Transform parent, string labelText, float yOffset)
        {
            CreateFormLabel(parent, labelText, yOffset);

            var go = CreateUIObject("Input_" + labelText, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yOffset - 60);
            rt.sizeDelta = new Vector2(-40, 100);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.965f, 0.969f, 0.976f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = colorBorder;
            outline.effectDistance = new Vector2(1, -1);

            var input = go.AddComponent<TMP_InputField>();

            var textArea = CreateUIObject("TextArea", go.transform);
            var trt = textArea.GetComponent<RectTransform>();
            FillParent(trt);
            trt.offsetMin = new Vector2(20, 5);
            trt.offsetMax = new Vector2(-20, -5);
            textArea.AddComponent<RectMask2D>();

            var placeholder = CreateUIObject("Placeholder", textArea.transform);
            var prt = placeholder.GetComponent<RectTransform>();
            FillParent(prt);
            var pt = placeholder.AddComponent<TextMeshProUGUI>();
            pt.text = "Enter " + labelText.Replace("*", "").Trim().ToLower() + "...";
            pt.fontSize = 28;
            pt.color = new Color(0.6f, 0.6f, 0.6f);
            pt.alignment = TextAlignmentOptions.MidlineLeft;

            var textGO = CreateUIObject("Text", textArea.transform);
            var txrt = textGO.GetComponent<RectTransform>();
            FillParent(txrt);
            var tt = textGO.AddComponent<TextMeshProUGUI>();
            tt.text = "";
            tt.fontSize = 28;
            tt.color = colorTextDark;
            tt.alignment = TextAlignmentOptions.MidlineLeft;

            input.textViewport = trt;
            input.textComponent = tt;
            input.placeholder = pt;

            return input;
        }

        private static TMP_Dropdown CreateDropdown(Transform parent, float yOffset)
        {
            var go = CreateUIObject("Dropdown", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, yOffset);
            rt.sizeDelta = new Vector2(-40, 100);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.965f, 0.969f, 0.976f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = colorBorder;
            outline.effectDistance = new Vector2(1, -1);

            var dd = go.AddComponent<TMP_Dropdown>();

            // Label
            var labelGO = CreateUIObject("Label", go.transform);
            var lrt = labelGO.GetComponent<RectTransform>();
            FillParent(lrt);
            lrt.offsetMin = new Vector2(20, 0);
            lrt.offsetMax = new Vector2(-60, 0);
            var lt = labelGO.AddComponent<TextMeshProUGUI>();
            lt.text = "-- Select --";
            lt.fontSize = 26;
            lt.color = colorTextDark;
            lt.alignment = TextAlignmentOptions.MidlineLeft;
            dd.captionText = lt;

            // Arrow
            var arrowGO = CreateUIObject("Arrow", go.transform);
            var art = arrowGO.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 0.5f);
            art.anchorMax = new Vector2(1, 0.5f);
            art.pivot = new Vector2(1, 0.5f);
            art.anchoredPosition = new Vector2(-20, 0);
            art.sizeDelta = new Vector2(30, 30);
            var atxt = arrowGO.AddComponent<TextMeshProUGUI>();
            atxt.text = "▼";
            atxt.fontSize = 26;
            atxt.color = colorPrimary;
            atxt.alignment = TextAlignmentOptions.Center;

            // Template (drop-down list)
            var template = CreateUIObject("Template", go.transform);
            template.SetActive(false);
            var trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, 0);
            trt.sizeDelta = new Vector2(0, 400);
            var timg = template.AddComponent<Image>();
            timg.color = colorBgPanel;
            var sr = template.AddComponent<ScrollRect>();
            sr.horizontal = false;

            var viewport = CreateUIObject("Viewport", template.transform);
            var vrt = viewport.GetComponent<RectTransform>();
            FillParent(vrt);
            viewport.AddComponent<RectMask2D>();
            var vimg = viewport.AddComponent<Image>();
            vimg.color = new Color(1, 1, 1, 0.01f);
            sr.viewport = vrt;

            var content = CreateUIObject("Content", viewport.transform);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 100);
            sr.content = crt;

            var item = CreateUIObject("Item", content.transform);
            var irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(1, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(0, 80);
            var itoggle = item.AddComponent<Toggle>();

            var itemBG = CreateUIObject("Item Background", item.transform);
            var ibgRT = itemBG.GetComponent<RectTransform>();
            FillParent(ibgRT);
            var ibg = itemBG.AddComponent<Image>();
            ibg.color = colorBgLight;

            var itemCheck = CreateUIObject("Item Checkmark", item.transform);
            var ickRT = itemCheck.GetComponent<RectTransform>();
            ickRT.anchorMin = new Vector2(0, 0.5f);
            ickRT.anchorMax = new Vector2(0, 0.5f);
            ickRT.pivot = new Vector2(0, 0.5f);
            ickRT.anchoredPosition = new Vector2(20, 0);
            ickRT.sizeDelta = new Vector2(30, 30);
            var ick = itemCheck.AddComponent<Image>();
            ick.color = colorPrimary;

            var itemLabel = CreateUIObject("Item Label", item.transform);
            var ilRT = itemLabel.GetComponent<RectTransform>();
            FillParent(ilRT);
            ilRT.offsetMin = new Vector2(60, 0);
            ilRT.offsetMax = new Vector2(-20, 0);
            var ilTxt = itemLabel.AddComponent<TextMeshProUGUI>();
            ilTxt.text = "Option";
            ilTxt.fontSize = 24;
            ilTxt.color = colorTextDark;
            ilTxt.alignment = TextAlignmentOptions.MidlineLeft;

            itoggle.targetGraphic = ibg;
            itoggle.graphic = ick;
            itoggle.isOn = true;

            dd.template = trt;
            dd.itemText = ilTxt;
            dd.targetGraphic = img;

            dd.options.Clear();
            dd.options.Add(new TMP_Dropdown.OptionData("-- Select --"));
            dd.RefreshShownValue();

            return dd;
        }

        private static Toggle CreateToggle(Transform parent, string label, Vector2 anchoredPos, ToggleGroup group, bool isOn)
        {
            var go = CreateUIObject("Toggle_" + label, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(380, 60);

            var t = go.AddComponent<Toggle>();
            t.group = group;
            t.isOn = isOn;

            var bg = CreateUIObject("Background", go.transform);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.pivot = new Vector2(0, 0.5f);
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = new Vector2(40, 40);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = colorBgLight;
            var ol = bg.AddComponent<Outline>();
            ol.effectColor = colorPrimary;
            ol.effectDistance = new Vector2(1, -1);

            var ck = CreateUIObject("Checkmark", bg.transform);
            var ckRT = ck.GetComponent<RectTransform>();
            FillParent(ckRT);
            ckRT.offsetMin = new Vector2(6, 6);
            ckRT.offsetMax = new Vector2(-6, -6);
            var ckImg = ck.AddComponent<Image>();
            ckImg.color = colorPrimary;

            var lbl = CreateUIObject("Label", go.transform);
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0, 0);
            lblRT.anchorMax = new Vector2(1, 1);
            lblRT.pivot = new Vector2(0.5f, 0.5f);
            lblRT.offsetMin = new Vector2(60, 0);
            lblRT.offsetMax = new Vector2(0, 0);
            var lblTxt = lbl.AddComponent<TextMeshProUGUI>();
            lblTxt.text = label;
            lblTxt.fontSize = 24;
            lblTxt.color = colorTextDark;
            lblTxt.alignment = TextAlignmentOptions.MidlineLeft;

            t.targetGraphic = bgImg;
            t.graphic = ckImg;
            return t;
        }

        private static Button CreateNavButton(Transform parent, string label, Color tint)
        {
            var go = CreateUIObject("Btn_" + label, parent);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = CreateUIObject("Text", go.transform);
            var rt = txtGO.GetComponent<RectTransform>();
            FillParent(rt);
            var t = txtGO.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 30;
            t.fontStyle = FontStyles.Bold;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static Button CreateActionButton(Transform parent, string label, Color tint, Vector2 anchoredPos)
        {
            var go = CreateUIObject("Btn_" + label, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(700, 130);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = CreateUIObject("Text", go.transform);
            var trt = txtGO.GetComponent<RectTransform>();
            FillParent(trt);
            var t = txtGO.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 36;
            t.fontStyle = FontStyles.Bold;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var current = EditorBuildSettings.scenes;
            foreach (var s in current)
                if (s.path == scenePath) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(current);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
