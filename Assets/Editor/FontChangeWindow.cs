using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UITextFontChange;
using UnityEditor.SceneManagement;

public enum eFontType
{
    UI_TEXT,
    TEXT_MESH_PRO
}

/// <summary>
/// 게임내의 모든 UI 오브젝트의 Text를 찾아내어 폰트를 변경합니다
/// </summary>
public class FontChangeWindow : EditorWindow
{
    #region [Private Variables]
    /// <summary>
    /// 기본 경로
    /// </summary>
    private string Path = "Assets";

    /// <summary>선택 Prefab</summary>
    private GameObject TargetPrefab;
    private GameObject PrevTargetPrefab;

    /// <summary>
    /// 폰트 타입
    /// </summary>
    private eFontType fontType;

    /// <summary>변경할 Font</summary>
    private Font ChangeFont;
    private TMP_FontAsset ChangeFontAsset;
    
    /// <summary>Prefab Type의 UIText를 포함하고 있는 오브젝트 카운트</summary>
    private int ReferencedTransformCount;

    /// <summary>
    /// 컴포넌트를 포함하고 있는 오브젝트 리스트
    /// </summary>
    private Dictionary<string, List<Transform>> referencedList = new Dictionary<string, List<Transform>>();
    /// <summary>
    /// 리스트 항목 접힘 상태 저장
    /// </summary>
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    /// <summary>상태 메세지</summary>
    private string StateMSG = string.Empty;
    /// <summary>에러메세지 스타일</summary>
    private GUIStyle ErrorLabelStyle = new GUIStyle();
    /// <summary>진행 여부</summary>
    private bool IsProgress = false;
    /// <summary>찾는 중 여부</summary>
    private bool IsFindCheck = false;

    /// <summary>진행도 수치</summary>
    private int ProcessCount = 0;

    /// <summary>선택 토글</summary>
    private bool IsTargetPath = true;
    private bool IsTargetSelect = false;

    /// <summary>
    /// 폰트 타입
    /// </summary>
    private bool IsFont = true;
    private bool IsTmpFont = false;

    /// <summary>
    /// 스크롤뷰
    /// </summary>
    private Vector2 scrollPosition;
    #endregion

    /// <summary>
    /// 초기화
    /// </summary>
    [MenuItem("Tools/Font or TMP_Font Change By Prefab")]
    public static void ShowMyEditor()
    {
        FontChangeWindow window = GetWindow<FontChangeWindow>();
        window.titleContent = new GUIContent("Font & TMP_Font Change By Prefab", "경로 내의 모든 프리팹 혹은 특정 프리팹의 폰트를 한번에 변경해 주는 툴입니다.");
        window.minSize = new Vector2(450f, 450f);
        window.maxSize = new Vector2(600f, 700f);
        window.InitVariables();
        window.ShowUtility();
    }

