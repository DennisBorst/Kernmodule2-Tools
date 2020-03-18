using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;

namespace SpritePlacer2D
{
    public class SpritePlacer : EditorWindow
    {
        public static Sprite[] spriteArray;
        public static Texture2D[] textureArray;

        private static Sprite[] cmCurSprites;
        private static Sprite child;
        private static Texture2D cmSelectedColor;
        private Texture2D aTexture;

        private static EditorWindow window;
        private static Vector2 tileScrollPosition = Vector2.zero;

        private static bool makeCollider = false;
        private static bool clickedTile = false;
        private static bool isActive;

        private static List<int> cmSelectedTile = new List<int>();
        private static List<Sprite> spriteList = new List<Sprite>();
        private static List<Sprite> cmCurSprite = new List<Sprite>();

        private static GameObject headObject;
        private static GameObject obj;

        private static Vector3 cmCurPos;

        private int spriteUnit = 80;
        private static int selectedTool;
        private static int orderInLayer = 0;
        private static int layer = 0;

        private static string tag = "Untagged";

        private static Rect newRect;
        private static Material mat;

        [MenuItem("Tools/SpritePlacer #p")]
        public static void OnEnable()
        {
            window = EditorWindow.GetWindow(typeof(SpritePlacer));
            window.minSize = new Vector2(260, 400);

            spriteArray = Resources.LoadAll<Sprite>("Sprites");
            textureArray = Resources.LoadAll<Texture2D>("Sprites");

            isActive = true;

            if (headObject == null)
            {
                headObject = GameObject.Find("Sprite map");
                if (headObject == null)
                {
                    headObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    headObject.name = "Sprite map";
                    headObject.transform.localScale = new Vector3(100, 100, 100);
                    headObject.AddComponent<MeshCollider>();
                    headObject.AddComponent<SpriteRenderer>();
                    headObject.GetComponent<Renderer>().enabled = false;
                }
            }

            SceneView.duringSceneGui += DrawObjectInEditor;
        }

        public void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.button);

            if (headObject == null)
            {
                CreateNewObject();
            }

            if (spriteArray == null)
            {
                return;
            }

            int columnCount = Mathf.RoundToInt((position.width) / 70) - 2;
            int x = 0;
            int y = 0;
            int current = 0;

            GUILayout.Label("2D Tile Placer", EditorStyles.boldLabel);

            if (GUILayout.Button("New sprite list"))
            {
                CreateNewObject();
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current tool state", GUILayout.Width(150));
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (isActive)
            {
                style.normal.textColor = Color.black;
                GUI.backgroundColor = new Color32(180, 255, 180, 255);

                if (GUILayout.Button("Active", style))
                {
                    isActive = false;
                }
            }
            else
            {
                style.normal.textColor = Color.red;

                if (GUILayout.Button("De-active", style))
                {
                    isActive = true;
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            tag = EditorGUILayout.TagField("Change tags to: ", tag);
            layer = EditorGUILayout.LayerField("Change layers to: ", layer);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Paint With Collider", GUILayout.Width(150));
            makeCollider = EditorGUILayout.Toggle(makeCollider);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            orderInLayer = EditorGUILayout.IntField("Order in layer: ", orderInLayer);
            EditorGUILayout.EndHorizontal();

            string[] tools = { "Draw", "Erase" };
            selectedTool = GUILayout.Toolbar(selectedTool, tools);
            Debug.Log(selectedTool);
            tileScrollPosition = EditorGUILayout.BeginScrollView(tileScrollPosition, false, true, GUILayout.Width(position.width));
            GUILayout.BeginHorizontal();

            for (int i = 0; i < spriteArray.Length; i++)
            {

                child = spriteArray[i];
                newRect = new Rect(child.rect.x / child.texture.width,
                                        child.rect.y / child.texture.height,
                                        child.rect.width / child.texture.width,
                                        child.rect.height / child.texture.height);

                aTexture = SpriteUtility.GetSpriteTexture(child, false);

                if (GUILayout.Button("", GUILayout.Width(spriteUnit + 1 + (spriteUnit * (1 / 16))), GUILayout.Height(spriteUnit + 1 + (spriteUnit * (1 / 16)))))
                {
                    //draw a clickable button
                    if (cmSelectedTile != null && !Event.current.control)
                    {
                        //empty the selected tile list if control isn't held. Allows multiselect of tiles.
                        cmSelectedTile.Clear();
                        cmCurSprite.Clear();
                    }
                    cmSelectedTile.Add(current); //Adds clicked on tile to list of selected tiles.
                    clickedTile = true;
                    cmCurSprite.Add(child);
                }

                //Displaying the sprite size inside the tool
                if (spriteUnit < 40)
                {
                    spriteUnit = 40;
                }
                else if (spriteUnit > 80)
                {
                    spriteUnit = 80;
                }
                //Draws the sprites
                GUI.DrawTextureWithTexCoords(new Rect(5 + (x * spriteUnit * 1.06f), 4 + (y * spriteUnit * 1.05f), spriteUnit, spriteUnit), child.texture, newRect, true);

                if (x < columnCount)
                {
                    x++;
                }
                else
                {
                    // if we have enough columns to fill the scroll area, reset the column count and start a new line of buttons
                    x = 0;
                    y++;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                //For selected tiles
                if (cmSelectedTile != null && cmSelectedTile.Contains(current))
                {
                    if (cmSelectedColor == null)
                    {
                        cmSelectedColor = new Texture2D(1, 1);
                        cmSelectedColor.alphaIsTransparency = true;
                        cmSelectedColor.filterMode = FilterMode.Point;
                        cmSelectedColor.SetPixel(0, 0, new Color(.5f, .5f, 1f, .5f));
                        cmSelectedColor.Apply();
                    }
                    GUI.DrawTexture(new Rect(5 + (x * spriteUnit * 1.06f), 4 + (y * spriteUnit * 1.05f), spriteUnit, spriteUnit), cmSelectedColor, ScaleMode.ScaleToFit, true);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        static void DrawObjectInEditor(SceneView sceneview)
        {
            if (!clickedTile)
            {
                return;
            }

            if (!isActive)
            {
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            Handles.BeginGUI();
            Handles.Label(cmCurPos, " ", EditorStyles.boldLabel);

            if (selectedTool == 0)
            {
                Handles.DrawSolidRectangleWithOutline(new Rect(cmCurPos.x, cmCurPos.y, 1, 1), Color.green, Color.black);
            }
            else if (selectedTool == 1)
            {
                Handles.DrawSolidRectangleWithOutline(new Rect(cmCurPos.x, cmCurPos.y, 1, 1), Color.red, Color.black);
            }

            Handles.EndGUI();


            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Transform child = headObject.GetComponentInChildren<Transform>();

                    //SelectedTool 0 = painting the sprites
                    if (selectedTool == 0)
                    {
                        Debug.Log(Event.current.mousePosition);
                        Transform selectedObj = child.Find("Tile|" + (Mathf.Floor(hit.point.x) + 3f) + "|" + (Mathf.Floor(hit.point.y) + 3f));
                        if (selectedObj == null || selectedObj != null && orderInLayer != selectedObj.GetComponent<SpriteRenderer>().sortingOrder)
                        {
                            obj = new GameObject("Tile|" + (Mathf.Floor(hit.point.x) + 3f) + "|" + (Mathf.Floor(hit.point.y) + 3f));
                            obj.transform.parent = child.transform;
                            obj.transform.position = new Vector2((Mathf.Floor(hit.point.x) + 0.5f), (Mathf.Floor(hit.point.y) + 0.5f));

                            obj.tag = tag;
                            obj.layer = layer;
                            obj.AddComponent<SpriteRenderer>().sortingOrder = orderInLayer;
                            obj.GetComponent<SpriteRenderer>().sprite = cmCurSprite[0];

                            if (makeCollider)
                            {
                                obj.AddComponent<BoxCollider2D>();
                            }
                        }
                        else
                        {
                            Debug.LogError("you already have placed a sprite on this spot");
                        }
                    }
                    //SelectedTool 1 = erasing the sprites
                    else if (selectedTool == 1)
                    {
                        Debug.Log("Erasing");
                        Transform selectedObj = child.Find("Tile|" + (Mathf.Floor(hit.point.x) + 3f) + "|" + (Mathf.Floor(hit.point.y) + 3f));
                        if (selectedObj != null)
                        {
                            Undo.DestroyObjectImmediate(selectedObj.gameObject);
                        }
                    }
                }
            }

            cmCurPos.x = (float)Mathf.Floor(hit.point.x);
            cmCurPos.y = (float)Mathf.Floor(hit.point.y);
            SceneView.RepaintAll();
        }

        static void CreateNewObject()
        {
            if (headObject != null)
            {
                headObject.name = "Sprite map finished";
            }

            headObject = GameObject.Find("Sprite map");

            if (headObject == null)
            {
                headObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                headObject.name = "Sprite map";
                headObject.transform.localScale = new Vector3(100, 100, 100);
                headObject.AddComponent<MeshCollider>();
                headObject.AddComponent<SpriteRenderer>();
                headObject.GetComponent<Renderer>().enabled = false;
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawObjectInEditor;
        }
    }
}