    /// <summary>
    /// 변수 초기화.
    /// </summary>
    public void InitVariables()
    {
        ChangeFont = null;
        ReferencedTransformCount = 0;
        IsFindCheck = false;
        if (referencedList != null)
            referencedList.Clear();
        else
            referencedList = new Dictionary<string, List<Transform>>();

        if (foldoutStates != null)
            foldoutStates.Clear();
        else
            foldoutStates = new Dictionary<string, bool>();
        ErrorLabelStyle.normal.textColor = Color.red;
        ErrorLabelStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void OnGUI()
    {
        DrawLine(5);
        DrawSelectTypeInfo();
        DrawLine(10);

        if (IsTargetPath)
        {
            //경로 선택 시 진행
            DrawSelectedPath();
            DrawLine(10);
            DrawSelectFontByType();
            DrawLine(10);
            DrawStepTwoByPath();
        }
        else// if (IsTargetSelect)
        {
            //프리팹 직접 선택 시 진행
            DrawSelectedObject();
            DrawLine(10);
            DrawSelectFontByType();
            DrawLine(10);
            DrawStepTwoBySelectObject();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(string.Format("버튼을 눌러 {0} 를 포함한 프리팹을 찾기", fontType.ToString())) == true)
        {
            if (IsFindCheck) return;
            if (referencedList != null)
                referencedList.Clear();
            else
                referencedList = new Dictionary<string, List<Transform>>();

            if (foldoutStates != null)
                foldoutStates.Clear();
            else
                foldoutStates = new Dictionary<string, bool>();
            IsFindCheck = true;
            if (IsTargetPath)
            {
                FindAllPrefabByPath();
                IsFindCheck = false;
            }
            if (IsTargetSelect)
            {
                FindAllPrefabBySelect();
                IsFindCheck = false;
            }
        }
        GUILayout.EndHorizontal();
        DrawLine(10);
        //결과 보여주기
        DrawResult();

        if (referencedList.Count > 0)
        {
            DrawLine(5);
            GUILayout.BeginHorizontal();
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            GUILayout.Label("Step.3 : 버튼을 눌러 적용합니다", style, GUILayout.Width(300f));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("검색 된 모든 대상 적용 하기") == true)
            {
                bool isProcessAble = ProcessChangeFont();
                if (isProcessAble)
                {
                    ChangeFontProcess();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            DrawLine(5);
        }
        /* 상태 메세지 사용 안함
       GUILayout.BeginHorizontal();
       GUILayout.Label(StateMSG, ErrorLabelStyle, GUILayout.Width(450f));
       GUILayout.EndHorizontal();
       */
    }

    /// <summary>
    /// 변경 할 타입을 선택하기
    /// </summary>
    private void DrawSelectTypeInfo()
    {
        EditorGUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("Step.1 : 변경할 타입을 선택하세요", style, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        IsTargetPath = EditorGUILayout.Toggle("경로 선택", IsTargetPath, GUILayout.ExpandWidth(true));
        IsTargetSelect = !IsTargetPath;
        IsTargetSelect = EditorGUILayout.Toggle("직접 선택", IsTargetSelect, GUILayout.ExpandWidth(true));
        IsTargetPath = !IsTargetSelect;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 변경 할 폰트 선택 하기
    /// </summary>
    private void DrawSelectFontByType()
    {
        GUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("Step.1-2 : 변경할 Font를 선택합니다", style, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        IsFont = EditorGUILayout.Toggle("UI Text Font", IsFont, GUILayout.ExpandWidth(true));
        IsTmpFont = !IsFont;
        IsTmpFont = EditorGUILayout.Toggle("Text Mesh Pro Font Asset", IsTmpFont, GUILayout.ExpandWidth(true));
        IsFont = !IsTmpFont;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
        fontType = IsFont ? eFontType.UI_TEXT : eFontType.TEXT_MESH_PRO;
        if (IsFont)
        {
            GUILayout.BeginHorizontal();
            ChangeFont = (Font)EditorGUILayout.ObjectField(ChangeFont, typeof(Font), false);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.BeginHorizontal();
            ChangeFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(ChangeFontAsset, typeof(TMP_FontAsset), false);
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(5);
    }

    /// <summary>
    /// 경로 선택을 사용 할 시 나오는 다음 스텝
    /// </summary>
    private void DrawSelectedPath()
    {
        EditorGUI.BeginDisabledGroup(IsTargetSelect);
        GUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("Step.1-1 : 검색할 경로를 설정 할 수 있습니다", style, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("설정 할 경로 :", GUILayout.Width(100f));
        Path = EditorGUILayout.TextField(Path, GUILayout.Width(340f));
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// 경로를 선택 한 후 두번째 스텝
    /// </summary>
    private void DrawStepTwoByPath()
    {
        GUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        string sForm = string.Format("Step.2 : Path 경로를 검색하여 [ {0} ]를 포함한 프리팹을 찾습니다", fontType.ToString());
        GUILayout.Label(sForm, style, GUILayout.Width(450f));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    /// <summary>
    /// 직접 선택을 사용 할 시 나오는 다음 스텝
    /// </summary>
    private void DrawSelectedObject()
    {
        EditorGUI.BeginDisabledGroup(IsTargetPath);
        GUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        GUILayout.Label("Step.1-1 : 직접 선택 할 수 있습니다 (드래그로 넣기)", style, GUILayout.Width(450f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("선택 할 Prefab :", GUILayout.Width(100f));
        TargetPrefab = (GameObject)EditorGUILayout.ObjectField(TargetPrefab, typeof(GameObject), true);
        if (PrevTargetPrefab != null && TargetPrefab != null)
        {
            if (PrevTargetPrefab.GetInstanceID() != TargetPrefab.GetInstanceID())
            {
                referencedList.Clear();
                foldoutStates.Clear();
                PrevTargetPrefab = TargetPrefab;
            }
        }
        else if (PrevTargetPrefab == null)
        {
            PrevTargetPrefab = TargetPrefab;
        }
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// 직접 선택을 한 후 두번째 스텝
    /// </summary>
    private void DrawStepTwoBySelectObject()
    {
        GUILayout.BeginHorizontal();
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        string sForm = string.Format("Step.2 : 선택된 프리팹에서 [ {0} ]를 포함한 오브젝트를 찾습니다", fontType.ToString());
        GUILayout.Label(sForm, style, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    /// <summary>
    /// 경로 내 모든 프리팹을 체크하여 Font Type에 맞는 컴포넌트를 장착한 프리팹만 저장한다.
    /// </summary>
    private void FindAllPrefabByPath()
    {
        if (string.IsNullOrEmpty(Path))
        {
            /*
            ErrorLabelStyle.normal.textColor = Color.red;
            StateMSG = "경로를 확인하세요";
            */
            Path = "Assets";
            return;
        }
        ReferencedTransformCount = 0;
        //프리팹 타입 모두 리턴
        string[] GUIDArr = AssetDatabase.FindAssets("t:prefab", new[] { Path });
        for (int i = 0; i < GUIDArr.Length; i++)
        {
            //리스트 생성.
            List<Transform> childList = new List<Transform>();
            string path = AssetDatabase.GUIDToAssetPath(GUIDArr[i]);
            Transform parent = (Transform)AssetDatabase.LoadAssetAtPath(path, typeof(Transform));
            bool isAdded = IsAddedComponentByFontType(parent);
            if (isAdded)
            {
                childList.Add(parent);
            }
            //해당 프리팹의 모든 자식 리턴
            List<Transform> allChildList = parent.GetAllChildren();
            for (int j = 0; j < allChildList.Count; j++)
            {
                isAdded = IsAddedComponentByFontType(allChildList[j]);
                if (isAdded)
                {
                    childList.Add(allChildList[j]);
                }
            }
            //딕셔너리에 추가
            if (referencedList.ContainsKey(path) == false && childList.Count > 0)
            {
                referencedList.Add(path, childList);
                foldoutStates.Add(path, false);
                ReferencedTransformCount += childList.Count;
            }
        }
    }

    /// <summary>
    /// 선택된 프리팹을 체크하여 Font Type에 맞는 컴포넌트를 장착한 프리팹만 저장한다.
    /// </summary>
    private void FindAllPrefabBySelect()
    {
        if (TargetPrefab == null)
        {
            /*
            ErrorLabelStyle.normal.textColor = Color.red;
            StateMSG = "선택 된 Prefab이 없습니다";
            */
            return;
        }
        ReferencedTransformCount = 0;

        //리스트 생성.
        List<Transform> childList = new List<Transform>();
        string path = AssetDatabase.GetAssetPath(TargetPrefab);
        Transform parent = (Transform)AssetDatabase.LoadAssetAtPath(path, typeof(Transform));
        //제일 최상위 오브젝트도 컴포넌트 존재 여부 체크
        bool isAdded = IsAddedComponentByFontType(parent);
        if (isAdded)
        {
            childList.Add(parent);
        }
        //해당 프리팹의 모든 자식 리턴
        List<Transform> allChildList = parent.GetAllChildren();
        for (int j = 0; j < allChildList.Count; j++)
        {
            isAdded = IsAddedComponentByFontType(allChildList[j]);
            if (isAdded)
            {
                childList.Add(allChildList[j]);
            }
        }
        //딕셔너리에 추가
        if (referencedList.ContainsKey(path) == false && childList.Count > 0)
        {
            referencedList.Add(path, childList);
            foldoutStates.Add(path, false);
            ReferencedTransformCount += childList.Count;
        }
    }

    /// <summary>
    /// 폰트 변경 진행 프로세스
    /// </summary>
    private bool ProcessChangeFont()
    {
        if (IsProgress) return false;

        if (ReferencedTransformCount == 0)
        {
            /*
            ErrorLabelStyle.normal.textColor = Color.yellow;
            StateMSG = string.Format("{0}를 포함한 프리팹이 없습니다", fontType.ToString());
            */
            return false;
        }

        if (fontType == eFontType.UI_TEXT && ChangeFont == null)
        {
            /*
            ErrorLabelStyle.normal.textColor = Color.red;
            StateMSG = string.Format("변경 할 {0} Font를 선택하세요", fontType.ToString());
            */
            return false;
        }

        if (fontType == eFontType.TEXT_MESH_PRO && ChangeFontAsset == null)
        {
            /*
            ErrorLabelStyle.normal.textColor = Color.red;
            StateMSG = string.Format("변경 할 {0} Font를 선택하세요", fontType.ToString());
            */
            return false;
        }

        ErrorLabelStyle.normal.textColor = Color.yellow;
        StateMSG = "폰트를 교체합니다";
        //진행 여부 체크.
        IsProgress = true;
        return true;
    }

    /// <summary>
    /// 폰트를 변경한다.
    /// </summary>
    private void ChangeFontProcess()
    {
        ProcessCount = 0;
        foreach (KeyValuePair<string, List<Transform>> data in referencedList)
        {
            Transform tr = (Transform)AssetDatabase.LoadAssetAtPath(data.Key, typeof(Transform));
            for (int i = 0; i < data.Value.Count; i++)
            {
                if (fontType == eFontType.UI_TEXT)
                {
                    Text text = data.Value[i].GetComponent<Text>();
                    if (text != null)
                    {
                        text.font = ChangeFont;
                        ProcessCount++;
                        ShowChangeProgressBar();
                    }
                }
                else
                {
                    TMP_Text text = data.Value[i].GetComponent<TMP_Text>();
                    if (text != null)
                    {
                        text.font = ChangeFontAsset;
                        ProcessCount++;
                        ShowChangeProgressBar();
                    }
                }
            }
            PrefabUtility.SavePrefabAsset(tr.gameObject);
        }
    }

    /// <summary>
    /// 검색 결과 보여주기
    /// </summary>
    private void DrawResult()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        // 게임 오브젝트 리스트를 순회하며 각 항목을 보여준다
        foreach (KeyValuePair<string, List<Transform>> obj in referencedList)
        {
            Transform tr = (Transform)AssetDatabase.LoadAssetAtPath(obj.Key, typeof(Transform));
            if (!foldoutStates.ContainsKey(obj.Key))
            {
                foldoutStates[obj.Key] = false;
            }

            GUILayout.BeginHorizontal();
            foldoutStates[obj.Key] = GUILayout.Toggle(foldoutStates[obj.Key], "( " + obj.Value.Count + " ) " + obj.Key, GUI.skin.button, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Open_Edit", GUILayout.Width(100)))
            {
                PrefabStageUtility.OpenPrefab(obj.Key);
                //Selection.activeObject = tr.gameObject;
            }
            GUILayout.EndHorizontal();

            if (foldoutStates[obj.Key])
            {
                foreach (Transform child in obj.Value)
                {
                    string fontName = string.Empty;
                    if (fontType == eFontType.UI_TEXT)
                    {
                        Text text = child.GetComponent<Text>();
                        if (text != null)
                        {
                            if (text.font != null)
                            {
                                fontName = text.font.name;
                            }
                        }
                    }
                    else
                    {
                        TMP_Text text = child.GetComponent<TMP_Text>();
                        if (text != null)
                        {
                            if (text.font != null)
                            {
                                fontName = text.font.name;
                            }
                        }
                    }

                    if (GUILayout.Button(child.name + "_"+ fontName))
                    {
                        Selection.activeGameObject = child.gameObject;
                    }
                }
            }
        }
        GUILayout.EndScrollView(); // 스크롤뷰 종료
    }

    /// <summary>
    /// 진행 상황 보여주기
    /// </summary>
    private void ShowChangeProgressBar()
    {
        float progress = (float)ProcessCount / ReferencedTransformCount;
        EditorUtility.DisplayProgressBar("적용 중", string.Format("{0} / {1}", ProcessCount, ReferencedTransformCount), progress);
        if (ProcessCount > 0 && progress == 1f)
        {
            IsProgress = false;
            EditorUtility.ClearProgressBar();
            bool isResult = EditorUtility.DisplayDialog("결과", "모든 폰트가 변경 되었습니다.", "확인");
            if (isResult)
            {
                StateMSG = string.Empty;
            }
        }
    }

    /// <summary>
    /// 폰트 타입에 맞는 컴포넌트가 장착되어 있는가 여부
    /// </summary>
    private bool IsAddedComponentByFontType(Transform tr)
    {
        if (fontType == eFontType.UI_TEXT)
        {
            Text text = tr.GetComponent<Text>();
            if (text != null)
            {
                return true;
            }
        }
        else
        {
            TMP_Text text = tr.GetComponent<TMP_Text>();
            if (text != null)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>라인 그리기</summary>
    private void DrawLine(int aSpace)
    {
        GUILayout.Space(aSpace);
        var rect = EditorGUILayout.BeginHorizontal();
        Handles.color = Color.gray;
        Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(aSpace);
    }
}
namespace UITextFontChange
{
    /// <summary>
    /// 자신을 제외한 자식을 리스트로 리턴한다 
    /// </summary>
    public static class TransformGetAllChildren
    {
        public static List<Transform> GetAllChildren(this Transform parent, List<Transform> transformList = null)
        {
            if (transformList == null) transformList = new List<Transform>();
            foreach (Transform child in parent)
            {
                transformList.Add(child);
                child.GetAllChildren(transformList);
            }
            return transformList;
        }
    }
}
